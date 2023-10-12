namespace Estreya.BlishHUD.LookingForGroup.Models;

using Newtonsoft.Json;

public class CategoryDefinition
{
    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("icon")]
    public string Icon { get; set; }

    [JsonProperty("maps")]
    public MapDefinition[] Maps { get; set; }
}