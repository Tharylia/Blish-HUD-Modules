namespace Estreya.BlishHUD.Shared.Models.BlishHudAPI
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class KofiStatus
    {
        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("lastPayment")]
        public DateTimeOffset? LastPayment { get; set; }
    }
}
