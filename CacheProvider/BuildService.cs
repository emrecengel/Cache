using CacheProvider.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace CacheProvider;

public static class BuildService
{
    public enum CacheProvider
    {
        Memory = 0,
        Redis = 1
    }

    public static IServiceCollection UseCacheProvider(this IServiceCollection services,
        Action<CacheServiceOption> value)
    {
        var cacheServiceOption = new CacheServiceOption();
        value(cacheServiceOption);


        services.AddScoped(provider =>
        {
            ICacheProvider cacheProvider = cacheServiceOption.CacheProvider switch
            {
                CacheProvider.Memory => new MemoryCacheProvider(),
                CacheProvider.Redis => new RedisCacheProvider(),
                _ => throw new ArgumentOutOfRangeException(nameof(provider))
            };

            cacheProvider.SetDefaultExpirationTime(cacheServiceOption.DefaultExpirationTime);
            cacheProvider.SetConnectionString(cacheServiceOption.ConnectionString, cacheServiceOption.DatabaseInstance);


            return cacheProvider;
        });

        return services;
    }

    public sealed class CacheServiceOption
    {
        internal CacheProvider CacheProvider { get; set; } = CacheProvider.Memory;
        internal string ConnectionString { get; set; }

        internal TimeSpan DefaultExpirationTime { get; set; } = TimeSpan.FromMinutes(10);
        internal int DatabaseInstance { get; set; } = 10;

        internal string[] UniqueCacheKeys { get; set; }

        internal string CachePrefix { get; set; }

        public CacheServiceOption SetDefaultCacheKeys(string cachePrefix, params string[] uniqueCacheKeys)
        {
            CachePrefix = cachePrefix;
            UniqueCacheKeys = uniqueCacheKeys;
            return this;
        }

        public CacheServiceOption UseMemoryCache()
        {
            CacheProvider = CacheProvider.Memory;
            return this;
        }

        public CacheServiceOption UseRedisCache(string connectionString, int databaseInstance = 0)
        {
            CacheProvider = CacheProvider.Redis;
            ConnectionString = connectionString;
            DatabaseInstance = databaseInstance;
            return this;
        }

        public CacheServiceOption SetDefaultExpirationTime(TimeSpan value)
        {
            DefaultExpirationTime = value;
            return this;
        }
    }
}
