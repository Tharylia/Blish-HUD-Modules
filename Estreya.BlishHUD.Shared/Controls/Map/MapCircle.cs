namespace Estreya.BlishHUD.Shared.Controls.Map
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text;

    public class MapCircle : MapEntity
    {
        private static Logger Logger = Logger.GetLogger<MapCircle>();

        private readonly float _x;
        private readonly float _y;
        private readonly float _radius;
        private readonly Color _color;
        private readonly float _thickness;

        public MapCircle(float x, float y, float radius, Color color, float thickness = 1)
        {
            this._x = x;
            this._y = y;
            this._radius = radius;
            this._color = color;
            this._thickness = thickness;
        }

        public override RectangleF? RenderToMiniMap(SpriteBatch spriteBatch, Rectangle bounds, double offsetX, double offsetY, double scale, float opacity)
        {
            var location = this.GetScaledLocation(this._x, this._y, scale, offsetX, offsetY);

            float radius = (this._radius * 2) / (float)scale;

            //Logger.Debug($"Location: {location} - OffsetX: {offsetX} - OffsetY: {offsetY} - Scale: {scale}");

            var circle = new CircleF(new Point2(location.X, location.Y), radius);
            spriteBatch.DrawCircle(circle,50, _color, _thickness);
            return circle.ToRectangleF();
        }
    }
}
