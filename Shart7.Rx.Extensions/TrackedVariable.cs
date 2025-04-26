namespace Sharp.Rx.Extensions;

public record TrackedVariable(string Name, int Start, int Length, Type Type)
{
    public int OffsetEnd => Start + Length;

    public override int GetHashCode() => HashCode.Combine(Name, Start, Length, Type);
  
}
