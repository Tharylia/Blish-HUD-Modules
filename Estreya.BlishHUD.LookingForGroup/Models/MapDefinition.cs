namespace Estreya.BlishHUD.LookingForGroup.Models;

using Estreya.BlishHUD.LookingForGroup.Controls;
using Newtonsoft.Json;
using System;

public class MapDefinition
{
    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("mapId")]
    public int MapId { get; set; } = -1;

    [JsonProperty("icon")]
    public string Icon { get; set; }

    [JsonIgnore]
    public WeakReference<CategoryDefinition> Category { get; private set; }

    public void Load(CategoryDefinition category)
    {
        this.Category = new WeakReference<CategoryDefinition>(category);
    }
}