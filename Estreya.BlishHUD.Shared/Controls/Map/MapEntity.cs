namespace Estreya.BlishHUD.Shared.Controls.Map;

using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;

public abstract class MapEntity : IDisposable
{
    protected Logger Logger;
    public string TooltipText { get; set; }

    public MapEntity()
    {
         this.Logger = Logger.GetLogger(this.GetType());
    }

    public void Dispose()
    {
        this.Disposed?.Invoke(this, EventArgs.Empty);
        this.InternalDispose();
    }

    public event EventHandler Disposed;

    public abstract RectangleF? RenderToMiniMap(SpriteBatch spriteBatch, Rectangle bounds, double offsetX, double offsetY, double scale, float opacity);

    protected virtual void InternalDispose() { }

    protected Vector2 GetScaledLocation(double x, double y, double scale, double offsetX, double offsetY)
    {
        //_packState.MapStates.EventCoordsToMapCoords(x, y, out double mapX, out double mapY);
        double mapX = x;
        double mapY = y;

        Vector2 scaledLocation = new Vector2((float)((mapX - GameService.Gw2Mumble.UI.MapCenter.X) / scale),
            (float)((mapY - GameService.Gw2Mumble.UI.MapCenter.Y) / scale));

        if (!GameService.Gw2Mumble.UI.IsMapOpen && GameService.Gw2Mumble.UI.IsCompassRotationEnabled)
        {
            scaledLocation = Vector2.Transform(scaledLocation, Matrix.CreateRotationZ((float)GameService.Gw2Mumble.UI.CompassRotation));
        }

        scaledLocation += new Vector2((float)offsetX, (float)offsetY);

        return scaledLocation;
    }
}