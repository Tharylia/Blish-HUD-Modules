namespace Estreya.BlishHUD.ArcDPSLogManager.Models;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PointOfView
{
    [JsonProperty("character")]
    public string CharacterName { get; set; }

    [JsonProperty(propertyName: "account")]
    public string AccountName { get; set; }
}
