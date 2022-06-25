namespace Estreya.BlishHUD.Shared.Models.GW2API.Converter;

using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;
using System;

public abstract class RectangleConverter : JsonConverter<Rectangle>
{
    private RectangleDirectionType DirectionType { get; }

    public RectangleConverter(RectangleDirectionType directionType)
    {
        this.DirectionType = directionType;
    }

    public override Rectangle ReadJson(JsonReader reader, Type objectType, Rectangle existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        serializer.Converters.Add(new CoordinatesConverter());

        if (reader.TokenType != JsonToken.StartArray)
        {
            throw new JsonException("Expected start of array");
        }

        Coordinates2[] values = new Coordinates2[2];
        for (int i = 0; i < 2; i++)
        {
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of array");
            }

            values[i] = serializer.Deserialize<Coordinates2>(reader);
        }

        if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
        {
            throw new JsonException("Expected end of array");
        }

        return new Rectangle(values[0], values[1], this.DirectionType);
    }

    public override void WriteJson(JsonWriter writer, Rectangle value, JsonSerializer serializer)
    {
        writer.WriteStartArray();

        serializer.Serialize(writer, value.BottomLeft);
        serializer.Serialize(writer, value.TopRight);

        writer.WriteEndArray();
    }
}
