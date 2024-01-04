namespace Estreya.BlishHUD.ArcDPSLogManager.Models;

using Estreya.BlishHUD.ArcDPSLogManager.Models.Enums;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;

public class LogPlayer
{
    [JsonProperty("character")]
    public string CharacterName { get; set; }

    [JsonProperty("account")]
    public string AccountName { get; set; }

    [JsonProperty("subgroup")]
    public int Subgroup { get; set; }

    [JsonProperty("profession")]
    public Profession Profession { get; set; }

    [JsonProperty("eliteSpecialization")]
    public EliteSpecialization EliteSpecialization { get; set; }

    [JsonProperty("guild")]
    public string GuildGuid { get; set; }

    [JsonProperty("tag")]
    public PlayerTag Tag { get; set; }

    public LogPlayer(string characterName, string accountName, int subgroup, Profession profession, EliteSpecialization eliteSpecialization, string guildGuid)
    {
        this.CharacterName = characterName;
        this.AccountName = accountName;
        this.Subgroup = subgroup;
        this.Profession = profession;
        this.EliteSpecialization = eliteSpecialization;
        this.GuildGuid = guildGuid;
    }
}
