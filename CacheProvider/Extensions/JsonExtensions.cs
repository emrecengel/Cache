using CacheProvider.Converters;
using Newtonsoft.Json;

namespace CacheProvider.Extensions;

internal static class JsonExtensions
{
    private static JsonSerializerSettings Settings()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new CustomDataTableConverter());
        settings.Formatting = Formatting.None;
        settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        return settings;
    }

    public static string ToJson<T>(this T selectedObject)
    {
        return JsonConvert.SerializeObject(selectedObject, Settings());
    }

    public static T ParseJson<T>(this string jsonString)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(jsonString.IfNullReturnEmpty().Trim(), Settings());
        }
        catch (Exception)
        {
            return default;
        }
    }
}
