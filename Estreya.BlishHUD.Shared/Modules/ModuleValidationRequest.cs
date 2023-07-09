namespace Estreya.BlishHUD.Shared.Modules;

using Json.Converter;
using Newtonsoft.Json;
using SemVer;

public struct ModuleValidationRequest
{
    [JsonProperty("version")] [JsonConverter(typeof(SemanticVersionConverter))]
    public Version Version;
}