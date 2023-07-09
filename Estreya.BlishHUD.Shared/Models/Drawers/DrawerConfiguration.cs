namespace Estreya.BlishHUD.Shared.Models.Drawers;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
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

    public SettingEntry<Color> BackgroundColor { get; set; }

    public SettingEntry<Color> TextColor { get; set; }

    public SettingEntry<FontSize> FontSize { get; set; }
}