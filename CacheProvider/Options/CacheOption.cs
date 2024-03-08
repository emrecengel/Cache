namespace CacheProvider.Options;

public sealed class CacheOption<T>
{
    internal Func<T> Function { get; private set; }
    internal Func<Task<T>> AsyncFunction { get; private set; }
    internal TimeSpan? ExpirationTimeSpan { get; private set; }
    internal DateTime? ExpirationDateTime { get; private set; }
    internal string[] UniqueParameters { get; private set; }
    internal Func<T, bool> CacheCondition { get; private set; }

    public CacheOption<T> SetFunction(Func<T> value)
    {
        Function = value;
        return this;
    }


    public CacheOption<T> SetFunction(Func<Task<T>> value)
    {
        AsyncFunction = value;
        return this;
    }

    public CacheOption<T> SetCacheCondition(Func<T, bool> value)
    {
        CacheCondition = value;
        return this;
    }

    public CacheOption<T> SetExpiration(TimeSpan value)
    {
        ExpirationTimeSpan = value;
        return this;
    }

    public CacheOption<T> SetExpiration(DateTime value)
    {
        ExpirationDateTime = value;
        return this;
    }

    public CacheOption<T> SetUniqueParameters(params string[] value)
    {
        UniqueParameters = value;
        return this;
    }
}
