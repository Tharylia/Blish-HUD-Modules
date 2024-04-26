namespace Estreya.BlishHUD.EventTable.Models;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct EventLocations
{
    [JsonProperty("tooltip")]
    public string Tooltip;

    [JsonProperty("map")]
    public EventMapLocation Map;

    [JsonProperty("world")]
    public float[] World;
}
