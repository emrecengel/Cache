namespace CacheProvider.Metadatas;

public sealed class CacheMetaData
{
    public CacheMetaData()
    {
    }

    public CacheMetaData(TimeSpan expiresAfter)
    {
        CachedOn = DateTime.UtcNow;
        ExpiresOn = CachedOn.AddSeconds(expiresAfter.TotalSeconds);
    }

    public CacheMetaData(DateTimeOffset expiration)
    {
        CachedOn = DateTime.UtcNow;
        ExpiresOn = expiration.DateTime;
    }

    public DateTime CachedOn { get; set; }
    public DateTime ExpiresOn { get; set; }
}
