namespace Estreya.BlishHUD.EventTable.Models;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventTimers
{
    [JsonProperty("mapId")]
    public int MapID;

    [JsonProperty("map")]
    public EventMapTimer[] Map;

    [JsonProperty("world")]
    public EventWorldTimer[] World;
}
