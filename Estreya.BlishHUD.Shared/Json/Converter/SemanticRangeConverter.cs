namespace Estreya.BlishHUD.Shared.Json.Converter;

using Newtonsoft.Json;
using SemVer;
using System;

public class SemanticRangeConverter : JsonConverter<Range>
{
    public override Range ReadJson(JsonReader reader, Type objectType, Range existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (objectType != typeof(Range))
        {
            return null;
        }

        string value = (string)reader.Value;

        return new Range(value);
    }

    public override void WriteJson(JsonWriter writer, Range value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}