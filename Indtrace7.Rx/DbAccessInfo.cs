using Sharp7.Rx;

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
