namespace CacheProvider.Extensions;

internal static class StringExtensions
{
    public static string IfNullReturnEmpty(this string value)
    {
        return value ?? string.Empty;
    }
}
