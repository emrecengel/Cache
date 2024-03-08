using System.Runtime.Caching;
using CacheProvider.Extensions;
using CacheProvider.Metadatas;
using CacheProvider.Options;

namespace CacheProvider.Providers;

internal sealed class MemoryCacheProvider : ICacheProvider
{
    private readonly MemoryCache _cache;
    private readonly string _metaDataKey;

    private string _cachePrefix;
    private TimeSpan _defaultExpiry;

    private string _keys;

    public MemoryCacheProvider()
    {
        _cache = MemoryCache.Default;
        _metaDataKey = "MetaData";
    }

    public void SetDefaultExpirationTime(TimeSpan value)
    {
        _defaultExpiry = value;
    }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_cachePrefix);

    public void SetUniqueKeys(string cachePrefix, params string[] keys)
    {
        _cachePrefix = cachePrefix;
        _keys = string.Join(".", keys);
    }

    public void SetConnectionString(string connectionString, int databaseInstance = 0)
    {
        //No Connection string is required for the mem cache provider.
    }

    public async Task AddAsync<T>(T model, TimeSpan? expiry, params string[] additionalKeys)
    {
        Add(model, expiry, additionalKeys);
        await Task.CompletedTask;
    }

    public async Task AddAsync<T>(T model, DateTime endsAtUtc, params string[] additionalKeys)
    {
        Add(model, endsAtUtc, additionalKeys);
        await Task.CompletedTask;
    }

    public void Add<T>(T model, TimeSpan? expiry, params string[] additionalKeys)
    {
        SetToCache(model, expiry, additionalKeys);
    }

    public void Add<T>(T model, DateTime endsAtUtc, params string[] additionalKeys)
    {
        SetToCache(model, endsAtUtc, additionalKeys);
    }

    public T Retrieve<T>(params string[] additionalKeys)
    {
        return GetFromCache<T>(additionalKeys);
    }

    public async Task<T> RetrieveAsync<T>(params string[] additionalKeys)
    {
        return await Task.FromResult(Retrieve<T>(additionalKeys));
    }

    public async Task InvalidateAsync<T>(params string[] additionalKeys)
    {
        Invalidate<T>(additionalKeys);
        await Task.CompletedTask;
    }

    public async Task InvalidateAsync(params string[] additionalKeys)
    {
        Invalidate(additionalKeys);
        await Task.CompletedTask;
    }

    public void Invalidate<T>(params string[] additionalKeys)
    {
        Expire<T>(additionalKeys);
    }

    public void Invalidate(params string[] additionalKeys)
    {
        throw new NotImplementedException();
    }

    public CacheMetaData RetrieveMetaData<T>(params string[] additionalKeys)
    {
        var key = CacheKeyMetaDataGenerator<T>(additionalKeys);
        return _cache.Get(key)?.ToString()?.ParseJson<CacheMetaData>();
    }

    public async Task<CacheMetaData> RetrieveMetaDataAsync<T>(params string[] additionalKeys)
    {
        return await Task.FromResult(RetrieveMetaData<T>(additionalKeys));
    }

    public void InvalidateByType(Type type)
    {
        Expire(type);
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

    #endregion


    public async Task<T> CacheProcessorAsync<T>(Action<CacheOption<T>> options)
    {
        var cacheOptions = new CacheOption<T>();
        options(cacheOptions);


        try
        {
            var cachedReturn = Retrieve<T>(cacheOptions.UniqueParameters);

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

    public void Dispose()
    {
        _cache?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetToCache<T>(T model, TimeSpan? expiry = null, params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return;
        }

        var expiration = expiry.HasValue
            ? DateTimeOffset.UtcNow.AddSeconds(expiry.Value.TotalSeconds)
            : DateTimeOffset.UtcNow.AddSeconds(_defaultExpiry.TotalSeconds);

        _cache.Set(CacheKeyGenerator<T>(additionalKeys), model.ToJson(),
            expiration);

        var cacheMetaData = new CacheMetaData(expiration);
        _cache.Set(CacheKeyMetaDataGenerator<T>(additionalKeys), cacheMetaData.ToJson(), expiration);
    }

    private void SetToCache<T>(T model, DateTime endsAtUtc, params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return;
        }

        var expiration = DateTimeOffset.UtcNow.AddSeconds((endsAtUtc - DateTime.UtcNow).TotalSeconds);
        _cache.Set($"{CacheKeyGenerator<T>(additionalKeys)}", model.ToJson(),
            expiration);

        var cacheMetaData = new CacheMetaData(expiration);
        _cache.Set(CacheKeyMetaDataGenerator<T>(additionalKeys), cacheMetaData.ToJson(), expiration);
    }

    private T GetFromCache<T>(params string[] additionalKeys)
    {
        if (!IsAvailable)
        {
            return default;
        }

        var cachePointer = CacheKeyGenerator<T>(additionalKeys);
        var cachedObject = _cache.Get(cachePointer);

        if (cachedObject == null)
        {
            return default;
        }

        return cachedObject
            .ToString()
            .ParseJson<T>();
    }

    private void Expire<T>(params string[] additionalKeys)
    {
        var keyDefinition = CacheKeyGenerator<T>(additionalKeys);

        var keyAndValues = _cache.AsEnumerable().Where(x => x.Key.Contains(keyDefinition));

        foreach (var keyAndValue in keyAndValues)
        {
            _cache.Remove(keyAndValue.Key);
        }
    }

    private void Expire(Type type)
    {
        var keyDefinition = CacheKeyGenerator(type);

        var keyAndValues = _cache.AsEnumerable().Where(x => x.Key.Contains(keyDefinition));

        foreach (var keyAndValue in keyAndValues)
        {
            _cache.Remove(keyAndValue.Key);
        }
    }

    private string CacheKeyGenerator<T>(params string[] additionalKeys)
    {
        return $"{_cachePrefix}.{_keys}.{typeof(T).NestedTypeName()}.{string.Join(".", additionalKeys)}";
    }

    private string CacheKeyGenerator(Type type)
    {
        return $"{_cachePrefix}.{_keys}.{type.NestedTypeName()}";
    }

    private string CacheKeyMetaDataGenerator<T>(params string[] additionalKeys)
    {
        var keys = additionalKeys.ToList();
        keys.Add(_metaDataKey);
        return CacheKeyGenerator<T>(keys.ToArray());
    }
}
