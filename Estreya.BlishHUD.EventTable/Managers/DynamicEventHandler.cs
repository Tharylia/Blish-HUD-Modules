namespace Estreya.BlishHUD.EventTable.Managers;
using Blish_HUD;
using Blish_HUD.ArcDps.Models;
using Blish_HUD.Entities;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Estreya.BlishHUD.EventTable.State;
using Estreya.BlishHUD.Shared.Controls.Map;
using Estreya.BlishHUD.Shared.Controls.World;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Utils;
using Humanizer;
using Microsoft.Xna.Framework;
using Octokit;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Estreya.BlishHUD.EventTable.State.DynamicEventState;
using static System.Net.Mime.MediaTypeNames;

public class DynamicEventHandler : IDisposable, IUpdatable
{
    private static readonly Logger Logger = Logger.GetLogger<DynamicEventHandler>();
    private readonly MapUtil _mapUtil;
    private readonly DynamicEventState _dynamicEventState;
    private readonly Gw2ApiManager _apiManager;
    private readonly ModuleSettings _moduleSettings;
    private ConcurrentDictionary<string, MapEntity> _mapEntities = new ConcurrentDictionary<string, MapEntity>();
    private ConcurrentDictionary<string, List<WorldEntity>> _worldEntities = new ConcurrentDictionary<string, List<WorldEntity>>();

    private ConcurrentQueue<(string Key, bool Add)> _entityQueue = new ConcurrentQueue<(string Key, bool Add)>();

    public DynamicEventHandler(MapUtil mapUtil, DynamicEventState dynamicEventState, Gw2ApiManager apiManager,
        ModuleSettings moduleSettings)
    {
        this._mapUtil = mapUtil;
        this._dynamicEventState = dynamicEventState;
        this._apiManager = apiManager;
        this._moduleSettings = moduleSettings;
        GameService.Gw2Mumble.CurrentMap.MapChanged += this.CurrentMap_MapChanged;
        this._moduleSettings.ShowDynamicEventsOnMap.SettingChanged += this.ShowDynamicEventsOnMap_SettingChanged;
        this._moduleSettings.ShowDynamicEventInWorld.SettingChanged += this.ShowDynamicEventsInWorldSetting_SettingChanged;
        this._moduleSettings.DisabledDynamicEventIds.SettingChanged += this.DisabledDynamicEventIds_SettingChanged;
    }

    private void DisabledDynamicEventIds_SettingChanged(object sender, ValueChangedEventArgs<List<string>> e)
    {
        var newElements = e.NewValue.Where(newKey => !e.PreviousValue.Any(oldKey => oldKey == newKey));
        var removeElements = e.PreviousValue.Where(oldKey => !e.NewValue.Any(newKey => newKey == oldKey));

        foreach (var newElement in newElements)
        {
            this._entityQueue.Enqueue((newElement, false));
        }

        foreach (var oldElement in removeElements)
        {
            this._entityQueue.Enqueue((oldElement, true));
        }
    }

    private async void ShowDynamicEventsInWorldSetting_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        await this.AddDynamicEventsToWorld();
    }

    private async void ShowDynamicEventsOnMap_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        await this.AddDynamicEventsToMap();
    }

    private async void CurrentMap_MapChanged(object sender, ValueEventArgs<int> e)
    {
        await this.AddDynamicEventsToMap();
        await this.AddDynamicEventsToWorld();
    }

    public async Task AddDynamicEventsToMap()
    {
        try
        {
            this._mapEntities?.Values.ToList().ForEach(m => this._mapUtil.RemoveEntity(m));
            this._mapEntities?.Clear();

            if (!this._moduleSettings.ShowDynamicEventsOnMap.Value || !GameService.Gw2Mumble.IsAvailable) return;

            var success = await this._dynamicEventState.WaitForCompletion(TimeSpan.FromMinutes(5));

            if (!success)
            {
                Logger.Debug("DynamicEventState did not finish in the given timespan. Abort.");
                return;
            }

            var mapId = GameService.Gw2Mumble.CurrentMap.Id;
            var events = this._dynamicEventState.GetEventsByMap(mapId)?.Where(de => !this._moduleSettings.DisabledDynamicEventIds.Value.Contains(de.ID)).OrderByDescending(d => d.Location.Points?.Length ?? 0).ThenByDescending(d => d.Location.Radius);
            if (events == null)
            {
                Logger.Debug($"No events found for map {mapId}");
                return;
            }

            var mapEntites = new List<MapEntity>();
            foreach (var ev in events)
            {
                await this.AddDynamicEventToMap(ev);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to add dynamic events to map.");
        }
    }

    private void RemoveDynamicEventFromMap(DynamicEvent dynamicEvent)
    {
        if (this._mapEntities.ContainsKey(dynamicEvent.ID))
        {
            this._mapUtil.RemoveEntity(this._mapEntities[dynamicEvent.ID]);
            this._mapEntities.TryRemove(dynamicEvent.ID, out _);
        }
    }

    public async Task AddDynamicEventToMap(DynamicEvent dynamicEvent)
    {
        this.RemoveDynamicEventFromMap(dynamicEvent);

        if (!this._moduleSettings.ShowDynamicEventsOnMap.Value || !GameService.Gw2Mumble.IsAvailable) return;

        var success = await this._dynamicEventState.WaitForCompletion(TimeSpan.FromMinutes(5));

        if (!success)
        {
            Logger.Debug("DynamicEventState did not finish in the given timespan. Abort.");
            return;
        }

        try
        {
            var map = await this._apiManager.Gw2ApiClient.V2.Maps.GetAsync(dynamicEvent.MapId);
            var coords = new Vector2((float)dynamicEvent.Location.Center[0], (float)dynamicEvent.Location.Center[1] );
            switch (dynamicEvent.Location.Type)
            {
                case "sphere":
                case "cylinder":
                    var circle = this._mapUtil.AddCircle(coords.X, coords.Y, dynamicEvent.Location.Radius * (1 / 24f), Color.DarkOrange, 3);
                    circle.TooltipText = $"{dynamicEvent.Name} (Level {dynamicEvent.Level})";
                    this._mapEntities.AddOrUpdate(dynamicEvent.ID, circle, (_, _) => circle);
                    break;
                case "poly":
                    var points = new List<float[]>();
                    foreach (var item in dynamicEvent.Location.Points)
                    {
                        var polyCoords = new Vector2((float)item[0], (float)item[1]);

                        points.Add(new float[] { (float)polyCoords.X, (float)polyCoords.Y });
                    }

                    var border = this._mapUtil.AddBorder(coords.X, coords.Y, points.ToArray(), Color.DarkOrange, 4);
                    border.TooltipText = $"{dynamicEvent.Name} (Level {dynamicEvent.Level})";
                    this._mapEntities.AddOrUpdate(dynamicEvent.ID, border, (_, _) => border);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, $"Failed to add {dynamicEvent.Name} to map.");
        }
    }

    private bool WorldEventRenderCondition(WorldEntity worldEntity)
    {
        if (this._moduleSettings.ShowDynamicEventsInWorldOnlyWhenInside.Value)
        {
            return worldEntity.IsPlayerInside(!this._moduleSettings.IgnoreZAxisOnDynamicEventsInWorld.Value);
        }
        else
        {
            return this._moduleSettings.DynamicEventsRenderDistance.Value >= worldEntity.DistanceToPlayer;
        }

        return true;
    }

    public async Task AddDynamicEventsToWorld()
    {
        GameService.Graphics.World.RemoveEntities(_worldEntities.Values.SelectMany(v => v));
        _worldEntities?.Clear();

        if (!this._moduleSettings.ShowDynamicEventInWorld.Value || !GameService.Gw2Mumble.IsAvailable) return;

        var success = await this._dynamicEventState.WaitForCompletion(TimeSpan.FromMinutes(5));

        if (!success)
        {
            Logger.Debug("DynamicEventState did not finish in the given timespan. Abort.");
            return;
        }

        var mapId = GameService.Gw2Mumble.CurrentMap.Id;
        var events = this._dynamicEventState.GetEventsByMap(mapId)
            .Where(de => !this._moduleSettings.DisabledDynamicEventIds.Value.Contains(de.ID));
        if (events == null)
        {
            Logger.Debug($"No events found for map {mapId}");
            return;
        }

        var sw = Stopwatch.StartNew();
        foreach (var ev in events)
        {
            await this.AddDynamicEventToWorld(ev);
        }

        sw.Stop();
        Logger.Debug($"Added events in {sw.ElapsedMilliseconds}ms");
    }

    private void RemoveDynamicEventFromWorld(DynamicEvent dynamicEvent)
    {
        if (this._worldEntities.ContainsKey(dynamicEvent.ID))
        {
            GameService.Graphics.World.RemoveEntities(_worldEntities[dynamicEvent.ID]);
            this._worldEntities.TryRemove(dynamicEvent.ID, out _);
        }
    }

    public async Task AddDynamicEventToWorld(DynamicEvent dynamicEvent)
    {
        this.RemoveDynamicEventFromWorld(dynamicEvent);

        if (!this._moduleSettings.ShowDynamicEventInWorld.Value || !GameService.Gw2Mumble.IsAvailable) return;

        try
        {
            var map = await this._apiManager.Gw2ApiClient.V2.Maps.GetAsync(dynamicEvent.MapId);
            var centerAsMapCoords = new Vector2((float)dynamicEvent.Location.Center[0], (float)dynamicEvent.Location.Center[1]);
            var centerAsWorldMeters = map.MapCoordsToWorldMeters(new Vector2((float)centerAsMapCoords.X, (float)centerAsMapCoords.Y));
            centerAsWorldMeters.Z = (float)Math.Abs(dynamicEvent.Location.Center[2].ToMeters());

            var entites = new List<WorldEntity>();
            switch (dynamicEvent.Location.Type)
            {
                case "poly":
                    entites.Add(await this.GetPolygone(dynamicEvent, map, centerAsWorldMeters, this.WorldEventRenderCondition));
                    break;
                case "sphere":
                    entites.Add(await this.GetSphere(dynamicEvent, map, centerAsWorldMeters, this.WorldEventRenderCondition));
                    break;
                case "cylinder":
                    entites.Add(await this.GetCylinder(dynamicEvent, map, centerAsWorldMeters, this.WorldEventRenderCondition));
                    break;
                default:
                    break;
            }

            _worldEntities.AddOrUpdate(dynamicEvent.ID, entites, (_, prev) => prev.Concat(entites).ToList());
            GameService.Graphics.World.AddEntities(entites);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, $"Failed to add {dynamicEvent.Name} to world.");
        }

    }

    private async Task<WorldEntity> GetSphere(DynamicEventState.DynamicEvent ev, Gw2Sharp.WebApi.V2.Models.Map map, Vector3 centerAsWorldMeters, Func<WorldEntity, bool> renderCondition)
    {
        var tessellation = 50;
        var connections = tessellation / 5;

        if (connections > tessellation)
        {
            throw new ArgumentOutOfRangeException("connections", "connections can't be greater than tessellation");
        }

        var radius = (float)ev.Location.Radius.ToMeters();

        var points = new List<Vector3>();

        for (int i = 0; i < tessellation; i++)
        {
            float circumferenceProgress = (float)i / tessellation;
            float currentRadian = (float)(circumferenceProgress * 2 * Math.PI);


            float xScaled = (float)Math.Cos(currentRadian);
            float yScaled = (float)Math.Sin(currentRadian);

            float x = xScaled * radius;
            float y = yScaled * radius;

            points.Add(new Vector3(x, y, 0));
        }

        // Polygone needs everything double
        var first = true;
        points = points.SelectMany(t =>
        {
            var arr = Enumerable.Repeat(t, first ? 1 : 2);
            first = false;
            return arr;
        }).ToList();

        // Connect end to start
        points.Add(points[0]);

        //var polygones = new List<WorldPolygone>();
        //polygones.Add(new WorldPolygone(centerAsWorldMeters, points.ToArray(), Color.White, renderCondition));

        var connectionSteps = tessellation / connections;
        var bendSteps = 12f;

        var connectionPoints = new List<Vector3>();

        for (int p = 0; p < tessellation; p += connectionSteps)
        {
            Vector3 point = points[p * 2];
            var mid = new Vector3(point.X, point.Y, 0);

            var bendPointsUp = new List<Vector3>();
            var up = new Vector3(0, 0, radius);
            Vector3 centerUp = new Vector3((mid.X + up.X), (mid.Y + up.Y), (mid.Z + up.Z));

            for (float ratio = 0; ratio <= 1; ratio += 1 / bendSteps)
            {
                var tangent1 = Vector3.Lerp(mid, centerUp, ratio);
                var tangent2 = Vector3.Lerp(centerUp, up, ratio);
                var curve = Vector3.Lerp(tangent1, tangent2, ratio);

                bendPointsUp.Add(curve);
            }
            var bendPointsUpFirst = true;
            bendPointsUp = bendPointsUp.SelectMany(t =>
            {
                var arr = Enumerable.Repeat(t, bendPointsUpFirst ? 1 : 2);
                bendPointsUpFirst = false;
                return arr;
            }).ToList();
            bendPointsUp.RemoveAt(bendPointsUp.Count - 1);
            connectionPoints.AddRange(bendPointsUp);
            //polygones.Add(new WorldPolygone(centerAsWorldMeters, bendPointsUp.ToArray(), Color.White, renderCondition));

            var down = new Vector3(0, 0, -radius);
            var bendPointsDown = new List<Vector3>();
            Vector3 centerDown = new Vector3((mid.X + down.X), (mid.Y + down.Y), (mid.Z + down.Z));

            for (float ratio = 0; ratio <= 1; ratio += 1 / bendSteps)
            {
                var tangent1 = Vector3.Lerp(mid, centerDown, ratio);
                var tangent2 = Vector3.Lerp(centerDown, down, ratio);
                var curve = Vector3.Lerp(tangent1, tangent2, ratio);

                bendPointsDown.Add(curve);
            }
            var bendPointsDownFirst = true;
            bendPointsDown = bendPointsDown.SelectMany(t =>
            {
                var arr = Enumerable.Repeat(t, bendPointsDownFirst ? 1 : 2);
                bendPointsDownFirst = false;
                return arr;
            }).ToList();
            bendPointsDown.RemoveAt(bendPointsDown.Count - 1);
            connectionPoints.AddRange(bendPointsDown);
        }

        var allPoints = points.Concat(connectionPoints);
        return  new WorldPolygone(centerAsWorldMeters, allPoints.ToArray(), Color.White, renderCondition) ;
    }

    private async Task<WorldEntity> GetCylinder(DynamicEventState.DynamicEvent ev, Gw2Sharp.WebApi.V2.Models.Map map, Vector3 centerAsWorldMeters, Func<WorldEntity, bool> renderCondition)
    {
        var tessellation = 50;
        var connections = tessellation / 4;

        if (connections > tessellation)
        {
            throw new ArgumentOutOfRangeException("connections", "connections can't be greater than tessellation");
        }

        var radius = (float)ev.Location.Radius.ToMeters();

        var height = (float)ev.Location.Height.ToMeters();

        var points = new List<Vector3>();

        for (int i = 0; i < tessellation; i++)
        {
            float circumferenceProgress = (float)i / tessellation;
            float currentRadian = (float)(circumferenceProgress * 2 * Math.PI);

            float xScaled = (float)Math.Cos(currentRadian);
            float yScaled = (float)Math.Sin(currentRadian);

            float x = xScaled * radius;
            float y = yScaled * radius;

            points.Add(new Vector3(x, y, centerAsWorldMeters.Z));
        }

        // Polygone needs everything double
        var first = true;
        points = points.SelectMany(t =>
        {
            var arr = Enumerable.Repeat(t, first ? 1 : 2);
            first = false;
            return arr;
        }).ToList();

        // Connect end to start
        points.Add(points[0]);

        var zRanges = new double[] { 0, height };
        var perZRangePoints = zRanges.OrderBy(z => z).Select(z =>
        {
            var p = points.Select(mp => new Vector3(mp.X, mp.Y, (float)z)).ToArray();
            return p;
        }).ToArray();

        var connectPoints = new List<Vector3>();

        for (int p = 0; p < connections; p++)
        {
            float circumferenceProgress = (float)p / connections;
            float currentRadian = (float)(circumferenceProgress * 2 * Math.PI);

            float xScaled = (float)Math.Cos(currentRadian);
            float yScaled = (float)Math.Sin(currentRadian);

            float x = xScaled * radius;
            float y = yScaled * radius;

            for (int i = 0; i < perZRangePoints.Length - 1; i++)
            {
                var curr = perZRangePoints[i];
                var next = perZRangePoints[i + 1];

                if (curr.Length != next.Length)
                {
                    throw new ArgumentOutOfRangeException("WorldPolygone.Points", "Length does not match.");
                }

                connectPoints.Add(new Vector3(x, y, curr[0].Z));
                connectPoints.Add(new Vector3(x, y, next[0].Z));
            }
        }

        var allPoints = perZRangePoints.SelectMany(x => x).Concat(connectPoints);

        return new WorldPolygone(centerAsWorldMeters, allPoints.ToArray(), Color.White, renderCondition);
    }

    private async Task<WorldEntity> GetPolygone(DynamicEventState.DynamicEvent dynamicEvent, Gw2Sharp.WebApi.V2.Models.Map map, Vector3 centerAsWorldMeters, Func<WorldEntity, bool> renderCondition)
    {

        // Map all event points to world coordinates
        var points = dynamicEvent.Location.Points.Select(p =>
        {
            var mapCoords = new Vector2((float)p[0], (float)p[1]);
            var worldCoords = map.MapCoordsToWorldMeters(new Vector2((float)mapCoords.X, (float)mapCoords.Y));
            var vector = new Vector3((float)worldCoords.X, (float)worldCoords.Y, centerAsWorldMeters.Z);
            return vector;
        }).ToArray();

        // Polygone needs everything double
        var first = true;
        points = points.SelectMany(t =>
        {
            var arr = Enumerable.Repeat(t, first ? 1 : 2);
            first = false;
            return arr;
        }).ToArray();

        // Connect end to start
        var pointList = points.ToList();
        pointList.Add(points[0]);
        points = pointList.ToArray();

        // Polygone needs stuff as offset from center
        var mappedPoints = new Vector3[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            var curPoint = points[i];
            var offset = curPoint - centerAsWorldMeters;


            mappedPoints[i] = offset;
        }

        var perZRangePoints = dynamicEvent.Location.ZRange.OrderBy(z => z).Select(z => centerAsWorldMeters.Z + z.ToMeters()).Select(z =>
        {
            var p = mappedPoints.Select(mp => new Vector3(mp.X, mp.Y, (float)z)).ToArray();
            return p;
        }).ToArray();

        var connectPoints = new List<Vector3>();
        for (int i = 0; i < perZRangePoints.Length - 1; i++)
        {
            var curr = perZRangePoints[i];
            var next = perZRangePoints[i + 1];

            if (curr.Length != next.Length)
            {
                throw new ArgumentOutOfRangeException("points", "Length does not match.");
            }

            for (int p = 0; p < curr.Length; p++)
            {
                var currP = curr[p];
                var nextP = next[p];
                connectPoints.Add(currP);
                connectPoints.Add(nextP);
            }
        }

        var allPoints = perZRangePoints.SelectMany(x => x).Concat(connectPoints);

        return new WorldPolygone(centerAsWorldMeters, allPoints.ToArray(), Color.White, renderCondition);
    }

    public void Dispose()
    {
        GameService.Graphics.World.RemoveEntities(_worldEntities.Values.SelectMany(v => v));
        _worldEntities?.Clear();

        _mapEntities?.Values.ToList().ForEach(me => this._mapUtil.RemoveEntity(me));
        _mapEntities?.Clear();

        GameService.Gw2Mumble.CurrentMap.MapChanged -= this.CurrentMap_MapChanged;
        this._moduleSettings.ShowDynamicEventsOnMap.SettingChanged -= this.ShowDynamicEventsOnMap_SettingChanged;
        this._moduleSettings.ShowDynamicEventInWorld.SettingChanged -= this.ShowDynamicEventsInWorldSetting_SettingChanged;
        this._moduleSettings.DisabledDynamicEventIds.SettingChanged -= this.DisabledDynamicEventIds_SettingChanged;
    }

    public void Update(GameTime gameTime)
    {
        while (this._entityQueue.TryDequeue(out var element))
        {
            try
            {
                var dynamicEvent = this._dynamicEventState.Events.Where(e => e.ID == element.Key).First();
                if (element.Add)
                {
                    _ = Task.Run(async () =>
                    {
                        await this.AddDynamicEventToMap(dynamicEvent);
                        await this.AddDynamicEventToWorld(dynamicEvent);
                    });
                }
                else
                {
                    this.RemoveDynamicEventFromMap(dynamicEvent);
                    this.RemoveDynamicEventFromWorld(dynamicEvent);
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed updating event {element.Key}");
            }
        }
    }
}
