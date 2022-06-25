namespace Estreya.BlishHUD.Shared.Models.GW2API.Converter;

using Gw2Sharp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CoordinatesConverter : JsonConverter<Coordinates2>
{
    public override Coordinates2 ReadJson(JsonReader reader, Type objectType, Coordinates2 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
        {
            throw new JsonException("Expected start of array");
        }

        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of array");
        }

        double x = serializer.Deserialize<double>(reader);

        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of array");
        }

        double y = serializer.Deserialize<double>(reader);

        if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
        {
            throw new JsonException("Expected end of array");
        }

        return new Coordinates2(x, y);
    }

    public override void WriteJson(JsonWriter writer, Coordinates2 value, JsonSerializer serializer)
    {
        writer.WriteStartArray();

        serializer.Serialize(writer, value.X);
        serializer.Serialize(writer, value.Y);

        writer.WriteEndArray();
    }
}
