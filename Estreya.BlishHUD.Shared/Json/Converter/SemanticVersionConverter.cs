namespace Estreya.BlishHUD.Shared.Json.Converter
{
    using Newtonsoft.Json;
    using System;

    public class SemanticVersionConverter : Newtonsoft.Json.JsonConverter<SemVer.Version>
    {
        public override SemVer.Version ReadJson(JsonReader reader, Type objectType, SemVer.Version existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (objectType != typeof(SemVer.Version))
            {
                return new SemVer.Version(0, 0, 0);
            }

            string value = (string)reader.Value;

            return new SemVer.Version(value);
        }

        public override void WriteJson(JsonWriter writer, SemVer.Version value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
