namespace Estreya.BlishHUD.EventTable.Services;

using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Threading.Events;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using Newtonsoft.Json;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Estreya.BlishHUD.EventTable.Models;

public partial class DynamicEventService : APIService<DynamicEvent>
{
    private readonly string _apiBaseUrl;
    private readonly string _directoryBasePath;
    private readonly IFlurlClient _flurlClient;

    public event AsyncEventHandler CustomEventsUpdated;

    public DynamicEventService(APIServiceConfiguration configuration, Gw2ApiManager apiManager, IFlurlClient flurlClient, string apiBaseUrl, string directoryBasePath) : base(apiManager, configuration)
    {
        this._flurlClient = flurlClient;
        this._apiBaseUrl = apiBaseUrl;
        this._directoryBasePath = directoryBasePath;
    }

    private string API_URL => $"{this._apiBaseUrl.TrimEnd('/')}/v1/gw2/dynamicEvents";

    private List<DynamicEvent> _customEvents = new List<DynamicEvent>();
    private bool _loadedFiles;

    public List<DynamicEvent> Events => this.APIObjectList.Concat(this._customEvents).ToList();

    private const string BASE_FOLDER_STRUCTURE = "dynamic_events";

    private const string FILE_NAME = "custom.json";

    public DynamicEvent[] GetEventsByMap(int mapId)
    {
        return this.Events?.Where(e => e.MapId == mapId).ToArray();
    }

    public DynamicEvent GetEventById(string eventId)
    {
        return this.Events?.Where(e => e.ID == eventId).FirstOrDefault();
    }

    private async Task<List<DynamicEvent>> GetEvents()
    {
        IFlurlRequest request = this._flurlClient.Request(this.API_URL).SetQueryParam("lang", "en"); // Language is ignored for now

        string eventJson = await request.GetStringAsync();
        List<DynamicEvent> events = JsonConvert.DeserializeObject<List<DynamicEvent>>(eventJson);

        return events;
    }

    public async Task AddCustomEvent(DynamicEvent dynamicEvent)
    {
        this._customEvents.RemoveAll(e => e.ID == dynamicEvent.ID);

        dynamicEvent.IsCustom = true;

        this._customEvents.Add(dynamicEvent);

        await this.Save();
        var oldList = new List<DynamicEvent>(this.APIObjectList);
        this.APIObjectList.Clear();
        this.APIObjectList.AddRange(this.FilterCustomizedEvents(oldList));
    }

    public async Task RemoveCustomEvent(string id)
    {
        var existingEvent = this.GetEventById(id);
        if (existingEvent is null || !existingEvent.IsCustom) return;

        this._customEvents.Remove(existingEvent);
        await this.Save();
        await this.Load();
    }

    private List<DynamicEvent> FilterCustomizedEvents(IEnumerable<DynamicEvent> events)
    {
        return events.Where(e => !this._customEvents.Any(ce => ce.ID == e.ID)).ToList();
    }

    public async Task NotifyCustomEventsUpdated()
    {
        await (this.CustomEventsUpdated?.Invoke(this) ?? Task.CompletedTask);
    }

    protected override async Task<List<DynamicEvent>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var events = await this.GetEvents();

        events = this.FilterCustomizedEvents(events);

        return events;
    }

    protected override async Task Save()
    {
        var directoryPath = Path.Combine(this._directoryBasePath, BASE_FOLDER_STRUCTURE);
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

        var filePath = Path.Combine(directoryPath, FILE_NAME);

        var json = JsonConvert.SerializeObject(this._customEvents, Formatting.Indented);
        await FileUtil.WriteStringAsync(filePath, json);
    }

    protected override async Task Load()
    {
        if (!this._loadedFiles)
        {
            var filePath = Path.Combine(this._directoryBasePath, BASE_FOLDER_STRUCTURE, FILE_NAME);
            if (File.Exists(filePath))
            {
                var json = await FileUtil.ReadStringAsync(filePath);
                var customEvents = JsonConvert.DeserializeObject<List<DynamicEvent>>(json);
                this._customEvents =customEvents;

                this._customEvents.ForEach(ce => ce.IsCustom = true);
            }

            this._loadedFiles = true;
        }

        await base.Load();
    }
}