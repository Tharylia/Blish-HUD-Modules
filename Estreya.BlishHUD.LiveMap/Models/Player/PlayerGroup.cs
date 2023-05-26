namespace Estreya.BlishHUD.LiveMap.Models.Player;

using System.Linq;
using System.Text.Json.Serialization;

public class PlayerGroup
{
    [JsonPropertyName("squad")] public string[] Squad { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not PlayerGroup playerGroup)
        {
            return false;
        }

        bool equals = true;
        equals &= this.Squad != null && playerGroup.Squad != null ? this.Squad.SequenceEqual(playerGroup.Squad) : this.Squad is null && playerGroup.Squad is null;

        return equals;
    }
}