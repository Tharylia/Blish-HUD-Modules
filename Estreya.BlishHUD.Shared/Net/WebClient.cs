namespace Estreya.BlishHUD.Shared.Net;

using System;
using System.Net;

public class WebClient : System.Net.WebClient
{
    protected override WebRequest GetWebRequest(Uri address)
    {
        // Default WebClient deletes user agent each time.

        string userAgent = this.Headers.Get("User-Agent");
        WebRequest webRequest = base.GetWebRequest(address);
        this.Headers.Set(HttpRequestHeader.UserAgent, userAgent);

        if (webRequest is HttpWebRequest httpWebRequest)
        {
            httpWebRequest.AllowAutoRedirect = true;
            httpWebRequest.UserAgent = userAgent;
        }

        return webRequest;
    }
}