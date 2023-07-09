namespace Estreya.BlishHUD.FoodReminder.Models;

using Newtonsoft.Json;

public class FoodDefinition
{
    [JsonProperty("id")] public int ID { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("display")] public string Display { get; set; }

    [JsonProperty("stats")] public string[] Stats { get; set; }
}