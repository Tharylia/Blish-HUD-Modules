namespace Estreya.BlishHUD.LookingForGroup.Models;

using Gw2Sharp;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Player
{
    [JsonProperty("accountName")]
    public string AccountName { get; set; }

    public int Profession { get; set; } = -1;
}
