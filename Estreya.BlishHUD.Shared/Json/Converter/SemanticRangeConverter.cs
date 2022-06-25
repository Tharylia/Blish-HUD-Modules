namespace Estreya.BlishHUD.Shared.Json.Converter
{
    using Newtonsoft.Json;
    using System;

    public class SemanticRangeConverter : Newtonsoft.Json.JsonConverter<SemVer.Range>
    {
        public override SemVer.Range ReadJson(JsonReader reader, Type objectType, SemVer.Range existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (objectType != typeof(SemVer.Range))
            {
                return null;
            }

            string value = (string)reader.Value;

            return new SemVer.Range(value);
        }

        public override void WriteJson(JsonWriter writer, SemVer.Range value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
