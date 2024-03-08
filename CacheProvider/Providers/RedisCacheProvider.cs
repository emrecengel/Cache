using CacheProvider.Extensions;
using CacheProvider.Metadatas;
using CacheProvider.Options;
using StackExchange.Redis;

namespace CacheProvider.Providers;

public sealed class RedisCacheProvider : ICacheProvider
{
    private static Lazy<ConnectionMultiplexer> _lazyConnection;
    private readonly string _metaDataKey;
    private IDatabase _cache;
    private string _cachePrefix;

    private string _connectionString;
    private TimeSpan _defaultExpiry;
    private string[] _keys;

    public RedisCacheProvider()
    {
        _metaDataKey = "MetaData";
    }

    private static ConnectionMultiplexer Connection => _lazyConnection.Value;

    //Config check to see IsCacheEnabled as well.
    public void SetDefaultExpirationTime(TimeSpan value)
    {
        _defaultExpiry = value;
    }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_cachePrefix) &&
                               !string.IsNullOrWhiteSpace(_connectionString) && Connection.IsConnected;

    /// <summary>
    ///     The top level unique keys, defaultly going in to be used in caching.
    /// </summary>
    /// <param name="cachePrefix"></param>
    /// <param name="keys">The keys.</param>
    public void SetUniqueKeys(string cachePrefix, params string[] keys)
    {
        _keys = keys;
        _cachePrefix = cachePrefix;
    }

    /// <summary>
    ///     Sets the connection string.
    /// </summary>
    /// <param name="connectionString">The connection string that redis is going to use</param>
    /// <param name="databaseInstance"></param>
    public void SetConnectionString(string connectionString, int databaseInstance = 0)
    {
        _connectionString = connectionString;
        _lazyConnection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(_connectionString));
        _cache = Connection.GetDatabase(databaseInstance);
    }

    /// <summary>
    ///     Adds the specified model.
    ///     Cache prefix and connection string is required, if not set, it is not going throw an exception but will not work
    ///     simply.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model">Any type of object that we want to create cache from</param>
    /// <param name="expiry">The expiry of the cache</param>
    /// <param name="additionalKeys">The additional keys, increase uniqueness of by filtering parameters or any sort.</param>
    public void Add<T>(T model, TimeSpan? expiry, params string[] additionalKeys)
    {
        SetToCache(model, expiry ?? _defaultExpiry, additionalKeys);
    }

    public void Add<T>(T model, DateTime endsAtUtc, params string[] additionalKeys)
    {
        SetToCache(model, endsAtUtc, additionalKeys);
    }

    public async Task AddAsync<T>(T model, TimeSpan? expiry, params string[] additionalKeys)
    {
        await SetToCacheAsync(model, expiry ?? _defaultExpiry, additionalKeys);
    }

    public async Task AddAsync<T>(T model, DateTime endsAtUtc, params string[] additionalKeys)
    {
        await SetToCacheAsync(model, endsAtUtc, additionalKeys);
    }

    /// <summary>
    ///     Retrieves the cached object by specified additional keys.
    ///     Cache prefix and connection string is required, if not set, it is not going throw an exception but not work simply.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="additionalKeys">
    ///     The additional keys, not required based on how it has been added, pull mechanism must be
    ///     the same.
    /// </param>
    /// <returns></returns>
    public T Retrieve<T>(params string[] additionalKeys)
    {
        return GetFromCache<T>(additionalKeys);
    }

    /// <summary>
    ///     Retrieves the cached object by specified additional keys.
    ///     Cache prefix and connection string is required, if not set, it is not going throw an exception but not work simply.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="additionalKeys">
    ///     The additional keys, not required based on how it has been added, pull mechanism must be
    ///     the same.
    /// </param>
    /// <returns></returns>
    public async Task<T> RetrieveAsync<T>(params string[] additionalKeys)
    {
        return await GetFromCacheAsync<T>(additionalKeys);
    }

    /// <summary>
    ///     Invalidates chache by specified additional keys. additional keys are not required
    ///     Cache prefix and connection string is required, if not set, it is not going throw an exception but not work simply.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="additionalKeys">The additional keys.</param>
    public void Invalidate<T>(params string[] additionalKeys)
    {
        Expire<T>(additionalKeys);
    }

    public void Invalidate(params string[] additionalKeys)
    {
        Expire(additionalKeys);
    }

    public void InvalidateByType(Type type)
    {
        Expire(type);
    }

    /// <summary>
    ///     Invalidates chache by specified additional keys. additional keys are not required
    ///     Cache prefix and connection string is required, if not set, it is not going throw an exception but not work simply.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="additionalKeys">The additional keys.</param>
    public async Task InvalidateAsync<T>(params string[] additionalKeys)
    {
        await ExpireAsync<T>(additionalKeys);
    }

    public async Task InvalidateAsync(params string[] additionalKeys)
    {
        await ExpireAsync(additionalKeys);
    }


    public void Dispose()
    {
        Connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    public CacheMetaData RetrieveMetaData<T>(params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return default;
        }

        var key = CacheKeyMetaDataGenerator<T>(additionalKeys);

        return _cache.StringGet(key).ToString().ParseJson<CacheMetaData>();
    }

    public async Task<CacheMetaData> RetrieveMetaDataAsync<T>(params string[] additionalKeys)
    {
        var key = CacheKeyMetaDataGenerator<T>(additionalKeys);
        var cachedString = await _cache.StringGetAsync(key);
        return cachedString.ToString().ParseJson<CacheMetaData>();
    }

    private void SetToCache<T>(T model, TimeSpan expiry, params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return;
        }

        _cache.StringSet(CacheKeyGenerator<T>(additionalKeys), model.ToJson(), expiry);
        var cacheMetaData = new CacheMetaData(expiry);
        _cache.StringSet(CacheKeyMetaDataGenerator<T>(additionalKeys), cacheMetaData.ToJson(), expiry);
    }

    private void SetToCache<T>(T model, DateTime endsUtcAt, params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return;
        }

        _cache.StringSet(CacheKeyGenerator<T>(additionalKeys), model.ToJson(), endsUtcAt - DateTime.UtcNow);
        var cacheMetaData = new CacheMetaData { CachedOn = DateTime.UtcNow, ExpiresOn = endsUtcAt };
        _cache.StringSet(CacheKeyMetaDataGenerator<T>(additionalKeys), cacheMetaData.ToJson(),
            endsUtcAt - DateTime.UtcNow);
    }

    private async Task SetToCacheAsync<T>(T model, DateTime endsUtcAt, params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return;
        }

        await _cache.StringSetAsync(CacheKeyGenerator<T>(additionalKeys), model.ToJson(),
            endsUtcAt - DateTime.UtcNow);

        var cacheMetaData = new CacheMetaData { CachedOn = DateTime.UtcNow, ExpiresOn = endsUtcAt };
        await _cache.StringSetAsync(CacheKeyMetaDataGenerator<T>(additionalKeys), cacheMetaData.ToJson(),
            endsUtcAt - DateTime.UtcNow);
    }

    private async Task SetToCacheAsync<T>(T model, TimeSpan expiry, params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return;
        }

        var setCache = _cache.StringSetAsync(CacheKeyGenerator<T>(additionalKeys), model.ToJson(), expiry);
        var cacheMetaData = new CacheMetaData(expiry);
        var setMetaData =
            _cache.StringSetAsync(CacheKeyMetaDataGenerator<T>(additionalKeys), cacheMetaData.ToJson(), expiry);
        await Task.WhenAll(setCache, setMetaData);
    }

    private T GetFromCache<T>(params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return default;
        }

        return _cache.StringGet(CacheKeyGenerator<T>(additionalKeys)).ToString().ParseJson<T>();
    }

    private async Task<T> GetFromCacheAsync<T>(params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return default;
        }

        var cachedString = await _cache.StringGetAsync(CacheKeyGenerator<T>(additionalKeys));

        return cachedString.ToString().ParseJson<T>();
    }

    private void Expire<T>(params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return;
        }

        var keyDefinition = CacheKeyGenerator<T>(additionalKeys);

        var server = Connection.GetServer(Connection.GetEndPoints().FirstOrDefault());

        //* is a special keyword, we can create a pattern around it, in our case everything after * going to be removed.
        //The intention is if we have an Associations list in cache, individual association with any types of additional keys, should be removed when the model invalidation is called.
        var keys = server.Keys(_cache.Database, $"{keyDefinition}*");

        foreach (var key in keys)
        {
            _cache.KeyExpire(key, TimeSpan.Zero);
        }
    }

    private void Expire(Type type)
    {
        if (!IsAvailable)
        {
            return;
        }

        var keyDefinition = CacheKeyGenerator(type);

        var server = Connection.GetServer(Connection.GetEndPoints().FirstOrDefault());

        //* is a special keyword, we can create a pattern around it, in our case everything after * going to be removed.
        //The intention is if we have an Associations list in cache, individual association with any types of additional keys, should be removed when the model invalidation is called.
        var keys = server.Keys(_cache.Database, $"{keyDefinition}*");

        foreach (var key in keys)
        {
            _cache.KeyExpire(key, TimeSpan.Zero);
        }
    }

    private void Expire(params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return;
        }

        var keyDefinition = CacheKeyGenerator(additionalKeys);

        var server = Connection.GetServer(Connection.GetEndPoints().FirstOrDefault());

        //* is a special keyword, we can create a pattern around it, in our case everything after * going to be removed.
        //The intention is if we have an Associations list in cache, individual association with any types of additional keys, should be removed when the model invalidation is called.
        var keys = server.Keys(_cache.Database, $"{keyDefinition}*");

        foreach (var key in keys)
        {
            _cache.KeyExpire(key, TimeSpan.Zero);
        }
    }

    private async Task ExpireAsync(params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return;
        }

        var keyDefinition = CacheKeyGenerator(additionalKeys);

        var server = Connection.GetServer(Connection.GetEndPoints().FirstOrDefault());

        //* is a special keyword, we can create a pattern around it, in our case everything after * going to be removed.
        //The intention is if we have an Associations list in cache, individual association with any types of additional keys, should be removed when the model invalidation is called.
        var keys = server.Keys(_cache.Database, $"{keyDefinition}*");

        foreach (var key in keys)
        {
            await _cache.KeyExpireAsync(key, TimeSpan.Zero);
        }
    }

    private async Task ExpireAsync<T>(params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return;
        }

        var keyDefinition = CacheKeyGenerator<T>(additionalKeys);

        var server = Connection.GetServer(Connection.GetEndPoints().FirstOrDefault());

        //* is a special keyword, we can create a pattern around it, in our case everything after * going to be removed.
        //The intention is if we have an Associations list in cache, individual association with any types of additional keys, should be removed when the model invalidation is called.
        var keys = server.Keys(_cache.Database, $"*{keyDefinition}*");

        foreach (var key in keys)
        {
            await _cache.KeyExpireAsync(key, TimeSpan.Zero);
        }
    }

    private string CacheKeyGenerator(params string[] additionalKeys)
    {
        var keys = new List<string> { _cachePrefix };

        if (_keys.Any())
        {
            keys.AddRange(_keys);
        }

        if (additionalKeys.Any())
        {
            keys.AddRange(additionalKeys.Where(x => x != "IResult"));
        }

        return string.Join(".", keys);
    }

    private string CacheKeyGenerator(Type type)
    {
        var keys = new List<string> { _cachePrefix };

        if (_keys.Any())
        {
            keys.AddRange(_keys);
        }

        keys.Add(type.NestedTypeName());

        return string.Join(".", keys);
    }

    private string CacheKeyGenerator<T>(params string[] additionalKeys)
    {
        var keys = new List<string> { _cachePrefix };

        if (_keys.Any())
        {
            keys.AddRange(_keys);
        }

        keys.Add(typeof(T).NestedTypeName());

        if (additionalKeys.Any())
        {
            keys.AddRange(additionalKeys);
        }

        return string.Join(".", keys);
    }

    private string CacheKeyMetaDataGenerator<T>(params string[] additionalKeys)
    {
        var keys = additionalKeys.ToList();
        keys.Add(_metaDataKey);
        return CacheKeyGenerator<T>(keys.ToArray());
    }


    #region Cache Processors

    public T CacheProcessor<T>(Action<CacheOption<T>> options)
    {
        var cacheOptions = new CacheOption<T>();
        options(cacheOptions);

        try
        {
            var cachedReturn = Retrieve<T>(cacheOptions.UniqueParameters);

            if (cachedReturn == null)
            {
                var value = cacheOptions.Function();
                if (value != null && cacheOptions.CacheCondition(value))
                {
                    if (cacheOptions.ExpirationDateTime != null)
                    {
                        Add(value, cacheOptions.ExpirationDateTime.Value, cacheOptions.UniqueParameters);
                    }
                    else
                    {
                        Add(value, cacheOptions.ExpirationTimeSpan, cacheOptions.UniqueParameters);
                    }
                }

                return value;
            }

            return cachedReturn;
        }
        catch (Exception)
        {
            return cacheOptions.Function();
        }
    }

    public async Task<T> CacheProcessorAsync<T>(Action<CacheOption<T>> options)
    {
        var cacheOptions = new CacheOption<T>();
        options(cacheOptions);

        try
        {
            var cachedReturn = await RetrieveAsync<T>(cacheOptions.UniqueParameters);

            if (cachedReturn == null)
            {
                var value = cacheOptions.AsyncFunction != null
                    ? await cacheOptions.AsyncFunction()
                    : cacheOptions.Function();
                if (value != null)
                {
                    if (cacheOptions.ExpirationDateTime != null)
                    {
                        await AddAsync(value, cacheOptions.ExpirationDateTime.Value, cacheOptions.UniqueParameters);
                    }
                    else
                    {
                        await AddAsync(value, cacheOptions.ExpirationTimeSpan, cacheOptions.UniqueParameters);
                    }
                }

                return value;
            }

            return cachedReturn;
        }
        catch (Exception)
        {
            return cacheOptions.AsyncFunction != null ? await cacheOptions.AsyncFunction() : cacheOptions.Function();
        }
    }

    #endregion
}
