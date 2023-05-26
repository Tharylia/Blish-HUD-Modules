namespace Estreya.BlishHUD.Shared.Models;

using Blish_HUD.Settings;
using Microsoft.Xna.Framework;

public class ScreenRegion
{
    private readonly SettingEntry<Point> _location;
    private readonly SettingEntry<Point> _size;

    private Rectangle? _bounds;

    public ScreenRegion(string regionName, SettingEntry<Point> location, SettingEntry<Point> size)
    {
        this.RegionName = regionName;
        this._location = location;
        this._size = size;
    }

    public Rectangle Bounds => this._bounds ?? new Rectangle(this.Location, this.Size);

    public string RegionName { get; set; }

    public Point Location
    {
        get => this._location.Value;
        set
        {
            this._location.Value = value;
            this._bounds = null;
        }
    }

    public Point Size
    {
        get => this._size.Value;
        set
        {
            this._size.Value = value;
            this._bounds = null;
        }
    }
}