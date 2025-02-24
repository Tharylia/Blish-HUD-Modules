namespace Estreya.BlishHUD.EventTable.Models;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventWaypoints
{
    [JsonProperty("EU")]
    public string EU;

    [JsonProperty("NA")]
    public string NA;
}
