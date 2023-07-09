namespace Estreya.BlishHUD.WebhookUpdater.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Net;

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
        public ProtocolException(Exception exception)
        {
            this.Message = exception?.Message;
            this.Stacktrace = exception?.StackTrace;
        }

        public string Message { get; set; }
        public string Stacktrace { get; set; }
    }
}