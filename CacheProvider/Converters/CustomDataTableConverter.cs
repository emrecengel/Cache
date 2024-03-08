using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CacheProvider.Converters;

internal sealed class CustomDataTableConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DataTable);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var dt = (DataTable)value;
        var metaDataObj = new JObject();
        foreach (DataColumn col in dt.Columns)
        {
            metaDataObj.Add(col.ColumnName, col.DataType.AssemblyQualifiedName);
        }

        var rowsArray = new JArray { metaDataObj };
        foreach (DataRow row in dt.Rows)
        {
            var rowDataObj = new JObject();
            foreach (DataColumn col in dt.Columns)
            {
                rowDataObj.Add(col.ColumnName, JToken.FromObject(row[col]));
            }

            rowsArray.Add(rowDataObj);
        }

        rowsArray.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var rowsArray = JArray.Load(reader);
        var metaDataObj = (JObject)rowsArray.First();
        var dt = new DataTable();
        foreach (var prop in metaDataObj.Properties())
        {
            dt.Columns.Add(prop.Name, Type.GetType((string)prop.Value, true));
        }

        foreach (var jToken in rowsArray.Skip(1))
        {
            var rowDataObj = (JObject)jToken;
            var row = dt.NewRow();
            foreach (DataColumn col in dt.Columns)
            {
                row[col] = rowDataObj[col.ColumnName].ToObject(col.DataType);
            }

            dt.Rows.Add(row);
        }

        return dt;
    }
}
