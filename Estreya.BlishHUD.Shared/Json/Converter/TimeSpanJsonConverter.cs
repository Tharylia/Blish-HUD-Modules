namespace Estreya.BlishHUD.Shared.Json.Converter
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        private IEnumerable<string> Formats { get; set; } = new[] { "dd\\.hh\\:mm", "hh\\:mm" };
        private string ToStringFormat { get; set; }

        public TimeSpanJsonConverter()
        {

        }
        public TimeSpanJsonConverter(string toStringFormat, IEnumerable<string> parseFormats) : this()
        {
            this.ToStringFormat = toStringFormat;
            this.Formats = parseFormats;
        }

        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(this.ToStringFormat))
            {
                throw new ArgumentNullException(nameof(this.ToStringFormat), "Format has not been specified.");
            }

            writer.WriteValue(value.ToString(this.ToStringFormat));
        }

        public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (objectType != typeof(TimeSpan))
            {
                return TimeSpan.Zero;
            }

            string value = (string)reader.Value;

            foreach (string format in this.Formats)
            {
                if (TimeSpan.TryParseExact(value, format, CultureInfo.InvariantCulture, out TimeSpan result))
                {
                    return result;
                }
            }

            return TimeSpan.Zero;
        }
    }
}
