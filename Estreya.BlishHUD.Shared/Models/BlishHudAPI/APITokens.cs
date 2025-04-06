namespace Estreya.BlishHUD.Shared.Models.BlishHudAPI;

using Newtonsoft.Json;

public struct APITokens
{
    [JsonProperty("access_token")] public string AccessToken { get; set; }

    [JsonProperty("refresh_token")] public string RefreshToken { get; set; }
}