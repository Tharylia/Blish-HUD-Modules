namespace Estreya.BlishHUD.FoodReminder.Models;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class UtilityDefinition
{
    [JsonProperty("id")]
    public int ID { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("display")]
    public string Display { get; set; }

    [JsonProperty("stats")]
    public string[] Stats { get; set; }
}
