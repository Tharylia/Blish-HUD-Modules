namespace Estreya.BlishHUD.EventTable.Models;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct EventMapLocation
{
    [JsonProperty("x")]
    public float X;

    [JsonProperty("y")]
    public float Y;

    [JsonProperty("radius")]
    public float Radius;
}
