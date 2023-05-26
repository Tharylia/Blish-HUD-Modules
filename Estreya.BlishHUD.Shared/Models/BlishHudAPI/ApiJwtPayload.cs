namespace Estreya.BlishHUD.Shared.Models.BlishHudAPI;

using Newtonsoft.Json;

public struct ApiJwtPayload
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("username")] public string Username { get; set; }

    [JsonProperty("exp")] public int Expiration { get; set; }
}