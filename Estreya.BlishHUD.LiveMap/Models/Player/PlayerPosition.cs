namespace Estreya.BlishHUD.LiveMap.Models.Player;

using System.Text.Json.Serialization;

public class PlayerPosition
{
    [JsonPropertyName("x")] public double X { get; set; }

    [JsonPropertyName("y")] public double Y { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not PlayerPosition playerPosition)
        {
            return false;
        }

        bool equals = true;

        equals &= this.X == playerPosition.X;
        equals &= this.Y == playerPosition.Y;

        return equals;
    }
}