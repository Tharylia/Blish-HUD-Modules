namespace Estreya.BlishHUD.LiveMap.Models.Player;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class PlayerFacing
{
    [JsonPropertyName("angle")]
    public double Angle { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not PlayerFacing playerFacing)
        {
            return false;
        }

        var equals = true;

        equals &= this.Angle == playerFacing.Angle;

        return equals;
    }
}
