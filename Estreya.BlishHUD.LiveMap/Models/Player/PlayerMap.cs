namespace Estreya.BlishHUD.LiveMap.Models.Player;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class PlayerMap
{
    [JsonPropertyName("continent")]
    public int Continent { get; set; }

    [JsonPropertyName("id")]
    public int ID { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("position")]

    public PlayerPosition Position { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not PlayerMap playerMap)
        {
            return false;
        }

        var equals = true;

        equals &= this.Continent.Equals(playerMap.Continent);
        equals &= this.ID.Equals(playerMap.ID);
        equals &= this.Name?.Equals(playerMap.Name) ?? this.Name is null && playerMap.Name is null;
        equals &= this.Position?.Equals(playerMap.Position) ?? this.Position is null && playerMap.Position is null;

        return equals;
    }
}
