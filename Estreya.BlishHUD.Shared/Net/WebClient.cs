namespace Estreya.BlishHUD.Shared.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

internal class WebClient : System.Net.WebClient
{
    protected override WebRequest GetWebRequest(Uri address)
    {
        // Default WebClient deletes user agent each time.

        var userAgent = this.Headers.Get("User-Agent");
        var webRequest =  base.GetWebRequest(address);
        this.Headers.Set(HttpRequestHeader.UserAgent, userAgent);

        if (webRequest is HttpWebRequest httpWebRequest)
        {
            httpWebRequest.UserAgent = userAgent;
        }

        return webRequest;
    }
}
