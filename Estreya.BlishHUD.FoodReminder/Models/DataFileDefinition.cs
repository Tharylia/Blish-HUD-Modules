namespace Estreya.BlishHUD.FoodReminder.Models;

using Newtonsoft.Json;
using System.Collections.Generic;

public struct DataFileDefinition
{
    [JsonProperty("food")] public List<FoodDefinition> Food { get; set; }

    [JsonProperty("utility")] public List<UtilityDefinition> Utility { get; set; }

    [JsonProperty("ignore")] public List<int> IgnoredBuffIds { get; set; }

    [JsonProperty("reinforcedSkillId")] public int ReinforcedSkillId { get; set; }
}