
using Sharp7.Rx;
using Sharp7.Rx.Enums;

namespace IndTrace7.Rx;

public class IndTracePlc(IDateTime dateTimeProvider, string ipAddress, int rackNumber, int cpuMpiAddress, int port = 102, TimeSpan? multiVarRequestCycleTime = null)
    : Sharp7Plc(ipAddress, rackNumber, cpuMpiAddress, port, multiVarRequestCycleTime), IIndTrace
{
    private readonly PlcAccessTracker tracker = new();
    private readonly PlcDbCache plcDbCache = new();
    private readonly IDateTime _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    private readonly TimeSpan cacheValidity = TimeSpan.FromSeconds(5);
    private readonly Dictionary<ushort, DateTime> lastCacheTime = new();

    public async Task ForceRefreshDbCache(ushort dbNo, CancellationToken token = default)
    {
        int maxLength = tracker.GetAllAccesses()
            .Where(d => d.DbNo == dbNo)
            .SelectMany(d => d.Variables)
            .Max(v => v.OffsetEnd);

        var data = await base.GetValue<byte[]>($"Db{dbNo}.Byte0.{maxLength}", token);
        plcDbCache.Update(dbNo, data);
        lastCacheTime[dbNo] = _dateTimeProvider.UtcNow;
    }

    public async Task<Dictionary<string, object>> ReadAllTrackedValuesFromDb(ushort dbNo, CancellationToken token = default)
    {
        var dbInfo = tracker.GetAllAccesses().FirstOrDefault(d => d.DbNo == dbNo);
        if (dbInfo is null)
            return new();

        await ForceRefreshDbCache(dbNo, token);

        if (!plcDbCache.TryGetDb(dbNo, out var cachedDb))
            throw new InvalidOperationException($"Cache refresh failed for DB{dbNo}");

        var results = new Dictionary<string, object>();
        foreach (var variable in dbInfo.Variables)
        {
            var address = new VariableAddress(Operand.Db, dbNo, DbType.Byte, (ushort)variable.Start, (ushort)variable.Length);
            var data = cachedDb.Buffer.Skip(variable.Start).Take(variable.Length).ToArray();
            var value = ValueConverter.ReadFromBuffer(data, address, variable.Type);
            results[variable.Name] = value;
        }

        return results;
    }

    public async Task<TValue> GetValuePlc<TValue>(
        string variableName,
        PlcReadMode mode = PlcReadMode.Cached,
        CancellationToken token = default)
    {
        var address = ParseAndVerify(variableName, typeof(TValue));

        switch (mode)
        {
            case PlcReadMode.Direct:
                return await base.GetValue<TValue>(variableName, token);

            case PlcReadMode.ForceRefresh:
                await ForceRefreshDbCache(address.DbNo, token);
                goto case PlcReadMode.Cached;

            case PlcReadMode.ForceRefreshAllTrackedValues:
                await ForceRefreshDbCache(address.DbNo, token);
                await ReadAllTrackedValuesFromDb(address.DbNo, token);
                goto case PlcReadMode.Cached;

            case PlcReadMode.Cached:
            default:
                if (plcDbCache.TryGetDb(address.DbNo, out var cachedDb) &&
                    lastCacheTime.TryGetValue(address.DbNo, out var lastUpdate) &&
                    (_dateTimeProvider.UtcNow - lastUpdate) <= cacheValidity)
                {
                    var slice = cachedDb.Buffer.Skip(address.Start).Take(address.BufferLength).ToArray();
                    return ValueConverter.ReadFromBuffer<TValue>(slice, address);
                }

                int maxLength = tracker.GetAllAccesses()
                    .Where(d => d.DbNo == address.DbNo)
                    .SelectMany(d => d.Variables)
                    .Max(v => v.OffsetEnd);

                var fullDb = await base.GetValue<byte[]>($"Db{address.DbNo}.Byte0.{maxLength}", token);
                plcDbCache.Update(address.DbNo, fullDb);
                lastCacheTime[address.DbNo] = _dateTimeProvider.UtcNow;

                var valueData = fullDb.Skip(address.Start).Take(address.BufferLength).ToArray();
                return ValueConverter.ReadFromBuffer<TValue>(valueData, address);
        }
    }

    public static object ReadFromBuffer(byte[] buffer, VariableAddress address, Type type)
    {
        var method = typeof(ValueConverter).GetMethod(nameof(ReadFromBuffer))!.MakeGenericMethod(type);
        return method.Invoke(null, new object[] { buffer, address })!;
    }
}

public enum PlcReadMode
{
    Direct,
    Cached,
    ForceRefresh,
    ForceRefreshAllTrackedValues
}
