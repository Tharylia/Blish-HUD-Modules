namespace Estreya.BlishHUD.EventTable.Models.SelfHosting;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SelfHostingEventDefinition
{
    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("wikiUrl")]
    public string WikiUrl { get; set; }
}
