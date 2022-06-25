namespace Estreya.BlishHUD.Shared.Models;

using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ScreenRegion
{

    private Rectangle? _bounds;

    public Rectangle Bounds => _bounds ?? new Rectangle(this.Location, this.Size);

    private readonly SettingEntry<Point> _location;
    private readonly SettingEntry<Point> _size;

    public string RegionName { get; set; }

    public Point Location
    {
        get => _location.Value;
        set
        {
            _location.Value = value;
            _bounds = null;
        }
    }

    public Point Size
    {
        get => _size.Value;
        set
        {
            _size.Value = value;
            _bounds = null;
        }
    }

    public ScreenRegion(string regionName, SettingEntry<Point> location, SettingEntry<Point> size)
    {
        this.RegionName = regionName;
        _location = location;
        _size = size;
    }

}
