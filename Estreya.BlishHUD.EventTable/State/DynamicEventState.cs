namespace Estreya.BlishHUD.EventTable.State;

using Estreya.BlishHUD.Shared.State;
using Flurl.Http;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DynamicEventState : ManagedState
{
    private const string BASE_URL = "https://api.guildwars2.com/v1";
    private readonly IFlurlClient _flurlClient;

    public Map[] Maps { get; private set; }
    public DynamicEvent[] Events { get; private set; }

    public DynamicEventState(StateConfiguration configuration, IFlurlClient flurlClient) : base(configuration)
    {
        this._flurlClient = flurlClient;
    }

    protected override Task Initialize()
    {
        return Task.CompletedTask;
    }

    protected override void InternalUnload()
    {
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
    }

    protected override async Task Load()
    {
        this.Maps = await this.GetMaps();
        this.Events = await this.GetEvents();
    }

    public Map GetMap(int id)
    {
        return this.Maps.Where(m => m.ID == id).FirstOrDefault();
    }

    public DynamicEvent[] GetEventsByMap(int mapId)
    {
        return this.Events.Where(e => e.MapId == mapId).ToArray();
    }

    public DynamicEvent GetEventById(string eventId)
    {
        return this.Events.Where(e => e.ID == eventId).FirstOrDefault();
    }

    private async Task<Map[]> GetMaps()
    {
        var request = this._flurlClient.Request(BASE_URL, "map_names.json").SetQueryParam("lang", "en");

        var maps = await request.GetJsonAsync<Map[]>();

        return maps;
    }

    private async Task<DynamicEvent[]> GetEvents()
    {
        var request = this._flurlClient.Request(BASE_URL, "event_details.json").SetQueryParam("lang", "en");

        var eventJson = await request.GetStringAsync();
        var events = JsonConvert.DeserializeAnonymousType(eventJson, new
        {
            events = new Dictionary<string, DynamicEvent>()
        });

        return events.events.Select(x =>
        {
            x.Value.ID = x.Key;

            return x.Value;
        }).ToArray();
    }

    public class Map
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class DynamicEvent
    {
        [JsonIgnore]
        public string ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("map_id")]
        public int MapId { get; set; }

        [JsonProperty("flags")]
        public string[] Flags { get; set; }

        [JsonProperty("location")]
        public DynamicEventLocation Location { get; set; }

        public class DynamicEventLocation
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("center")]
            public double[] Center { get; set; }

            [JsonProperty("radius")]
            public double Radius { get; set; }

            [JsonProperty("rotation")]
            public double Rotation { get; set; }

            [JsonProperty("z_range")]
            public double[] ZRange { get; set; }

            [JsonProperty("points")]
            public double[][] Points { get; set; }
        }
    }
}
