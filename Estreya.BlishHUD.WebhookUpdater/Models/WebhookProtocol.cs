namespace Estreya.BlishHUD.WebhookUpdater.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

public class WebhookProtocol
{
    public DateTime TimestampUTC { get; } = DateTime.UtcNow;

    public string Url { get; set; }

    public string Message { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public HTTPMethod Method { get; set; }

    public string Payload { get; set; }

    public string ContentType { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public HttpStatusCode StatusCode { get; set; }

    public ProtocolException Exception { get; set; }

    public class ProtocolException
    {
        public string Message { get; set; }
        public string Stacktrace { get; set; }

        public ProtocolException(Exception exception)
        {
            this.Message = exception?.Message;
            this.Stacktrace = exception?.StackTrace;
        }
    }
}
