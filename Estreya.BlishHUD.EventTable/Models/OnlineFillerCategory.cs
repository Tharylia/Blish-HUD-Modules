namespace Estreya.BlishHUD.EventTable.Models;

using Newtonsoft.Json;

internal class OnlineFillerCategory
{
    [JsonProperty("key")] public string Key { get; set; }

    [JsonProperty("fillers")] public OnlineFillerEvent[] Fillers { get; set; }
}