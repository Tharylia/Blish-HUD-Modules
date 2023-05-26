namespace Estreya.BlishHUD.EventTable.Managers;

using Blish_HUD;
using Blish_HUD.Entities;
using Blish_HUD.Modules.Managers;
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

public class DynamicEventHandler : IDisposable, IUpdatable
{
    private static readonly Logger Logger = Logger.GetLogger<DynamicEventHandler>();

    private static TimeSpan _checkLostEntitiesInterval = TimeSpan.FromSeconds(5);
    private readonly Gw2ApiManager _apiManager;
    private readonly DynamicEventService _dynamicEventService;
    private readonly MapUtil _mapUtil;
    private readonly ModuleSettings _moduleSettings;

    private readonly ConcurrentQueue<(string Key, bool Add)> _entityQueue = new ConcurrentQueue<(string Key, bool Add)>();
    private double _lastLostEntitiesCheck;
    private readonly ConcurrentDictionary<string, MapEntity> _mapEntities = new ConcurrentDictionary<string, MapEntity>();

    private bool _notifiedLostEntities;
    private readonly ConcurrentDictionary<string, List<WorldEntity>> _worldEntities = new ConcurrentDictionary<string, List<WorldEntity>>();

    public DynamicEventHandler(MapUtil mapUtil, DynamicEventService dynamicEventService, Gw2ApiManager apiManager,
        ModuleSettings moduleSettings)
    {
        this._mapUtil = mapUtil;
        this._dynamicEventService = dynamicEventService;
        this._apiManager = apiManager;
        this._moduleSettings = moduleSettings;
        GameService.Gw2Mumble.CurrentMap.MapChanged += this.CurrentMap_MapChanged;
        this._moduleSettings.ShowDynamicEventsOnMap.SettingChanged += this.ShowDynamicEventsOnMap_SettingChanged;
        this._moduleSettings.ShowDynamicEventInWorld.SettingChanged += this.ShowDynamicEventsInWorldSetting_SettingChanged;
        this._moduleSettings.DisabledDynamicEventIds.SettingChanged += this.DisabledDynamicEventIds_SettingChanged;
    }

    public void Dispose()
    {
        GameService.Graphics.World.RemoveEntities(this._worldEntities.Values.SelectMany(v => v));
        this._worldEntities?.Clear();

        this._mapEntities?.Values.ToList().ForEach(me => this._mapUtil.RemoveEntity(me));
        this._mapEntities?.Clear();

        GameService.Gw2Mumble.CurrentMap.MapChanged -= this.CurrentMap_MapChanged;
        this._moduleSettings.ShowDynamicEventsOnMap.SettingChanged -= this.ShowDynamicEventsOnMap_SettingChanged;
        this._moduleSettings.ShowDynamicEventInWorld.SettingChanged -= this.ShowDynamicEventsInWorldSetting_SettingChanged;
        this._moduleSettings.DisabledDynamicEventIds.SettingChanged -= this.DisabledDynamicEventIds_SettingChanged;
    }

    public void Update(GameTime gameTime)
    {
        UpdateUtil.Update(this.CheckLostEntityReferences, gameTime, _checkLostEntitiesInterval.TotalMilliseconds, ref this._lastLostEntitiesCheck);

        while (this._entityQueue.TryDequeue(out (string Key, bool Add) element))
        {
            try
            {
                DynamicEvent dynamicEvent = this._dynamicEventService.Events.Where(e => e.ID == element.Key).First();
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

    public event EventHandler FoundLostEntities;

    private void DisabledDynamicEventIds_SettingChanged(object sender, ValueChangedEventArgs<List<string>> e)
    {
        IEnumerable<string> newElements = e.NewValue.Where(newKey => !e.PreviousValue.Any(oldKey => oldKey == newKey));
        IEnumerable<string> removeElements = e.PreviousValue.Where(oldKey => !e.NewValue.Any(newKey => newKey == oldKey));

        foreach (string newElement in newElements)
        {
            this._entityQueue.Enqueue((newElement, false));
        }

        foreach (string oldElement in removeElements)
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

            if (!this._moduleSettings.ShowDynamicEventsOnMap.Value || !GameService.Gw2Mumble.IsAvailable)
            {
                return;
            }

            bool success = await this._dynamicEventService.WaitForCompletion(TimeSpan.FromMinutes(5));

            if (!success)
            {
                Logger.Debug("DynamicEventService did not finish in the given timespan. Abort.");
                return;
            }

            int mapId = GameService.Gw2Mumble.CurrentMap.Id;
            IOrderedEnumerable<DynamicEvent> events = this._dynamicEventService.GetEventsByMap(mapId)?.Where(de => !this._moduleSettings.DisabledDynamicEventIds.Value.Contains(de.ID)).OrderByDescending(d => d.Location.Points?.Length ?? 0).ThenByDescending(d => d.Location.Radius);
            if (events == null)
            {
                Logger.Debug($"No events found for map {mapId}");
                return;
            }

            List<MapEntity> mapEntites = new List<MapEntity>();
            foreach (DynamicEvent ev in events)
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

        if (!this._moduleSettings.ShowDynamicEventsOnMap.Value || !GameService.Gw2Mumble.IsAvailable)
        {
            return;
        }

        bool success = await this._dynamicEventService.WaitForCompletion(TimeSpan.FromMinutes(5));

        if (!success)
        {
            Logger.Debug("DynamicEventService did not finish in the given timespan. Abort.");
            return;
        }

        try
        {
            Vector2 coords = new Vector2(dynamicEvent.Location.Center[0], dynamicEvent.Location.Center[1]);
            switch (dynamicEvent.Location.Type)
            {
                case "sphere":
                case "cylinder":
                    MapEntity circle = this._mapUtil.AddCircle(coords.X, coords.Y, dynamicEvent.Location.Radius * (1 / 24f), Color.DarkOrange, 3);
                    circle.TooltipText = $"{dynamicEvent.Name} (Level {dynamicEvent.Level})";
                    this._mapEntities.AddOrUpdate(dynamicEvent.ID, circle, (_, _) => circle);
                    break;
                case "poly":
                    List<float[]> points = new List<float[]>();
                    foreach (float[] item in dynamicEvent.Location.Points)
                    {
                        Vector2 polyCoords = new Vector2(item[0], item[1]);

                        points.Add(new[]
                        {
                            polyCoords.X,
                            polyCoords.Y
                        });
                    }

                    MapEntity border = this._mapUtil.AddBorder(coords.X, coords.Y, points.ToArray(), Color.DarkOrange, 4);
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

        return this._moduleSettings.DynamicEventsRenderDistance.Value >= worldEntity.DistanceToPlayer;

        return true;
    }

    public async Task AddDynamicEventsToWorld()
    {
        GameService.Graphics.World.RemoveEntities(this._worldEntities.Values.SelectMany(v => v));
        this._worldEntities?.Clear();

        if (!this._moduleSettings.ShowDynamicEventInWorld.Value || !GameService.Gw2Mumble.IsAvailable)
        {
            return;
        }

        bool success = await this._dynamicEventService.WaitForCompletion(TimeSpan.FromMinutes(5));

        if (!success)
        {
            Logger.Debug("DynamicEventService did not finish in the given timespan. Abort.");
            return;
        }

        int mapId = GameService.Gw2Mumble.CurrentMap.Id;
        IEnumerable<DynamicEvent> events = this._dynamicEventService.GetEventsByMap(mapId)
                                               .Where(de => !this._moduleSettings.DisabledDynamicEventIds.Value.Contains(de.ID));
        if (events == null)
        {
            Logger.Debug($"No events found for map {mapId}");
            return;
        }

        Stopwatch sw = Stopwatch.StartNew();
        foreach (DynamicEvent ev in events)
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
            GameService.Graphics.World.RemoveEntities(this._worldEntities[dynamicEvent.ID]);
            this._worldEntities.TryRemove(dynamicEvent.ID, out _);
        }
    }

    public async Task AddDynamicEventToWorld(DynamicEvent dynamicEvent)
    {
        this.RemoveDynamicEventFromWorld(dynamicEvent);

        if (!this._moduleSettings.ShowDynamicEventInWorld.Value || !GameService.Gw2Mumble.IsAvailable)
        {
            return;
        }

        try
        {
            Map map = await this._apiManager.Gw2ApiClient.V2.Maps.GetAsync(dynamicEvent.MapId);
            Vector2 centerAsMapCoords = new Vector2(dynamicEvent.Location.Center[0], dynamicEvent.Location.Center[1]);
            Vector3 centerAsWorldMeters = map.MapCoordsToWorldMeters(new Vector2(centerAsMapCoords.X, centerAsMapCoords.Y));
            centerAsWorldMeters.Z = Math.Abs(dynamicEvent.Location.Center[2].ToMeters());

            List<WorldEntity> entites = new List<WorldEntity>();
            switch (dynamicEvent.Location.Type)
            {
                case "poly":
                    entites.Add(this.GetPolygone(dynamicEvent, map, centerAsWorldMeters, this.WorldEventRenderCondition));
                    break;
                case "sphere":
                    entites.Add(this.GetSphere(dynamicEvent, map, centerAsWorldMeters, this.WorldEventRenderCondition));
                    break;
                case "cylinder":
                    entites.Add(this.GetCylinder(dynamicEvent, map, centerAsWorldMeters, this.WorldEventRenderCondition));
                    break;
            }

            this._worldEntities.AddOrUpdate(dynamicEvent.ID, entites, (_, prev) => prev.Concat(entites).ToList());
            GameService.Graphics.World.AddEntities(entites);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, $"Failed to add {dynamicEvent.Name} to world.");
        }
    }

    private WorldEntity GetSphere(DynamicEvent ev, Map map, Vector3 centerAsWorldMeters, Func<WorldEntity, bool> renderCondition)
    {
        int tessellation = 50;
        int connections = tessellation / 5;

        if (connections > tessellation)
        {
            throw new ArgumentOutOfRangeException("connections", "connections can't be greater than tessellation");
        }

        float radius = ev.Location.Radius.ToMeters();

        List<Vector3> points = new List<Vector3>();

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
        bool first = true;
        points = points.SelectMany(t =>
        {
            IEnumerable<Vector3> arr = Enumerable.Repeat(t, first ? 1 : 2);
            first = false;
            return arr;
        }).ToList();

        // Connect end to start
        points.Add(points[0]);

        //var polygones = new List<WorldPolygone>();
        //polygones.Add(new WorldPolygone(centerAsWorldMeters, points.ToArray(), Color.White, renderCondition));

        int connectionSteps = tessellation / connections;
        float bendSteps = 12f;

        List<Vector3> connectionPoints = new List<Vector3>();

        for (int p = 0; p < tessellation; p += connectionSteps)
        {
            Vector3 point = points[p * 2];
            Vector3 mid = new Vector3(point.X, point.Y, 0);

            List<Vector3> bendPointsUp = new List<Vector3>();
            Vector3 up = new Vector3(0, 0, radius);
            Vector3 centerUp = new Vector3(mid.X + up.X, mid.Y + up.Y, mid.Z + up.Z);

            for (float ratio = 0; ratio <= 1; ratio += 1 / bendSteps)
            {
                Vector3 tangent1 = Vector3.Lerp(mid, centerUp, ratio);
                Vector3 tangent2 = Vector3.Lerp(centerUp, up, ratio);
                Vector3 curve = Vector3.Lerp(tangent1, tangent2, ratio);

                bendPointsUp.Add(curve);
            }

            bool bendPointsUpFirst = true;
            bendPointsUp = bendPointsUp.SelectMany(t =>
            {
                IEnumerable<Vector3> arr = Enumerable.Repeat(t, bendPointsUpFirst ? 1 : 2);
                bendPointsUpFirst = false;
                return arr;
            }).ToList();
            bendPointsUp.RemoveAt(bendPointsUp.Count - 1);
            connectionPoints.AddRange(bendPointsUp);
            //polygones.Add(new WorldPolygone(centerAsWorldMeters, bendPointsUp.ToArray(), Color.White, renderCondition));

            Vector3 down = new Vector3(0, 0, -radius);
            List<Vector3> bendPointsDown = new List<Vector3>();
            Vector3 centerDown = new Vector3(mid.X + down.X, mid.Y + down.Y, mid.Z + down.Z);

            for (float ratio = 0; ratio <= 1; ratio += 1 / bendSteps)
            {
                Vector3 tangent1 = Vector3.Lerp(mid, centerDown, ratio);
                Vector3 tangent2 = Vector3.Lerp(centerDown, down, ratio);
                Vector3 curve = Vector3.Lerp(tangent1, tangent2, ratio);

                bendPointsDown.Add(curve);
            }

            bool bendPointsDownFirst = true;
            bendPointsDown = bendPointsDown.SelectMany(t =>
            {
                IEnumerable<Vector3> arr = Enumerable.Repeat(t, bendPointsDownFirst ? 1 : 2);
                bendPointsDownFirst = false;
                return arr;
            }).ToList();
            bendPointsDown.RemoveAt(bendPointsDown.Count - 1);
            connectionPoints.AddRange(bendPointsDown);
        }

        IEnumerable<Vector3> allPoints = points.Concat(connectionPoints);
        return new WorldPolygone(centerAsWorldMeters, allPoints.ToArray(), Color.White, renderCondition);
    }

    private WorldEntity GetCylinder(DynamicEvent ev, Map map, Vector3 centerAsWorldMeters, Func<WorldEntity, bool> renderCondition)
    {
        int tessellation = 50;
        int connections = tessellation / 4;

        if (connections > tessellation)
        {
            throw new ArgumentOutOfRangeException("connections", "connections can't be greater than tessellation");
        }

        float radius = ev.Location.Radius.ToMeters();

        float height = ev.Location.Height.ToMeters();

        List<Vector3> points = new List<Vector3>();

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
        bool first = true;
        points = points.SelectMany(t =>
        {
            IEnumerable<Vector3> arr = Enumerable.Repeat(t, first ? 1 : 2);
            first = false;
            return arr;
        }).ToList();

        // Connect end to start
        points.Add(points[0]);

        double[] zRanges =
        {
            0,
            height
        };
        Vector3[][] perZRangePoints = zRanges.OrderBy(z => z).Select(z =>
        {
            Vector3[] p = points.Select(mp => new Vector3(mp.X, mp.Y, (float)z)).ToArray();
            return p;
        }).ToArray();

        List<Vector3> connectPoints = new List<Vector3>();

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
                Vector3[] curr = perZRangePoints[i];
                Vector3[] next = perZRangePoints[i + 1];

                if (curr.Length != next.Length)
                {
                    throw new ArgumentOutOfRangeException("WorldPolygone.Points", "Length does not match.");
                }

                connectPoints.Add(new Vector3(x, y, curr[0].Z));
                connectPoints.Add(new Vector3(x, y, next[0].Z));
            }
        }

        IEnumerable<Vector3> allPoints = perZRangePoints.SelectMany(x => x).Concat(connectPoints);

        return new WorldPolygone(centerAsWorldMeters, allPoints.ToArray(), Color.White, renderCondition);
    }

    private WorldEntity GetPolygone(DynamicEvent dynamicEvent, Map map, Vector3 centerAsWorldMeters, Func<WorldEntity, bool> renderCondition)
    {
        // Map all event points to world coordinates
        Vector3[] points = dynamicEvent.Location.Points.Select(p =>
        {
            Vector2 mapCoords = new Vector2(p[0], p[1]);
            Vector3 worldCoords = map.MapCoordsToWorldMeters(new Vector2(mapCoords.X, mapCoords.Y));
            Vector3 vector = new Vector3(worldCoords.X, worldCoords.Y, centerAsWorldMeters.Z);
            return vector;
        }).ToArray();

        // Polygone needs everything double
        bool first = true;
        points = points.SelectMany(t =>
        {
            IEnumerable<Vector3> arr = Enumerable.Repeat(t, first ? 1 : 2);
            first = false;
            return arr;
        }).ToArray();

        // Connect end to start
        List<Vector3> pointList = points.ToList();
        pointList.Add(points[0]);
        points = pointList.ToArray();

        // Polygone needs stuff as offset from center
        Vector3[] mappedPoints = new Vector3[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 curPoint = points[i];
            Vector3 offset = curPoint - centerAsWorldMeters;

            mappedPoints[i] = offset;
        }

        Vector3[][] perZRangePoints = dynamicEvent.Location.ZRange.OrderBy(z => z).Select(z => centerAsWorldMeters.Z + z.ToMeters()).Select(z =>
        {
            Vector3[] p = mappedPoints.Select(mp => new Vector3(mp.X, mp.Y, z)).ToArray();
            return p;
        }).ToArray();

        List<Vector3> connectPoints = new List<Vector3>();
        for (int i = 0; i < perZRangePoints.Length - 1; i++)
        {
            Vector3[] curr = perZRangePoints[i];
            Vector3[] next = perZRangePoints[i + 1];

            if (curr.Length != next.Length)
            {
                throw new ArgumentOutOfRangeException("points", "Length does not match.");
            }

            for (int p = 0; p < curr.Length; p++)
            {
                Vector3 currP = curr[p];
                Vector3 nextP = next[p];
                connectPoints.Add(currP);
                connectPoints.Add(nextP);
            }
        }

        IEnumerable<Vector3> allPoints = perZRangePoints.SelectMany(x => x).Concat(connectPoints);

        return new WorldPolygone(centerAsWorldMeters, allPoints.ToArray(), Color.White, renderCondition);
    }

    private void CheckLostEntityReferences()
    {
        IEnumerable<IEntity> lostEntities = GameService.Graphics.World.Entities.Where(e => e is WorldEntity);
        bool hasEntities = lostEntities.Any();

        if (!this._notifiedLostEntities && !this._moduleSettings.ShowDynamicEventInWorld.Value && hasEntities)
        {
            try
            {
                this.FoundLostEntities?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception) { }

            this._notifiedLostEntities = true;
        }

        if (this._moduleSettings.ShowDynamicEventInWorld.Value)
        {
            this._notifiedLostEntities = false;
        }
    }
}