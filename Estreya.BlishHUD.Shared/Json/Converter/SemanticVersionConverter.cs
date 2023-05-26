namespace Estreya.BlishHUD.Shared.Json.Converter;

using Newtonsoft.Json;
using System;
using Version = SemVer.Version;

public class SemanticVersionConverter : JsonConverter<Version>
{
    public override Version ReadJson(JsonReader reader, Type objectType, Version existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (objectType != typeof(Version))
        {
            return new Version(0, 0, 0);
        }

        string value = (string)reader.Value;

        return new Version(value);
    }

    public override void WriteJson(JsonWriter writer, Version value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}