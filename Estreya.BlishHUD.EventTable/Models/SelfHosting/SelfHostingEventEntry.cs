namespace Estreya.BlishHUD.EventTable.Models.SelfHosting;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SelfHostingEventEntry
{
    [JsonProperty("categoryKey")]
    public string CategoryKey { get; set; }

    [JsonProperty("zoneKey")]
    public string ZoneKey { get; set; }

    [JsonProperty("eventKey")]
    public string EventKey { get; set; }

    [JsonProperty("accountName")]
    public string AccountName { get; set; }

    [JsonProperty("instanceIP")]
    public string InstanceIP { get; set; }

    [JsonProperty("startTime")]
    public DateTimeOffset StartTime { get; set; }

    [JsonProperty("duration")]
    public int Duration { get; set; }
}
