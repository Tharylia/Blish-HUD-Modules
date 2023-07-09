namespace Estreya.BlishHUD.EventTable.Models.Settings;

using Newtonsoft.Json;
using SemVer;
using Shared.Json.Converter;
using System.Collections.Generic;

public class EventSettingsFile
{
    [JsonProperty("version")]
    [JsonConverter(typeof(SemanticVersionConverter))]
    public Version Version { get; set; } = new Version(0, 0, 0);

    [JsonProperty("moduleVersion")]
    [JsonConverter(typeof(SemanticRangeConverter))]
    public Range MinimumModuleVersion { get; set; } = new Range(">=0.0.0");

    [JsonProperty("eventCategories")] public List<EventCategory> EventCategories { get; set; }
}