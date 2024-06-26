﻿namespace Estreya.BlishHUD.Shared.Utils;

using Blish_HUD;
using Blish_HUD.Controls.Extern;
using Blish_HUD.Controls.Intern;
using Blish_HUD.Input;
using Blish_HUD.Modules.Managers;
using Controls;
using Controls.Map;
using Extensions;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;
using Keyboard = Blish_HUD.Controls.Intern.Keyboard;
using Mouse = Blish_HUD.Controls.Intern.Mouse;
using Point = System.Drawing.Point;

public class MapUtil : IDisposable
{
    public enum ChangeMapLayerDirection
    {
        Up,
        Down
    }

    private static readonly Logger Logger = Logger.GetLogger(typeof(MapUtil));
    private readonly Gw2ApiManager _apiManager;
    private readonly KeyBinding _mapKeybinding;

    private FlatMap _flatMap;

    public MapUtil(KeyBinding mapKeybinding, Gw2ApiManager apiManager)
    {
        this._mapKeybinding = mapKeybinding;
        this._apiManager = apiManager;

        this._flatMap = new FlatMap { Parent = GameService.Graphics.SpriteScreen };
    }

    public static int MouseMoveAndClickDelay { get; set; } = 50;
    public static int KeyboardPressDelay { get; set; } = 20;

    public void Dispose()
    {
        this._flatMap?.Dispose();
        this._flatMap = null;
    }

    private double GetDistance(double x1, double y1, double x2, double y2)
    {
        return this.GetDistance(x2 - x1, y2 - y1);
    }

    private double GetDistance(double offsetX, double offsetY)
    {
        return Math.Sqrt(Math.Pow(offsetX, 2) + Math.Pow(offsetY, 2));
    }

    private async Task WaitForTick(int ticks = 1)
    {
        int tick = GameService.Gw2Mumble.Tick;
        while (GameService.Gw2Mumble.Tick - tick < ticks * 2)
        {
            await Task.Delay(10);
        }
    }

    public async Task WaitForMapClose(int delay = 10)
    {
        while (GameService.Gw2Mumble.UI.IsMapOpen)
        {
            await Task.Delay(delay);
        }
    }

    public async Task<bool> OpenFullscreenMap()
    {
        if (!this.IsInGame())
        {
            Logger.Debug("Not in game");
            return false;
        }

        if (GameService.Gw2Mumble.UI.IsMapOpen)
        {
            return true;
        }

        // Consider pressing the open map icon in the UI.
        if (this._mapKeybinding.ModifierKeys != ModifierKeys.None)
        {
            VirtualKeyShort modifier = this._mapKeybinding.ModifierKeys.GetFlags().Select(flag => (VirtualKeyShort)flag).Aggregate((a, b) => a | b);
            Keyboard.Press(modifier);
        }

        Keyboard.Stroke((VirtualKeyShort)this._mapKeybinding.PrimaryKey);

        if (this._mapKeybinding.ModifierKeys != ModifierKeys.None)
        {
            VirtualKeyShort modifier = this._mapKeybinding.ModifierKeys.GetFlags().Select(flag => (VirtualKeyShort)flag).Aggregate((a, b) => a | b);
            Keyboard.Release(modifier);
        }

        await Task.Delay(500);

        return GameService.Gw2Mumble.UI.IsMapOpen;
    }

    public async Task<bool> CloseFullscreenMap()
    {
        if (!this.IsInGame())
        {
            Logger.Debug("Not in game");
            return false;
        }

        if (!GameService.Gw2Mumble.UI.IsMapOpen)
        {
            return true;
        }

        Keyboard.Press(VirtualKeyShort.ESCAPE);

        await Task.Delay(500);

        return !GameService.Gw2Mumble.UI.IsMapOpen;
    }

    private bool IsInGame()
    {
        return GameService.GameIntegration.Gw2Instance.IsInGame;
    }

    private async Task<bool> Zoom(double requiredZoomLevel, int steps)
    {
        if (!this.IsInGame())
        {
            Logger.Debug("Not in game");
            return false;
        }

        int maxTries = 10;
        int remainingTries = maxTries;
        double startZoom = this.GetMapScale();

        bool isZoomIn = steps > 0;

        while (isZoomIn ? startZoom > requiredZoomLevel : startZoom < requiredZoomLevel)
        {
            await this.WaitForTick(2);
            if (!GameService.Gw2Mumble.UI.IsMapOpen)
            {
                Logger.Debug("User closed map.");
                return false;
            }

            Mouse.RotateWheel(steps);
            Mouse.RotateWheel(steps);
            Mouse.RotateWheel(steps);
            Mouse.RotateWheel(steps);
            await this.WaitForTick();

            double zoomAterScroll = this.GetMapScale();

            Logger.Debug($"Scrolled from {startZoom} to {zoomAterScroll}");

            if (startZoom == zoomAterScroll)
            {
                remainingTries--;

                if (remainingTries <= 0)
                {
                    return false;
                }
            }
            else
            {
                remainingTries = maxTries;
            }

            startZoom = zoomAterScroll;
        }

        return true;
    }

    public Task<bool> ZoomOut(double requiredZoomLevel)
    {
        return this.Zoom(requiredZoomLevel, -int.MaxValue);
    }

    public Task<bool> ZoomIn(double requiredZoomLevel)
    {
        return this.Zoom(requiredZoomLevel, int.MaxValue);
    }

    private double GetMapScale()
    {
        return GameService.Gw2Mumble.UI.MapScale * GameService.Graphics.UIScaleMultiplier;
    }

    /// <summary>
    ///     Moves the map to the specified continent coordinates.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="targetDistance"></param>
    /// <returns></returns>
    private async Task<bool> MoveMap(double x, double y, double targetDistance)
    {
        while (true)
        {
            if (!this.IsInGame())
            {
                Logger.Debug("Not in game");
                return false;
            }

            await this.WaitForTick(2);
            if (!GameService.Gw2Mumble.UI.IsMapOpen)
            {
                Logger.Debug("User closed map.");
                return false;
            }

            Coordinates2 mapPos = GameService.Gw2Mumble.UI.MapCenter;

            double offsetX = mapPos.X - x;
            double offsetY = mapPos.Y - y;

            Logger.Debug($"Distance remaining: {this.GetDistance(mapPos.X, mapPos.Y, x, y)}");
            Logger.Debug($"Map Position: {GameService.Gw2Mumble.UI.MapPosition.X}, {GameService.Gw2Mumble.UI.MapPosition.Y}");

            double distance = Math.Sqrt(Math.Pow(offsetX, 2) + Math.Pow(offsetY, 2));
            if (distance < targetDistance)
            {
                break;
            }
            //Logger.Debug($"Distance remaining: {offsetX}, {offsetY}");

            Mouse.SetPosition(GameService.Graphics.WindowWidth / 2, GameService.Graphics.WindowHeight / 2);

            Point startPos = Mouse.GetPosition();
            Mouse.Press(MouseButton.RIGHT);
            Mouse.SetPosition(startPos.X + (int)MathHelper.Clamp((float)offsetX / (float)(this.GetMapScale() * 0.9d), -100000, 100000),
                startPos.Y + (int)MathHelper.Clamp((float)offsetY / (float)(this.GetMapScale() * 0.9d), -100000, 100000));

            await this.WaitForTick();
            startPos = Mouse.GetPosition();
            Mouse.SetPosition(startPos.X + (int)MathHelper.Clamp((float)offsetX / (float)(this.GetMapScale() * 0.9d), -100000, 100000),
                startPos.Y + (int)MathHelper.Clamp((float)offsetY / (float)(this.GetMapScale() * 0.9d), -100000, 100000));

            Mouse.Release(MouseButton.RIGHT);

            await Task.Delay(MouseMoveAndClickDelay);
        }

        return true;
    }

    public async Task<bool> ChangeMapLayer(ChangeMapLayerDirection direction)
    {
        if (!this.IsInGame())
        {
            Logger.Debug("Not in game");
            return false;
        }

        Keyboard.Press(VirtualKeyShort.SHIFT);
        await Task.Delay(KeyboardPressDelay);
        Mouse.RotateWheel(int.MaxValue * (direction == ChangeMapLayerDirection.Up ? 1 : -1));
        await Task.Delay(KeyboardPressDelay);
        Keyboard.Release(VirtualKeyShort.SHIFT);

        return true;
    }

    public Task<NavigationResult> NavigateToPosition(ContinentFloorRegionMapPoi poi)
    {
        return this.NavigateToPosition(poi, false);
    }

    public Task<NavigationResult> NavigateToPosition(ContinentFloorRegionMapPoi poi, bool directTeleport)
    {
        return this.NavigateToPosition(poi.Coord.X, poi.Coord.Y, poi.Type == PoiType.Waypoint, directTeleport);
    }

    public Task<NavigationResult> NavigateToPosition(double x, double y)
    {
        return this.NavigateToPosition(x, y, false, false);
    }

    public async Task<NavigationResult> NavigateToPosition(double x, double y, bool isWaypoint, bool directTeleport)
    {
        try
        {
            if (!this.IsInGame())
            {
                Logger.Debug("Not in game");
                return new NavigationResult(false, "Not in game.");
            }

            if (!await this.OpenFullscreenMap())
            {
                Logger.Debug("Could not open map.");
                return new NavigationResult(false, "Could not open map.");
            }

            ScreenNotification.ShowNotification(new[]
            {
                "DO NOT MOVE THE CURSOR!",
                "Close map to cancel."
            }, ScreenNotification.NotificationType.Warning, duration: 7);

            await this.WaitForTick();

            Coordinates2 mapPos = GameService.Gw2Mumble.UI.MapCenter;

            Mouse.SetPosition(GameService.Graphics.WindowWidth / 2, GameService.Graphics.WindowHeight / 2);

            if (GameService.Gw2Mumble.CurrentMap.Id == 1206) // Mistlock Santuary
            {
                if (!await this.ChangeMapLayer(ChangeMapLayerDirection.Down))
                {
                    Logger.Debug("Changing map layer failed.");
                    return new NavigationResult(false, "Changing map layer failed.");
                }
            }

            if (!await this.ZoomOut(6))
            {
                Logger.Debug("Zooming out did not work.");
                return new NavigationResult(false, "Zooming out did not work.");
            }

            double totalDist = this.GetDistance(mapPos.X, mapPos.Y, x, y) / (this.GetMapScale() * 0.9d);

            Logger.Debug($"Distance: {totalDist}");

            if (!await this.MoveMap(x, y, 50))
            {
                Logger.Debug("Moving the map did not work.");
                return new NavigationResult(false, "Moving the map did not work.");
            }

            await this.WaitForTick();

            int finalMouseX = GameService.Graphics.WindowWidth / 2;
            int finalMouseY = GameService.Graphics.WindowHeight / 2;

            Logger.Debug($"Set mouse on waypoint: x = {finalMouseX}, y = {finalMouseY}");

            Mouse.SetPosition(finalMouseX, finalMouseY, true);

            if (!await this.ZoomIn(2))
            {
                Logger.Debug("Zooming in did not work.");
                return new NavigationResult(false, "Zooming in did not work.");
            }

            if (!await this.MoveMap(x, y, 5))
            {
                Logger.Debug("Moving the map did not work.");
                return new NavigationResult(false, "Moving the map did not work.");
            }

            if (isWaypoint)
            {
                Logger.Debug($"Set mouse on waypoint: x = {finalMouseX}, y = {finalMouseY}");

                Mouse.SetPosition(finalMouseX, finalMouseY, true);

                await Task.Delay(MouseMoveAndClickDelay);

                Mouse.Click(MouseButton.LEFT);

                await Task.Delay(MouseMoveAndClickDelay);

                finalMouseX -= 50;
                finalMouseY += 10;
                Logger.Debug($"Set mouse on waypoint yes button: x = {finalMouseX}, y = {finalMouseY}");
                Mouse.SetPosition(finalMouseX, finalMouseY, true);

                if (directTeleport)
                {
                    await Task.Delay(250); // Wait for teleport window to open.
                    Mouse.Click(MouseButton.LEFT);
                }
            }

            return new NavigationResult(true, null);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Navigation to position failed:");
            return new NavigationResult(false, ex.Message);
        }
    }

    public MapEntity AddEntity(MapEntity entity)
    {
        this._flatMap.AddEntity(entity);
        return entity;
    }

    public MapEntity AddCircle(double x, double y, double radius, Color color, float thickness = 1)
    {
        MapCircle circle = new MapCircle((float)x, (float)y, (float)radius, color, thickness);
        this._flatMap.AddEntity(circle);

        return circle;
    }

    public MapEntity AddBorder(double x, double y, float[][] points, Color color, float thickness = 1)
    {
        MapBorder border = new MapBorder((float)x, (float)y, points, color, thickness);
        this._flatMap.AddEntity(border);

        return border;
    }

    public void ClearMapEntities()
    {
        this._flatMap.ClearEntities();
    }

    public void RemoveEntity(MapEntity mapEntity)
    {
        this.RemoveEntities(mapEntity);
    }
    public void RemoveEntities(params MapEntity[] mapEntity)
    {
        foreach (MapEntity entity in mapEntity)
        {
            this._flatMap.RemoveEntity(entity);
        }
    }

    private async Task<NavigationResult> MoveMouse(int x, int y, bool sendToSystem = false)
    {
        Point startPos = Mouse.GetPosition();
        Mouse.SetPosition(x, y, sendToSystem);

        await this.WaitForTick();

        return new NavigationResult(true, null);
    }

    public class NavigationResult
    {
        public NavigationResult(bool success, string message)
        {
            this.Success = success;
            this.Message = message;
        }

        public bool Success { get; set; }
        public string Message { get; set; }
    }
}