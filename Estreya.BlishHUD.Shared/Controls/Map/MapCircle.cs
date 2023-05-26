namespace Estreya.BlishHUD.Shared.Controls.Map;

using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

public class MapCircle : MapEntity
{
    private static Logger Logger = Logger.GetLogger<MapCircle>();
    private readonly Color _color;
    private readonly float _radius;
    private readonly float _thickness;

    private readonly float _x;
    private readonly float _y;

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
        Vector2 location = this.GetScaledLocation(this._x, this._y, scale, offsetX, offsetY);

        float radius = this._radius / (float)scale;

        //Logger.Debug($"Location: {location} - OffsetX: {offsetX} - OffsetY: {offsetY} - Scale: {scale}");

        CircleF circle = new CircleF(new Point2(location.X, location.Y), radius);
        spriteBatch.DrawCircle(circle, 50, this._color, this._thickness);
        return circle.ToRectangleF();
    }
}