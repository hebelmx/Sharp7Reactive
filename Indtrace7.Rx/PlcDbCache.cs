namespace IndTrace7.Rx;

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