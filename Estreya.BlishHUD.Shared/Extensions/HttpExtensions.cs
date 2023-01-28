namespace Estreya.BlishHUD.Shared.Extensions;

using Flurl.Http.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public static class HttpExtensions
{
    public static async Task<T> GetJsonAsync<T>(this HttpResponseMessage responseMessage)
    {
        using var stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);

        var serializer = new JsonSerializer();

        using var sr = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(sr);

        return serializer.Deserialize<T>(jsonTextReader);
    }
}
