using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public class SingleArrayToArrayOfArraysConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(double[][]);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
        if (token.Type == JTokenType.Array && token.First?.Type != JTokenType.Array)
        {
            // It's a single array, so wrap it into an array of arrays
            return new double[][] { token.ToObject<double[]>() };
        }
        else
        {
            // It's already an array of arrays, so just deserialize it normally
            return token.ToObject<double[][]>();
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}
