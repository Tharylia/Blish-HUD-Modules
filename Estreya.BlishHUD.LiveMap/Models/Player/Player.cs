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

    [JsonPropertyName("map")]
    public PlayerMap Map { get; set; }

    [JsonPropertyName("facing")]
    public PlayerFacing Facing { get; set; }

    [JsonPropertyName("wvw")]
    public PlayerWvW WvW { get; set; }

    [JsonPropertyName("group")]
    public PlayerGroup Group { get; set; }

    [JsonPropertyName("commander")]
    public bool Commander { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not Player player)
        {
            return false;
        }

        var equals = true;

        equals &= this.Identification.Equals(player.Identification);
        equals &= this.Map.Equals(player.Map);
        equals &= this.Facing.Equals(player.Facing);
        equals &= this.Commander.Equals(player.Commander);
        equals &= this.Group.Equals(player.Group);
        equals &= this.WvW.Equals(player.WvW);

        return equals;
    }
}
