namespace Estreya.BlishHUD.LiveMap.Models.Player;

using System.Text.Json.Serialization;

public class PlayerWvW
{
    [JsonPropertyName("match")] public string Match { get; set; }

    [JsonPropertyName("teamColor")] public string TeamColor { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not PlayerWvW playerWvW)
        {
            return false;
        }

        bool equals = true;

        equals &= this.Match == playerWvW.Match;
        equals &= this.TeamColor == playerWvW.TeamColor;

        return equals;
    }
}