namespace Estreya.BlishHUD.EventTable.Models.Settings
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class EventSettingsFile
    {
        [JsonProperty("version"), JsonConverter(typeof(Json.SemanticVersionConverter))]
        public SemVer.Version Version { get; set; } = new SemVer.Version(0, 0, 0);

        [JsonProperty("moduleVersion"), JsonConverter(typeof(Json.SemanticRangeConverter))]
        public SemVer.Range MinimumModuleVersion { get; set; } = new SemVer.Range(">=0.0.0");

        [JsonProperty("eventCategories")]
        public List<EventCategory> EventCategories { get; set; }
    }
}
