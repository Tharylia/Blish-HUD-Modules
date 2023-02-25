namespace Estreya.BlishHUD.LiveMap.Models.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Blish_HUD.ArcDps.ArcDpsEnums;

public class PlayerGroup
{
    [JsonPropertyName("squad")]
    public string[] Squad { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not PlayerGroup playerGroup)
        {
            return false;
        }
        var equals = true;
        equals &= this.Squad != null && playerGroup.Squad != null ? Enumerable.SequenceEqual(this.Squad, playerGroup.Squad) : this.Squad is null && playerGroup.Squad is null;

        return equals;
    }
}
