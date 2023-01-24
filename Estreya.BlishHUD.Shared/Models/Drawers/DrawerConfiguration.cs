namespace Estreya.BlishHUD.Shared.Models.Drawers;

using Blish_HUD.Input;
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
    public string Name { get; set; }

    public SettingEntry<bool> Enabled { get; set; }

    public SettingEntry<KeyBinding> EnabledKeybinding { get; set; }

    public DrawerLocation Location { get; set; }

    public DrawerSize Size { get; set; }

    public SettingEntry<BuildDirection> BuildDirection { get; set; }

    public SettingEntry<float> Opacity { get; set; }

    public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> BackgroundColor { get; set; }

    public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> TextColor { get; set; }

    public SettingEntry<FontSize> FontSize { get; set; }
}
