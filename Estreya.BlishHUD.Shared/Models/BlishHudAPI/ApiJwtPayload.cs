namespace Estreya.BlishHUD.Shared.Models.BlishHudAPI;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct ApiJwtPayload
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; }

    [JsonProperty("exp")]
    public int Expiration { get; set; }
}
