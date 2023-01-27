namespace Estreya.BlishHUD.EventTable.Models;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class OnlineFillerCategory
{
    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("fillers")]
    public OnlineFillerEvent[] Fillers { get; set; }
}
