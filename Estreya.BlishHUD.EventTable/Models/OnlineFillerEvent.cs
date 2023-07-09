namespace Estreya.BlishHUD.EventTable.Models;

using Newtonsoft.Json;
using System;

internal class OnlineFillerEvent
{
    [JsonProperty("key")] public string Key { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("duration")] public int Duration { get; set; }

    [JsonProperty("occurences")] public DateTimeOffset[] Occurences { get; set; }
}