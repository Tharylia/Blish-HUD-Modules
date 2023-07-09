namespace Estreya.BlishHUD.Shared.Json.Converter;

using Gw2Sharp;
using Gw2Sharp.WebApi;
using Newtonsoft.Json;
using System;
using Utils;

public class RenderUrlConverter : JsonConverter<RenderUrl>
{
    private readonly IConnection _connection;

    public RenderUrlConverter(IConnection connection)
    {
        this._connection = connection;
    }

    public override RenderUrl ReadJson(JsonReader reader, Type objectType, RenderUrl existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return default; // This ignores the constructor.
        }

        if (reader.TokenType is not JsonToken.String)
        {
            throw new JsonException("Expected a string value");
        }

        return Gw2SharpHelper.CreateRenderUrl(this._connection, reader.Value as string);
    }

    public override void WriteJson(JsonWriter writer, RenderUrl value, JsonSerializer serializer)
    {
        // Save render url only as string. The rest is not needed.
        writer.WriteValue(value.Url?.AbsoluteUri);
    }
}