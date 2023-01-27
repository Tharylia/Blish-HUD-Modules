namespace Estreya.BlishHUD.LiveMap.Models.Player;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class PlayerIdentification
{
    [JsonPropertyName("account")]
    public string Account { get; set; }
    [JsonPropertyName("character")]
    public string Character { get; set; }

    [JsonPropertyName("guild")]
    public string GuildId { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not PlayerIdentification playerIdentification)
        {
            return false;
        }

        var equals = true;

        equals &= this.Account == playerIdentification.Account;
        equals &= this.Character == playerIdentification.Character;
        equals &= this.GuildId == playerIdentification.GuildId;

        return equals;
    }
}
