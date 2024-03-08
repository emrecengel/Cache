using CacheProvider.Metadatas;
using CacheProvider.Options;

namespace CacheProvider;

public interface ICacheProvider : IDisposable
{
    bool IsAvailable { get; }

    void SetDefaultExpirationTime(TimeSpan value);

    /// <summary>
    ///     The top level unique keys, by default going in to be used in caching.
    /// </summary>
    /// <param name="cachePrefix"></param>
    /// <param name="keys">The keys.</param>
    void SetUniqueKeys(string cachePrefix, params string[] keys);

    /// <summary>
    ///     Sets the connection string.
    /// </summary>
    /// <param name="connectionString">The connection string that redis is going to use</param>
    /// <param name="databaseInstance"></param>
    void SetConnectionString(string connectionString, int databaseInstance = 0);

    /// <summary>
    ///     Adds the specified model.
    ///     Cache prefix and connection string is required, if not set, it is not going throw an exception but will not work
    ///     simply.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model">Any type of object that we want to create cache from</param>
    /// <param name="expiry">The expiry of the cache</param>
    /// <param name="additionalKeys">The additional keys, increase uniqueness of by filtering parameters or any sort.</param>
    void Add<T>(T model, TimeSpan? expiry, params string[] additionalKeys);

    /// <summary>
    ///     Adds the specified model.
    ///     Cache prefix and connection string is required, if not set, it is not going throw an exception but will not work
    ///     simply.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model">Any type of object that we want to create cache from</param>
    /// <param name="endsAtUtc">The DateTime in UTC to expire the cache</param>
    /// <param name="additionalKeys">The additional keys, increase uniqueness of by filtering parameters or any sort.</param>
    void Add<T>(T model, DateTime endsAtUtc, params string[] additionalKeys);

    /// <summary>
    ///     Adds the specified model.
    ///     Cache prefix and connection string is required, if not set, it is not going throw an exception but will not work
    ///     simply.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model">Any type of object that we want to create cache from</param>
    /// <param name="expiry">The expiry of the cache</param>
    /// <param name="additionalKeys">The additional keys, increase uniqueness of by filtering parameters or any sort.</param>
    Task AddAsync<T>(T model, TimeSpan? expiry, params string[] additionalKeys);


    /// <summary>
    ///     Adds the specified model.
    ///     Cache prefix and connection string is required, if not set, it is not going throw an exception but will not work
    ///     simply.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model">Any type of object that we want to create cache from</param>
    /// <param name="endsAtUtc">The DateTime in UTC to expire the cache</param>
    /// <param name="additionalKeys">The additional keys, increase uniqueness of by filtering parameters or any sort.</param>
    Task AddAsync<T>(T model, DateTime endsAtUtc, params string[] additionalKeys);

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
    T Retrieve<T>(params string[] additionalKeys);

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
    Task<T> RetrieveAsync<T>(params string[] additionalKeys);

    /// <summary>
    ///     Invalidates cache by specified additional keys. additional keys are not required
    ///     Cache prefix and connection string is required, if not set, it is not going throw an exception but not work simply.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="additionalKeys">The additional keys.</param>
    void Invalidate<T>(params string[] additionalKeys);

    void Invalidate(params string[] additionalKeys);

    /// <summary>
    ///     Invalidates chache by specified additional keys. additional keys are not required
    ///     Cache prefix and connection string is required, if not set, it is not going throw an exception but not work simply.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="additionalKeys">The additional keys.</param>
    Task InvalidateAsync<T>(params string[] additionalKeys);

    Task InvalidateAsync(params string[] additionalKeys);
    T CacheProcessor<T>(Action<CacheOption<T>> options);
    Task<T> CacheProcessorAsync<T>(Action<CacheOption<T>> options);


    CacheMetaData RetrieveMetaData<T>(params string[] additionalKeys);
    Task<CacheMetaData> RetrieveMetaDataAsync<T>(params string[] additionalKeys);
    void InvalidateByType(Type type);
}
