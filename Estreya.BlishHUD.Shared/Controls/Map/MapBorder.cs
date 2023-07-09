namespace Estreya.BlishHUD.Shared.Controls.Map;

using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System.Collections.ObjectModel;
using System.Linq;

public class MapBorder : MapEntity
{
    private static Logger Logger = Logger.GetLogger<MapBorder>();
    private readonly Color _color;
    private readonly float[][] _points;
    private readonly float _thickness;

    private readonly float _x;
    private readonly float _y;

    public MapBorder(float x, float y, float[][] points, Color color, float thickness = 1)
    {
        this._x = x;
        this._y = y;
        this._points = points;
        this._color = color;
        this._thickness = thickness;
    }

    public override RectangleF? RenderToMiniMap(SpriteBatch spriteBatch, Rectangle bounds, double offsetX, double offsetY, double scale, float opacity)
    {
        Vector2 location = this.GetScaledLocation(this._x, this._y, scale, offsetX, offsetY);

        //Logger.Debug($"Location: {location} - OffsetX: {offsetX} - OffsetY: {offsetY} - Scale: {scale}");

        ReadOnlyCollection<Vector2> points = this._points.Select(p => this.GetScaledLocation(p[0], p[1], scale, offsetX, offsetY)).ToList().AsReadOnly();
        spriteBatch.DrawPolygon(Vector2.Zero, points, this._color, this._thickness);

        float top = points.Min(p => p.Y);
        float bottom = points.Max(p => p.Y);
        float left = points.Min(p => p.X);
        float right = points.Max(p => p.X);

        return new RectangleF(left, top, right - left, bottom - top);
    }
}