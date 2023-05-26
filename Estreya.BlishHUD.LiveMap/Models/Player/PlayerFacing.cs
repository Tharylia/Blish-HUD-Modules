namespace Estreya.BlishHUD.LiveMap.Models.Player;

using System.Text.Json.Serialization;

public class PlayerFacing
{
    [JsonPropertyName("angle")] public double Angle { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not PlayerFacing playerFacing)
        {
            return false;
        }

        bool equals = true;

        equals &= this.Angle == playerFacing.Angle;

        return equals;
    }
}