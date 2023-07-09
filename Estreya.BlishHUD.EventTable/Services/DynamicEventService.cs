namespace Estreya.BlishHUD.EventTable.Services;

using Blish_HUD.Modules.Managers;
using Flurl.Http;
using Newtonsoft.Json;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DynamicEventService : APIService
{
    private readonly string _apiBaseUrl;
    private readonly IFlurlClient _flurlClient;

    public DynamicEventService(APIServiceConfiguration configuration, Gw2ApiManager apiManager, IFlurlClient flurlClient, string apiBaseUrl) : base(apiManager, configuration)
    {
        this._flurlClient = flurlClient;
        this._apiBaseUrl = apiBaseUrl;
    }

    private string API_URL => $"{this._apiBaseUrl.TrimEnd('/')}/v1/gw2/dynamicEvents";

    public DynamicEvent[] Events { get; private set; } = new DynamicEvent[0];

    public DynamicEvent[] GetEventsByMap(int mapId)
    {
        return this.Events?.Where(e => e.MapId == mapId).ToArray();
    }

    public DynamicEvent GetEventById(string eventId)
    {
        return this.Events?.Where(e => e.ID == eventId).FirstOrDefault();
    }

    private async Task<DynamicEvent[]> GetEvents()
    {
        IFlurlRequest request = this._flurlClient.Request(this.API_URL).SetQueryParam("lang", "en"); // Language is ignored for now

        string eventJson = await request.GetStringAsync();
        List<DynamicEvent> events = JsonConvert.DeserializeObject<List<DynamicEvent>>(eventJson);

        return events.ToArray();
    }

    protected override async Task FetchFromAPI(Gw2ApiManager apiManager, IProgress<string> progress)
    {
        try
        {
            progress.Report("Loading events..");
            this.Events = await this.GetEvents();
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed loading events:");
        }
    }

    public class DynamicEvent
    {
        [JsonProperty("id")] public string ID { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("level")] public int Level { get; set; }

        [JsonProperty("map_id")] public int MapId { get; set; }

        [JsonProperty("flags")] public string[] Flags { get; set; }

        [JsonProperty("location")] public DynamicEventLocation Location { get; set; }

        [JsonProperty("icon")] public DynamicEventIcon Icon { get; set; }

        public class DynamicEventLocation
        {
            [JsonProperty("type")] public string Type { get; set; }

            [JsonProperty("center")] public float[] Center { get; set; }

            [JsonProperty("radius")] public float Radius { get; set; }

            /// <summary>
            ///     Height defines the total height
            /// </summary>
            [JsonProperty("height")]
            public float Height { get; set; }

            [JsonProperty("rotation")] public float Rotation { get; set; }

            /// <summary>
            ///     Z Ranges defines the top and bottom boundaries offset from the center z.
            /// </summary>
            [JsonProperty("z_range")]
            public float[] ZRange { get; set; }

            [JsonProperty("points")] public float[][] Points { get; set; }
        }

        public class DynamicEventIcon
        {
            [JsonProperty("file_id")] public int FileID { get; set; }

            [JsonProperty("signature")] public string Signature { get; set; }
        }
    }
}