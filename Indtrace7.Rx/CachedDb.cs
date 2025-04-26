namespace IndTrace7.Rx;

public record CachedDb(byte[] Buffer, DateTime TimeStamp)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5); // Adjustable
    public bool IsExpired => DateTime.UtcNow - TimeStamp > CacheDuration;
}