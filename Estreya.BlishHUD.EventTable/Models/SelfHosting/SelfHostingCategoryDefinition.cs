namespace Estreya.BlishHUD.EventTable.Models.SelfHosting;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SelfHostingCategoryDefinition
{
    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("zones")]
    public List<SelfHostingZoneDefinition> Zones { get; set; }

}
