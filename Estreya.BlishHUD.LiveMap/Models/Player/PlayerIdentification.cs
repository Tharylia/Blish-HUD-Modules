namespace Estreya.BlishHUD.LiveMap.Models.Player;

using System.Text.Json.Serialization;

public class PlayerIdentification
{
    [JsonPropertyName("account")] public string Account { get; set; }

    [JsonPropertyName("character")] public string Character { get; set; }

    [JsonPropertyName("guild")] public string GuildId { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not PlayerIdentification playerIdentification)
        {
            return false;
        }

        bool equals = true;

        equals &= this.Account == playerIdentification.Account;
        equals &= this.Character == playerIdentification.Character;
        equals &= this.GuildId == playerIdentification.GuildId;

        return equals;
    }
}