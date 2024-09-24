namespace Estreya.BlishHUD.Shared.Models.BlishHudAPI
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class APIError
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
