namespace Estreya.BlishHUD.LookingForGroup.Models;

using Blish_HUD;
using Blish_HUD.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class LFGEntry
{
    [JsonProperty("categoryKey")]
    public string CategoryKey { get; set; }

    [JsonProperty("mapKey")]
    public string MapKey { get; set; }

    [JsonProperty("id")]
    public Guid ID { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("commander")]
    public Player Commander { get; set; }

    [JsonProperty("maxCount")]
    public int MaxCount { get; set; }

    [JsonProperty("players")]
    public Player[] Players { get; set; } = new Player[0];

}
