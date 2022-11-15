namespace Estreya.BlishHUD.LiveMap.Models.Player;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class Player
{
    [JsonPropertyName("identification")]
    public PlayerIdentification Identification { get; set; }

    [JsonPropertyName("position")]
    public PlayerPosition Position { get; set; }

    [JsonPropertyName("facing")]
    public PlayerFacing Facing { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not Player player)
        {
            return false;
        }

        var equals = true;

        equals &= this.Identification.Equals(player.Identification);
        equals &= this.Position.Equals(player.Position);
        equals &= this.Facing.Equals(player.Facing);

        return equals;
    }
}
