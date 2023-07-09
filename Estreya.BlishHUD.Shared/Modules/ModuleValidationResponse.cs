namespace Estreya.BlishHUD.Shared.Modules;

using Newtonsoft.Json;

public struct ModuleValidationResponse
{
    [JsonProperty("message")] public string Message { get; set; }
}