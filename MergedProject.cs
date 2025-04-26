// ===== File: CachedDb.cs =====
﻿namespace IndTrace7.Rx;

public record CachedDb(byte[] Buffer, DateTime TimeStamp)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5); // Adjustable
    public bool IsExpired => DateTime.UtcNow - TimeStamp > CacheDuration;
}



// ===== File: DbAccessInfo.cs =====
﻿using Sharp7.Rx;

namespace IndTrace7.Rx;

public class DbAccessInfo(int dbNo)
{
    public int DbNo { get; } = dbNo;
    public int MaxOffsetRead => _trackedVariables.Max(v => v.OffsetEnd);
    private readonly HashSet<TrackedVariable> _trackedVariables = new();

    public void TrackVariable(string name, VariableAddress address, Type type)
    {
        _trackedVariables.Add(new TrackedVariable(name, address.Start, address.BufferLength, type));
    }

    public IEnumerable<TrackedVariable> Variables => _trackedVariables;
}




// ===== File: IDateTime.cs =====
﻿namespace IndTrace7.Rx;

public interface IDateTime 
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}



// ===== File: IIndTrace.cs =====
﻿using Sharp7.Rx.Interfaces;

namespace IndTrace7.Rx;

public interface IIndTrace : IPlc
{
    new Task<TValue> GetValue<TValue>(string variableName, CancellationToken token = default);

}




// ===== File: IndTrace.cs =====
﻿using Sharp7.Rx.Interfaces;
using System;

namespace IndTrace7.Rx;
public interface IndTrace : IPlc
{
    Task<TValue> GetValuePlc<TValue>(string variableName, CancellationToken token = default);

}




// ===== File: IndTracePlc.cs =====
﻿using Sharp7.Rx;

namespace IndTrace7.Rx;

public class IndTracePlc : Sharp7Plc, IIndTrace
{

    private readonly PlcAccessTracker _tracker = new();
    private readonly PlcDbCache _plcDbCache = new();
    private readonly IDateTime _dateTimeProvider; // Removed 'required' keyword
    private readonly TimeSpan _cacheValidity = TimeSpan.FromSeconds(5);
    private readonly Dictionary<ushort, DateTime> _lastCacheTime = new();

    public IndTracePlc(IDateTime dateTimeProvider, string ipAddress, int rackNumber, int cpuMpiAddress, int port = 102, TimeSpan? multiVarRequestCycleTime = null)
        : base(ipAddress, rackNumber, cpuMpiAddress, port, multiVarRequestCycleTime)
    {
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider)); // Ensure it's initialized
    }

    public async Task<TValue> GetValuePlc<TValue>(string variableName, CancellationToken token = default)
    {
        var address = ParseAndVerify(variableName, typeof(TValue));

        if (_plcDbCache.TryGetDb(address.DbNo, out var cachedDb) &&
            _lastCacheTime.TryGetValue(address.DbNo, out var lastUpdate) &&
            (_dateTimeProvider.UtcNow - lastUpdate) <= _cacheValidity)
        {
            var slice = cachedDb.Buffer.Skip(address.Start).Take(address.BufferLength).ToArray();
            return ValueConverter.ReadFromBuffer<TValue>(slice, address);
        }

        int maxLength = _tracker.GetAllAccesses()
            .Where(d => d.DbNo == address.DbNo)
            .SelectMany(d => d.Variables)
            .Max(v => v.OffsetEnd);

        var fullDb = await base.GetValue<byte[]>($"Db{address.DbNo}.Byte0.{maxLength}", token);
        _plcDbCache.Update(address.DbNo, fullDb);
        _lastCacheTime[address.DbNo] = _dateTimeProvider.UtcNow;

        var valueData = fullDb.Skip(address.Start).Take(address.BufferLength).ToArray();
        return ValueConverter.ReadFromBuffer<TValue>(valueData, address);
    }
}




// ===== File: PlcAccessTracker.cs =====
﻿using Sharp7.Rx;

namespace IndTrace7.Rx;

public class PlcAccessTracker
{
    private readonly Dictionary<int, DbAccessInfo> _dbAccessRecords = new();

    public void TrackAccess(string variableName, VariableAddress address, Type valueType)
    {
        if (!_dbAccessRecords.TryGetValue(address.DbNo, out var dbInfo))
        {
            dbInfo = new DbAccessInfo(address.DbNo);
            _dbAccessRecords[address.DbNo] = dbInfo;
        }

        dbInfo.TrackVariable(variableName, address, valueType);
    }

    public IEnumerable<DbAccessInfo> GetAllAccesses() => _dbAccessRecords.Values;
}




// ===== File: PlcDbCache.cs =====
﻿namespace IndTrace7.Rx;

public class PlcDbCache
{
    private readonly Dictionary<ushort, CachedDb> _dbCache = new();

    public void Update(ushort dbNo, byte[] buffer)
    {
        _dbCache[dbNo] = new CachedDb(buffer, DateTime.UtcNow);
    }

    public bool TryGetDb(ushort dbNo, out CachedDb cachedDb) =>
        _dbCache.TryGetValue(dbNo, out cachedDb) && !cachedDb.IsExpired;
}



// ===== File: SystemDateTime.cs =====
﻿using System;
namespace IndTrace7.Rx;

public class SystemDateTime : IDateTime
{
    private readonly TimeProvider timeProvider;

    private SystemDateTime(TimeProvider? timeProvider)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public DateTime Now => timeProvider.GetLocalNow().DateTime;
    public DateTime UtcNow => timeProvider.GetUtcNow().DateTime;
}




// ===== File: TrackedVariable.cs =====
﻿namespace IndTrace7.Rx;

public record TrackedVariable(string Name, int Start, int Length, Type Type)
{
    public int OffsetEnd => Start + Length;

    public override int GetHashCode() => HashCode.Combine(Name, Start, Length, Type);
  
}



