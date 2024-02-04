namespace Estreya.BlishHUD.EventTable.Models;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SelfHostedEventEntry
{
    [JsonProperty("userGuid")]
    public string UserGUID { get; set; }

    [JsonProperty("eventKey")]
    public string EventKey { get; set; }

    [JsonProperty("eventName")]
    public string EventName { get; set; }

    [JsonProperty("startTime")]
    public DateTimeOffset StartTime { get; set; }

    [JsonProperty("duration")]
    public int Duration { get; set; }
}
