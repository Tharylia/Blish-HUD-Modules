namespace Estreya.BlishHUD.Shared.Json.Converter;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class TimeSpanArrayJsonConverter : JsonConverter<TimeSpan[]>
{
    public TimeSpanArrayJsonConverter()
    {
    }

    public TimeSpanArrayJsonConverter(string toStringFormat, IEnumerable<string> parseFormats) : this()
    {
        this.ToStringFormat = toStringFormat;
        this.Formats = parseFormats;
    }

    public TimeSpanArrayJsonConverter(string toStringFormat, IEnumerable<string> parseFormats, bool keepExistingIfEmpty) : this(toStringFormat, parseFormats)
    {
        this.KeepExistingIfEmpty = keepExistingIfEmpty;
    }

    private IEnumerable<string> Formats { get; } = new[]
    {
        "dd\\.hh\\:mm",
        "hh\\:mm"
    };

    private string ToStringFormat { get; set; }
    private bool KeepExistingIfEmpty { get; }

    public override void WriteJson(JsonWriter writer, TimeSpan[] value, JsonSerializer serializer)
    {
        if (string.IsNullOrWhiteSpace(this.ToStringFormat))
        {
            throw new ArgumentNullException(nameof(this.ToStringFormat), "Format has not been specified.");
        }

        string[] stringValues = value.Select(v => v.ToString(this.ToStringFormat)).ToArray();
        serializer.Serialize(writer, stringValues);
    }

    public override TimeSpan[] ReadJson(JsonReader reader, Type objectType, TimeSpan[] existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (objectType != typeof(TimeSpan[]))
        {
            return new TimeSpan[0];
        }

        List<TimeSpan> timespans = new List<TimeSpan>();

        // deserialze the array
        if (reader.TokenType != JsonToken.Null)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                // this one respects other registered converters (e.g. the TrimmedStringConverter)
                // but causes server crashes when used globally due to endless loops
                string[] tempValues = serializer.Deserialize<string[]>(reader);

                timespans.AddRange(tempValues.Select(tv =>
                {
                    TimeSpan? ts = null;
                    foreach (string format in this.Formats)
                    {
                        if (TimeSpan.TryParseExact(tv, format, CultureInfo.InvariantCulture, out TimeSpan result))
                        {
                            ts = result;
                            break;
                        }
                    }

                    return ts;
                }).Where(ts => ts.HasValue).Select(ts => ts.Value).ToList());
            }
        }

        if (timespans.Count == 0 && this.KeepExistingIfEmpty && hasExistingValue)
        {
            timespans.AddRange(existingValue);
        }

        return timespans.ToArray();
    }
}