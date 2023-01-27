namespace Estreya.BlishHUD.EventTable.Models;

using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class OnlineFillerRequest
{
    [JsonProperty("module")]
    public OnlineFillerRequestModule Module { get; set; }

    [JsonProperty("times")]
    public OnlineFillerRequestTimes Times { get; set; }

    [JsonProperty("eventKeys")]
    public string[] EventKeys { get; set; }

    public class OnlineFillerRequestModule
    {
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class OnlineFillerRequestTimes
    {
        [JsonProperty("now")]
        public string Now_UTC_ISO { get; set; }

        [JsonProperty("min")]
        public string Min_UTC_ISO { get; set; }

        [JsonProperty("max")]
        public string Max_UTC_ISO { get; set; }
    }
}