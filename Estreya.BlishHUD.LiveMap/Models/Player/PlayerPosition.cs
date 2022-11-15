namespace Estreya.BlishHUD.LiveMap.Models.Player;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class PlayerPosition
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not PlayerPosition playerPosition)
        {
            return false;
        }

        var equals = true;

        equals &= this.X == playerPosition.X;
        equals &= this.Y == playerPosition.Y;

        return equals;
    }
}
