namespace Estreya.BlishHUD.Shared.Models.BlishHudAPI;

using Newtonsoft.Json;

public struct APITokens
{
    [JsonProperty("accessToken")] public string AccessToken { get; set; }

    [JsonProperty("refreshToken")] public string RefreshToken { get; set; }
}