namespace Estreya.BlishHUD.EventTable.Managers;

using Blish_HUD;
using Blish_HUD.Entities;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Controls.Map;
using Estreya.BlishHUD.EventTable.Controls.World;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.Shared.Services;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Services;
using Shared.Controls.Map;
using Shared.Controls.World;
using Shared.Extensions;
using Shared.Utils;
using Shared.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Radios;
using static Services.DynamicEventService;
using Color = Microsoft.Xna.Framework.Color;
using Humanizer;
using Microsoft.Xna.Framework.Graphics;
using Blish_HUD.Content;
using MonoGame.Extended;
using Blish_HUD.ArcDps.Models;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using Blish_HUD.Controls;
using Blish_HUD._Extensions;
using static Blish_HUD.ContentService;
using MonoGame.Extended.BitmapFonts;
using NodaTime;

public class EventTimerHandler : IDisposable, IUpdatable
{
    private static readonly Logger Logger = Logger.GetLogger<EventTimerHandler>();

    private static TimeSpan _checkLostEntitiesInterval = TimeSpan.FromSeconds(5);
    private double _lastLostEntitiesCheck;
    private static TimeSpan _readdInterval = TimeSpan.FromSeconds(5);
    private AsyncRef<double> _lastReadd = new AsyncRef<double>(0);
    private bool _notifiedLostEntities;
    private ConcurrentDictionary<FontSize, BitmapFont> _fonts = new ConcurrentDictionary<FontSize, BitmapFont>();

    private readonly Gw2ApiManager _apiManager;
    private readonly Func<Task<List<Event>>> _getEvents;
    private readonly Func<Instant> _getNow;
    private readonly MapUtil _mapUtil;
    private readonly ModuleSettings _moduleSettings;
    private readonly TranslationService _translationService;
    private readonly IconService _iconService;
    private readonly ConcurrentQueue<(string Key, bool Add)> _entityQueue = new ConcurrentQueue<(string Key, bool Add)>();

    private readonly ConcurrentDictionary<string, List<MapEntity>> _mapEntities = new ConcurrentDictionary<string, List<MapEntity>>();
    private readonly ConcurrentDictionary<string, List<WorldEntity>> _worldEntities = new ConcurrentDictionary<string, List<WorldEntity>>();

    public event EventHandler FoundLostEntities;

    public EventTimerHandler(Func<Task<List<Event>>> getEvents, Func<Instant> getNow, MapUtil mapUtil, Gw2ApiManager apiManager, ModuleSettings moduleSettings, TranslationService translationService, IconService iconService)
    {
        this._getEvents = getEvents;
        this._getNow = getNow;
        this._mapUtil = mapUtil;
        this._apiManager = apiManager;
        this._moduleSettings = moduleSettings;
        this._translationService = translationService;
        this._iconService = iconService;
        GameService.Gw2Mumble.CurrentMap.MapChanged += this.CurrentMap_MapChanged;

        this._moduleSettings.ShowEventTimersOnMap.SettingChanged += this.ShowEventTimersOnMap_SettingChanged;
        this._moduleSettings.ShowEventTimersInWorld.SettingChanged += this.ShowEventTimersInWorld_SettingChanged;
        this._moduleSettings.DisabledEventTimerSettingKeys.SettingChanged += this.DisabledEventTimerSettingKeys_SettingChanged;
    }

    public void Update(GameTime gameTime)
    {
        // This does not work in combination with DynamicEvents
        //UpdateUtil.Update(this.CheckLostEntityReferences, gameTime, _checkLostEntitiesInterval.TotalMilliseconds, ref this._lastLostEntitiesCheck);
#if DEBUG
        //_ = UpdateUtil.UpdateAsync(this.AddAll, gameTime, _readdInterval.TotalMilliseconds, _lastReadd, false);
#endif
    }

    private async Task AddAll()
    {
        await this.AddEventTimersToMap();
        await this.AddEventTimersToWorld();
    }

    private async void CurrentMap_MapChanged(object sender, ValueEventArgs<int> e)
    {
        Logger.Debug($"Changed map to id {e.Value}");

        await this.AddAll();
    }

    private async Task<List<Event>> GetAllEvents()
    {
        var allEvents = await this._getEvents();
        var events = allEvents.Where(ev => ev.Timers is not null).ToList();

        return events;
    }

    private async Task<List<Event>> GetEventsForMap(int mapId)
    {
        var allEvents = await this.GetAllEvents();
        var events = allEvents.Where(ev => ev.MapIds.Contains(mapId)).ToList();

        return events;
    }

    public async Task AddEventTimersToMap()
    {
        try
        {
            this._mapEntities?.Values.ToList().ForEach(m => this._mapUtil.RemoveEntities(m.ToArray()));
            this._mapEntities?.Clear();

            if (!this._moduleSettings.ShowEventTimersOnMap.Value || !GameService.Gw2Mumble.IsAvailable)
            {
                return;
            }

            var mapId = GameService.Gw2Mumble.CurrentMap.Id;
            var events = await this.GetAllEvents();
            //var events = await this.GetEventsForMap(mapId); // Load all events to display on full map
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
            Logger.Warn(ex, "Failed to add event timers to map.");
        }
    }

    private void RemoveEventTimerFromMap(Event ev)
    {
        if (this._mapEntities.ContainsKey(ev.SettingKey))
        {
            this._mapUtil.RemoveEntities(this._mapEntities[ev.SettingKey].ToArray());
            this._mapEntities.TryRemove(ev.SettingKey, out _);
        }
    }

    public Task AddEventTimerToMap(Event ev)
    {
        this.RemoveEventTimerFromMap(ev);

        if (!this._moduleSettings.ShowEventTimersOnMap.Value || !GameService.Gw2Mumble.IsAvailable || ev.Timers is null || (this._moduleSettings.DisabledEventTimerSettingKeys.Value?.Contains(ev.SettingKey) ?? false))
        {
            return Task.CompletedTask;
        }

        try
        {
            var mapTimers = ev.Timers.Where(t => /*t.MapID == GameService.Gw2Mumble.CurrentMap.Id &&*/ t.Map != null && t.Map.Length > 0).SelectMany(t => t.Map).ToList();

            var entities = new List<MapEntity>();

            foreach (var mapTimer in mapTimers)
            {
                MapEntity circle = this._mapUtil.AddEntity(new Controls.Map.EventMapTimer(ev, mapTimer, Color.DarkOrange, this._getNow, this._translationService, 3));
                circle.TooltipText = $"{ev.Name}";

                entities.Add(circle);
            }
            this._mapEntities.AddOrUpdate(ev.SettingKey, entities, (_, prev) => prev.Concat(entities).ToList());
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, $"Failed to add {ev.SettingKey} to map.");
        }

        return Task.CompletedTask;
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
        var events = await this.GetEventsForMap(mapId);
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
        Logger.Debug($"Added events for map {mapId} in {sw.ElapsedMilliseconds}ms");
    }

    private void RemoveEventTimerFromWorld(Event ev)
    {
        if (this._worldEntities.ContainsKey(ev.SettingKey))
        {
            GameService.Graphics.World.RemoveEntities(this._worldEntities[ev.SettingKey]);
            this._worldEntities.TryRemove(ev.SettingKey, out _);
        }
    }

    private BitmapFont GetFont(FontSize fontSize)
    {
        return this._fonts.GetOrAdd(fontSize, size => GameService.Content.GetFont(ContentService.FontFace.Menomonia, size, ContentService.FontStyle.Regular));
    }

    public Task AddEventTimerToWorld(Event ev)
    {
        this.RemoveEventTimerFromWorld(ev);

        if (!this._moduleSettings.ShowEventTimersInWorld.Value || !GameService.Gw2Mumble.IsAvailable || ev.Timers is null || (this._moduleSettings.DisabledEventTimerSettingKeys.Value?.Contains(ev.SettingKey) ?? false))
        {
            return Task.CompletedTask;
        }

        try
        {
            var renderCondition = (WorldEntity entity) => entity.DistanceToPlayer <= this._moduleSettings.EventTimersRenderDistance.Value;
            var mapId = GameService.Gw2Mumble.CurrentMap.Id;
            var now = this._getNow();

            var worldTimers = ev.Timers.Where(t => t.MapID == mapId && t.World != null && t.World.Length > 0).SelectMany(t => t.World).ToList();
            List<WorldEntity> entites = new List<WorldEntity>();
            foreach (var worldTimer in worldTimers)
            {
                Vector3 centerAsWorldMeters = new Vector3(worldTimer.X, worldTimer.Y, worldTimer.Z);

                float width = 5;
                float boxHeight = 3.5f;
                // This is offset from center
                var statuePoints = new List<Vector3>
                {
                    new Vector3(-(width/2),0,0),
                    new Vector3(width/2,0,0),
                    new Vector3(width/2,0,boxHeight),
                    new Vector3(-(width/2),0,boxHeight),
                    new Vector3(-(width/2),0,0),
                };

                // Polygone needs everything double. A -> B, B -> C, C -> D
                bool first = true;
                statuePoints = statuePoints.SelectMany(t =>
                {
                    IEnumerable<Vector3> arr = Enumerable.Repeat(t, first ? 1 : 2);
                    first = false;
                    return arr;
                }).ToList();

                statuePoints = statuePoints.Take(statuePoints.Count - 1).ToList();

                var boxTopPosition = centerAsWorldMeters + new Vector3(0, 0, boxHeight);
                var halfCircleRadius = width / 2;
                var halfCirclePosition = boxTopPosition;
                var texturePosition = halfCirclePosition + new Vector3(0, 0, halfCircleRadius * 0.75f);
                var textureScale = 1f;
                var textureIcon = this._iconService.GetIcon(ev.Icon);
                var resizeTextureSize = new Size(128, 128);

                var remainingText = () =>
                {
                    var current = ev.GetCurrentOccurrence();
                    var remainingTimeText = "---";

                    if (current != default)
                    {
                        remainingTimeText = (current.Plus(ev.Duration) - this._getNow()).ToTimeSpan().Humanize(2, minUnit: Humanizer.Localisation.TimeUnit.Second);
                    }

                    return $"Current remaining: {remainingTimeText}";
                };
                var remainingFont = this.GetFont(FontSize.Size36);
                var remainingScale = 0.4f;
                var remainingScaleWidth = 2.75f;
                var remainingColor = this._moduleSettings.EventTimersRemainingTextColor.Value.Cloth.ToXnaColor();
                var startsInText = () => $"Next in: {(ev.GetNextOccurrence() - this._getNow()).ToTimeSpan().Humanize(2, minUnit: Humanizer.Localisation.TimeUnit.Second)}";
                var startsInFont = this.GetFont(FontSize.Size36);
                var startsInScale = 0.4f;
                var startsInScaleWidth = 2.5f;
                var startsInColor = this._moduleSettings.EventTimersStartsInTextColor.Value.Cloth.ToXnaColor();
                var nextOccurrenceScale = 0.4f;
                var nextOccurrenceScaleWidth = 2f;
                var nextOccurrenceText = () => ev.GetNextOccurrence().InZone(DateTimeZoneProviders.Tzdb.GetSystemDefault()).ToString();
                var nextOccurrenceColor = this._moduleSettings.EventTimersNextOccurenceTextColor.Value.Cloth.ToXnaColor();
                var nextOccurrenceFont = this.GetFont(FontSize.Size36);
                var nameText = () => $"{ev.Name}";
                var nameFont = this.GetFont(FontSize.Size36);
                var nameScale = 0.6f;
                var nameScaleWidth = width / 1.5f;
                var nameColor = this._moduleSettings.EventTimersNameTextColor.Value.Cloth.ToXnaColor();
                var durationText = () => $"Duration: {ev.Duration}min";
                var durationFont = this.GetFont(FontSize.Size36);
                var durationScale = 0.4f;
                var durationScaleWidth = 2f;
                var durationColor = this._moduleSettings.EventTimersDurationTextColor.Value.Cloth.ToXnaColor();
                var repeatText = () => $"Repeats every: {ev.Repeat.ToTimeSpan().Humanize()}";
                var repeatFont = this.GetFont(FontSize.Size36);
                var repeatScale = 0.4f;
                var repeatScaleWidth = 2f;
                var repeatColor = this._moduleSettings.EventTimersRepeatTextColor.Value.Cloth.ToXnaColor();

#if DEBUG
                var sideIndicatorColor = Color.Red;
                var sideIndicatorFrontText = () => "FRONT";
                var sideIndicatorBackText = () => "BACK";
                var sideIndicatorFont = this.GetFont(FontSize.Size36);
                var sideIndicatorScale = 0.75f;
                var sideIndicatorScaleWidth = 2f;
#endif

                var namePosition = texturePosition + new Vector3(0, 0, -0.75f);
                var durationPosition = namePosition + new Vector3(0, 0, -(nameScale / 2f + durationScale / 2f));
                var repeatPosition = durationPosition + new Vector3(0, 0, -(durationScale / 2f + repeatScale / 2f));
                var remainingPosition = boxTopPosition + new Vector3(0, 0, -(nameScale / 2f));
                var startsInPosition = remainingPosition + new Vector3(0, 0, -remainingScale);
                var nextOccurrencePosition = startsInPosition + new Vector3(0, 0, -startsInScale);
                var sideIndicatorPosition = halfCirclePosition + new Vector3(0, 0, halfCircleRadius + 0.5f);

                entites.AddRange(new WorldEntity[]
                {
#if DEBUG
                    new WorldPolygone(centerAsWorldMeters, new Vector3[]{ new Vector3(0, 0, 0.25f), new Vector3(2,0, 0.25f) }, Color.DarkGreen) { RenderCondition = renderCondition },
                    new WorldPolygone(centerAsWorldMeters, new Vector3[]{ new Vector3(0, 0, 0.25f), new Vector3(0,2, 0.25f) }, Color.DarkBlue) { RenderCondition = renderCondition } ,
                    new WorldPolygone(centerAsWorldMeters, new Vector3[]{ new Vector3(0, 0, 0.25f), new Vector3(0,0, 2.25f) }, Color.DarkMagenta) { RenderCondition = renderCondition } ,
#endif
                    // Box
                    new WorldPolygone(centerAsWorldMeters, statuePoints.ToArray()) {
                        RotationZ = worldTimer.Rotation,
                        RenderCondition = renderCondition,
                        //InteractionAction = async () => ScreenNotification.ShowNotification("TEST")
                    },
                    //new WorldPolygone(centerAsWorldMeters, new Vector3[]
                    //{
                    //    new Vector3(width / 2f, 0,0),
                    //    new Vector3(width / 2f, -0.5f,0),
                    //    new Vector3(width / 2f, -0.5f,0),
                    //    new Vector3(width / 2f + 0.5f, -0.5f,0),
                    //    new Vector3(width / 2f, -0.5f,0),
                    //    new Vector3(width / 2f, -0.5f,1),
                    //    new Vector3(width / 2f, -0.5f,1),
                    //    new Vector3(width / 2f + 0.5f, -0.5f,1),
                    //    new Vector3(width / 2f + 0.5f, 0,0),
                    //    new Vector3(width / 2f + 0.5f, 0,1),
                    //    new Vector3(width / 2f + 0.5f, 0,1),
                    //    new Vector3(width / 2f + 0.5f, -0.5f,1),
                    //    new Vector3(width / 2f, 0,1),
                    //    new Vector3(width / 2f, -0.5f,1),
                    //    new Vector3(width / 2f, 0,1),
                    //    new Vector3(width / 2f + 0.5f, 0,1),
                    //    new Vector3(width / 2f, 0,0),
                    //    new Vector3(width / 2f+0.5f,0,0),
                    //    new Vector3(width / 2f, 0,0),
                    //    new Vector3(width / 2f, 0,1),
                    //    new Vector3(width / 2f+0.5f, -0.5f,0),
                    //    new Vector3(width / 2f+0.5f, -0.5f,1),
                    //    new Vector3(width / 2f+0.5f, 0,0),
                    //    new Vector3(width / 2f+0.5f, -0.5f,0),
                    //}, Color.Red)
                    //{
                    //    RotationZ = worldTimer.Rotation
                    //},
                    // Top half circle
                    new WorldHalfCircle(halfCirclePosition, halfCircleRadius) {
                        RotationZ = worldTimer.Rotation,
                        RotationX=90,
                        RenderCondition = renderCondition
                    },
                    // TEXTURE - START
                    new WorldTexture(textureIcon, texturePosition, textureScale)
                    {
                        RotationZ = worldTimer.Rotation,
                        RotationX=90,
                        RenderCondition = renderCondition
                        //ResizeHeight = resizeTextureSize.Height,
                        //ResizeWidth = resizeTextureSize.Width
                    },
                    new WorldTexture(textureIcon, texturePosition, textureScale)
                    {
                        RotationZ = worldTimer.Rotation + 180,
                        RotationX=90,
                        RenderCondition = renderCondition
                        //ResizeHeight = resizeTextureSize.Height,
                        //ResizeWidth = resizeTextureSize.Width
                    },
                    // TEXTURE - END
                    // Clock
                    //new WorldClock(centerAsWorldMeters + new Vector3(0,0, boxHeight + width/3), 0.75f, this._getNow)
                    //{
                    //    RotationZ = worldTimer.Rotation,
                    //    RotationX=90
                    //},
                    //new WorldRectangle(centerAsWorldMeters + new Vector3(0,0, boxHeight + width/3), Color.Black, 1.55f)
                    //{
                    //    RotationZ = worldTimer.Rotation,
                    //    RotationX=90,
                    //},
                    //new WorldClock(centerAsWorldMeters + new Vector3(0,0, boxHeight + width/3), 0.75f, this._getNow)
                    //{
                    //    RotationZ = worldTimer.Rotation+ 180,
                    //    RotationX=90
                    //},
                    // CURRENT REMAINING - START
                    new WorldText(remainingText, remainingFont, remainingPosition, remainingScale, remainingColor)
                    {
                        RotationZ = worldTimer.Rotation,
                        RotationX=90,
                        ScaleX=remainingScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    new WorldText(remainingText, remainingFont, remainingPosition, remainingScale, remainingColor)
                    {
                        RotationZ = worldTimer.Rotation + 180,
                        RotationX=90,
                        ScaleX=remainingScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    // CURRENT REMAINING - END
                    // STARTS IN - START
                    new WorldText(startsInText, startsInFont, startsInPosition, startsInScale, startsInColor)
                    {
                        RotationZ = worldTimer.Rotation,
                        RotationX=90,
                        ScaleX=startsInScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    new WorldText(startsInText, startsInFont, startsInPosition, startsInScale, startsInColor)
                    {
                        RotationZ = worldTimer.Rotation + 180,
                        RotationX=90,
                        ScaleX=startsInScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    // STARTS IN - END
                    // NEXT OCCURENCE - START
                    new WorldText(nextOccurrenceText, nextOccurrenceFont, nextOccurrencePosition, nextOccurrenceScale, nextOccurrenceColor)
                    {
                        RotationZ = worldTimer.Rotation,
                        RotationX=90,
                        ScaleX=nextOccurrenceScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    new WorldText(nextOccurrenceText, nextOccurrenceFont, nextOccurrencePosition, nextOccurrenceScale, nextOccurrenceColor)
                    {
                        RotationZ = worldTimer.Rotation + 180,
                        RotationX=90,
                        ScaleX=nextOccurrenceScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    // NEXT OCCURENCE - END
                    // NAME - START
                    new WorldText(nameText, nameFont, namePosition, nameScale, nameColor)
                    {
                        RotationZ = worldTimer.Rotation,
                        RotationX=90,
                        ScaleX=nameScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    new WorldText(nameText, nameFont, namePosition, nameScale, nameColor)
                    {
                        RotationZ = worldTimer.Rotation + 180,
                        RotationX=90,
                        ScaleX=nameScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    // NAME - END
                    // Duration - START
                    new WorldText(durationText, durationFont, durationPosition, durationScale, durationColor)
                    {
                        RotationZ = worldTimer.Rotation,
                        RotationX=90,
                        ScaleX=durationScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    new WorldText(durationText, durationFont, durationPosition, durationScale, durationColor)
                    {
                        RotationZ = worldTimer.Rotation + 180,
                        RotationX=90,
                        ScaleX=durationScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    // Duration - END
                    // Repeat - START
                    new WorldText(repeatText, repeatFont, repeatPosition, repeatScale, repeatColor)
                    {
                        RotationZ = worldTimer.Rotation,
                        RotationX=90,
                        ScaleX=repeatScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    new WorldText(repeatText, repeatFont, repeatPosition, repeatScale, repeatColor)
                    {
                        RotationZ = worldTimer.Rotation + 180,
                        RotationX=90,
                        ScaleX=repeatScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    // Repeat - END
#if DEBUG
                    new WorldText(sideIndicatorFrontText, sideIndicatorFont, sideIndicatorPosition, sideIndicatorScale, sideIndicatorColor)
                    {
                        RotationZ = worldTimer.Rotation,
                        RotationX=90,
                        ScaleX=sideIndicatorScaleWidth, // Width
                        RenderCondition = renderCondition
                    },
                    new WorldText(sideIndicatorBackText, sideIndicatorFont, sideIndicatorPosition, sideIndicatorScale, sideIndicatorColor)
                    {
                        RotationZ = worldTimer.Rotation + 180,
                        RotationX=90,
                        ScaleX=sideIndicatorScaleWidth, // Width
                        RenderCondition = renderCondition
                    }
#endif
                });
            }

            this._worldEntities.AddOrUpdate(ev.SettingKey, entites, (_, prev) => prev.Concat(entites).ToList());
            GameService.Graphics.World.AddEntities(entites);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, $"Failed to add {ev.SettingKey} to world.");
        }

        return Task.CompletedTask;
    }

    public async Task NotifyUpdatedEvents()
    {
        await this.AddAll();
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

    private async void DisabledEventTimerSettingKeys_SettingChanged(object sender, ValueChangedEventArgs<List<string>> e)
    {
        await this.AddAll();
    }

    private async void ShowEventTimersInWorld_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        await this.AddAll();
    }

    private async void ShowEventTimersOnMap_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        await this.AddAll();
    }

    public void Dispose()
    {
        GameService.Graphics.World.RemoveEntities(this._worldEntities.Values.SelectMany(v => v));
        this._worldEntities?.Clear();

        this._mapEntities?.Values.ToList().ForEach(me => this._mapUtil.RemoveEntities(me.ToArray()));
        this._mapEntities?.Clear();

        GameService.Gw2Mumble.CurrentMap.MapChanged -= this.CurrentMap_MapChanged;

        this._moduleSettings.ShowEventTimersOnMap.SettingChanged -= this.ShowEventTimersOnMap_SettingChanged;
        this._moduleSettings.ShowEventTimersInWorld.SettingChanged -= this.ShowEventTimersInWorld_SettingChanged;
        this._moduleSettings.DisabledEventTimerSettingKeys.SettingChanged -= this.DisabledEventTimerSettingKeys_SettingChanged;

        this._fonts?.Clear();
    }
}