using Sharp7.Rx;

namespace Sharp.Rx.Extensions;

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
