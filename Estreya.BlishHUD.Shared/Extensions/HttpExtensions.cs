namespace Estreya.BlishHUD.Shared.Extensions;

using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public static class HttpExtensions
{
    public static async Task<T> GetJsonAsync<T>(this HttpResponseMessage responseMessage)
    {
        return await GetJsonAsync<T>(responseMessage, JsonSerializer.Create(JsonConvert.DefaultSettings?.Invoke() ?? null));
    }

    public static async Task<T> GetJsonAsync<T>(this HttpResponseMessage responseMessage, JsonSerializer serializer)
    {
        using Stream stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);

        using StreamReader sr = new StreamReader(stream);
        using JsonTextReader jsonTextReader = new JsonTextReader(sr);

        return serializer.Deserialize<T>(jsonTextReader);
    }
}