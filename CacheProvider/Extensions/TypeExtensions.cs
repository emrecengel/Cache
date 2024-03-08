namespace CacheProvider.Extensions;

public static class TypeExtensions
{
    public static string NestedTypeName(this Type type)
    {
        if (type.GetGenericArguments().Any())
        {
            return NestedTypeName(type.GetGenericArguments()[0]);
        }

        return type.Name;
    }
}
