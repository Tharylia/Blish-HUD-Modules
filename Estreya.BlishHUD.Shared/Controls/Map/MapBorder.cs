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
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Windows;

    public class MapBorder : MapEntity
    {
        private static Logger Logger = Logger.GetLogger<MapBorder>();

        private readonly float _x;
        private readonly float _y;
        private readonly float[][] _points;
        private readonly Color _color;
        private readonly float _thickness;

        public MapBorder(float x, float y, float[][] points, Color color, float thickness = 1 )
        {
            this._x = x;
            this._y = y;
            this._points = points;
            this._color = color;
            this._thickness = thickness;
        }

        public override RectangleF? RenderToMiniMap(SpriteBatch spriteBatch, Rectangle bounds, double offsetX, double offsetY, double scale, float opacity)
        {
            var location = this.GetScaledLocation(this._x, this._y, scale, offsetX, offsetY);

            //Logger.Debug($"Location: {location} - OffsetX: {offsetX} - OffsetY: {offsetY} - Scale: {scale}");

            var points = this._points.Select(p => this.GetScaledLocation(p[0], p[1], scale, offsetX, offsetY)).ToList().AsReadOnly();
            spriteBatch.DrawPolygon(Vector2.Zero, points, _color, _thickness);

            var top = points.Min(p => p.Y);
            var bottom = points.Max(p => p.Y);
            var left = points.Min(p => p.X);
            var right = points.Max(p => p.X);

            return new RectangleF(left, top, right - left, bottom - top);
        }
    }
}
