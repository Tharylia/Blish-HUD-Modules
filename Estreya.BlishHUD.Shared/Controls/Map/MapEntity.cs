namespace Estreya.BlishHUD.Shared.Controls.Map
{
    using Blish_HUD.Entities;
    using Blish_HUD;
    using Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;
    using static Glide.MemberLerper;
    using MonoGame.Extended;

    public abstract class MapEntity : IDisposable
    {
        public event EventHandler Disposed;
        public string TooltipText { get; set; }

        public abstract RectangleF? RenderToMiniMap(SpriteBatch spriteBatch, Rectangle bounds, double offsetX, double offsetY, double scale, float opacity);

        public void Dispose()
        {
            this.Disposed?.Invoke(this, EventArgs.Empty);
            this.InternalDispose();
        }

        protected virtual void InternalDispose() { }

        protected Vector2 GetScaledLocation(double x, double y, double scale, double offsetX, double offsetY)
        {
            //_packState.MapStates.EventCoordsToMapCoords(x, y, out double mapX, out double mapY);
            var mapX = x;
            var mapY = y;

            var scaledLocation = new Vector2((float)((mapX - GameService.Gw2Mumble.UI.MapCenter.X) / scale),
                                             (float)((mapY - GameService.Gw2Mumble.UI.MapCenter.Y) / scale));

            if (!GameService.Gw2Mumble.UI.IsMapOpen && GameService.Gw2Mumble.UI.IsCompassRotationEnabled)
            {
                scaledLocation = Vector2.Transform(scaledLocation, Matrix.CreateRotationZ((float)GameService.Gw2Mumble.UI.CompassRotation));
            }

            scaledLocation += new Vector2((float)offsetX, (float)offsetY);

            return scaledLocation;
        }
    }
}
