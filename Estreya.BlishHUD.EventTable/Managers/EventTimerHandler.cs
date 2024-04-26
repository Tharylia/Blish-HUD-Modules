namespace Estreya.BlishHUD.EventTable.Managers;

using Blish_HUD;
using Blish_HUD.Entities;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Controls.Map;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.Shared.Services;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Services;
using Shared.Controls.Map;
using Shared.Controls.World;
using Shared.Extensions;
using Shared.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Services.DynamicEventService;
using Color = Microsoft.Xna.Framework.Color;

public class EventTimerHandler : IDisposable, IUpdatable
{
    private static readonly Logger Logger = Logger.GetLogger<EventTimerHandler>();

    private static TimeSpan _checkLostEntitiesInterval = TimeSpan.FromSeconds(5);
    private double _lastLostEntitiesCheck;
    private bool _notifiedLostEntities;

    private readonly Gw2ApiManager _apiManager;
    private readonly Func<Task<List<Event>>> _getEvents;
    private readonly Func<DateTime> _getNow;
    private readonly MapUtil _mapUtil;
    private readonly ModuleSettings _moduleSettings;
    private readonly TranslationService _translationService;

    private readonly ConcurrentQueue<(string Key, bool Add)> _entityQueue = new ConcurrentQueue<(string Key, bool Add)>();

    private readonly ConcurrentDictionary<string, MapEntity> _mapEntities = new ConcurrentDictionary<string, MapEntity>();
    private readonly ConcurrentDictionary<string, List<WorldEntity>> _worldEntities = new ConcurrentDictionary<string, List<WorldEntity>>();

    public event EventHandler FoundLostEntities;

    public EventTimerHandler(Func<Task<List<Event>>> getEvents, Func<DateTime> getNow, MapUtil mapUtil, Gw2ApiManager apiManager, ModuleSettings moduleSettings, TranslationService translationService)
    {
        this._getEvents = getEvents;
        this._getNow = getNow;
        this._mapUtil = mapUtil;
        this._apiManager = apiManager;
        this._moduleSettings = moduleSettings;
        this._translationService = translationService;
        GameService.Gw2Mumble.CurrentMap.MapChanged += this.CurrentMap_MapChanged;
    }

    public void Update(GameTime gameTime)
    {
        UpdateUtil.Update(this.CheckLostEntityReferences, gameTime, _checkLostEntitiesInterval.TotalMilliseconds, ref this._lastLostEntitiesCheck);

        while (this._entityQueue.TryDequeue(out (string Key, bool Add) element))
        {
            try
            {
                //DynamicEvent dynamicEvent = this._dynamicEventService.Events.Where(e => e.ID == element.Key).First();
                if (element.Add)
                {
                    _ = Task.Run(async () =>
                    {
                        //await this.AddDynamicEventToMap(dynamicEvent);
                        //await this.AddDynamicEventToWorld(dynamicEvent);
                    });
                }
                else
                {
                    //this.RemoveDynamicEventFromMap(dynamicEvent);
                    //this.RemoveDynamicEventFromWorld(dynamicEvent);
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed updating event {element.Key}");
            }
        }
    }

    private async void CurrentMap_MapChanged(object sender, ValueEventArgs<int> e)
    {
        await this.AddEventTimersToMap();
        await this.AddEventTimersToWorld();
    }

    private async Task<List<Event>> GetEventForMap(int mapId)
    {
        var allEvents = await this._getEvents();
        var events = allEvents.Where(ev => ev.MapIds.Contains(mapId)).ToList();

        return events;
    }

    public async Task AddEventTimersToMap()
    {
        try
        {
            this._mapEntities?.Values.ToList().ForEach(m => this._mapUtil.RemoveEntity(m));
            this._mapEntities?.Clear();

            if (!this._moduleSettings.ShowEventTimersOnMap.Value || !GameService.Gw2Mumble.IsAvailable)
            {
                return;
            }

            var mapId = GameService.Gw2Mumble.CurrentMap.Id;
            var events = await this.GetEventForMap(mapId);
            if (events == null || events.Count == 0)
            {
                Logger.Debug($"No events found for map {mapId}");
                return;
            }

            foreach (var ev in events)
            {
                await this.AddEventTimerToMap(ev);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to add dynamic events to map.");
        }
    }

    private void RemoveEventTimerFromMap(Event ev)
    {
        if (this._mapEntities.ContainsKey(ev.SettingKey))
        {
            this._mapUtil.RemoveEntity(this._mapEntities[ev.SettingKey]);
            this._mapEntities.TryRemove(ev.SettingKey, out _);
        }
    }

    public async Task AddEventTimerToMap(Event ev)
    {
        this.RemoveEventTimerFromMap(ev);

        if (!this._moduleSettings.ShowEventTimersOnMap.Value || !GameService.Gw2Mumble.IsAvailable)
        {
            return;
        }

        try
        {
            MapEntity circle = this._mapUtil.AddEntity(new EventTimer(ev, Color.DarkOrange, this._getNow, this._translationService, 3));
            circle.TooltipText = $"{ev.Name}";
            this._mapEntities.AddOrUpdate(ev.SettingKey, circle, (_, _) => circle);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, $"Failed to add {ev.SettingKey} to map.");
        }
    }

    public async Task AddEventTimersToWorld()
    {
        GameService.Graphics.World.RemoveEntities(this._worldEntities.Values.SelectMany(v => v));
        this._worldEntities?.Clear();

        if (!this._moduleSettings.ShowEventTimersInWorld.Value || !GameService.Gw2Mumble.IsAvailable)
        {
            return;
        }

        int mapId = GameService.Gw2Mumble.CurrentMap.Id;
        var events = await this.GetEventForMap(mapId);
        if (events == null || events.Count == 0)
        {
            Logger.Debug($"No events found for map {mapId}");
            return;
        }

        Stopwatch sw = Stopwatch.StartNew();
        foreach (var ev in events)
        {
            await this.AddEventTimerToWorld(ev);
        }

        sw.Stop();
        Logger.Debug($"Added events in {sw.ElapsedMilliseconds}ms");
    }

    private void RemoveEventTimerFromWorld(Event ev)
    {
        if (this._worldEntities.ContainsKey(ev.SettingKey))
        {
            GameService.Graphics.World.RemoveEntities(this._worldEntities[ev.SettingKey]);
            this._worldEntities.TryRemove(ev.SettingKey, out _);
        }
    }

    public async Task AddEventTimerToWorld(Event ev)
    {
        this.RemoveEventTimerFromWorld(ev);

        if (!this._moduleSettings.ShowEventTimersInWorld.Value || !GameService.Gw2Mumble.IsAvailable)
        {
            return;
        }

        try
        {
            //Map map = await this._apiManager.Gw2ApiClient.V2.Maps.GetAsync(dynamicEvent.MapId);
            //Vector2 centerAsMapCoords = new Vector2(dynamicEvent.Location.Center[0], dynamicEvent.Location.Center[1]);
            //Vector3 centerAsWorldMeters = map.MapCoordsToWorldMeters(new Vector2(centerAsMapCoords.X, centerAsMapCoords.Y));
            //centerAsWorldMeters.Z = Math.Abs(dynamicEvent.Location.Center[2].ToMeters());

            //List<WorldEntity> entites = new List<WorldEntity>();
            //switch (dynamicEvent.Location.Type)
            //{
            //    case "poly":
            //        entites.Add(this.GetPolygone(dynamicEvent, map, centerAsWorldMeters, this.WorldEventRenderCondition));
            //        break;
            //    case "sphere":
            //        entites.Add(this.GetSphere(dynamicEvent, map, centerAsWorldMeters, this.WorldEventRenderCondition));
            //        break;
            //    case "cylinder":
            //        entites.Add(this.GetCylinder(dynamicEvent, map, centerAsWorldMeters, this.WorldEventRenderCondition));
            //        break;
            //}

            //this._worldEntities.AddOrUpdate(dynamicEvent.ID, entites, (_, prev) => prev.Concat(entites).ToList());
            //GameService.Graphics.World.AddEntities(entites);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, $"Failed to add {ev.SettingKey} to world.");
        }
    }

    private void CheckLostEntityReferences()
    {
        IEnumerable<IEntity> lostEntities = GameService.Graphics.World.Entities.Where(e => e is WorldEntity);
        bool hasEntities = lostEntities.Any();

        if (!this._notifiedLostEntities && !this._moduleSettings.ShowEventTimersInWorld.Value && hasEntities)
        {
            try
            {
                this.FoundLostEntities?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception) { }

            this._notifiedLostEntities = true;
        }

        if (this._moduleSettings.ShowEventTimersInWorld.Value)
        {
            this._notifiedLostEntities = false;
        }
    }

    public void Dispose()
    {
        GameService.Graphics.World.RemoveEntities(this._worldEntities.Values.SelectMany(v => v));
        this._worldEntities?.Clear();

        this._mapEntities?.Values.ToList().ForEach(me => this._mapUtil.RemoveEntity(me));
        this._mapEntities?.Clear();

        GameService.Gw2Mumble.CurrentMap.MapChanged -= this.CurrentMap_MapChanged;
    }
}