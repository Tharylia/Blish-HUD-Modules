namespace Estreya.BlishHUD.Shared.Models.Drawers;

using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Blish_HUD.ContentService;

public class DrawerConfiguration
{
    public string Name { get; init; }

    public DrawerLocation Location { get; init; }

    public DrawerSize Size { get; init; }

    public SettingEntry<BuildDirection> BuildDirection { get; init; }

    public SettingEntry<float> Opacity { get; init; }

    public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> BackgroundColor { get; init; }

    public SettingEntry<FontSize> FontSize { get; init; }
}
