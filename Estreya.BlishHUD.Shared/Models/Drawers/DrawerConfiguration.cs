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

    public SettingEntry<Models.FontFace> FontFace { get; set; }

    public SettingEntry<string> CustomFontPath { get; set; }

    public SettingEntry<FontSize> FontSize { get; set; }

    public void CopyTo(DrawerConfiguration config)
    {
        // Dont copy name
        config.Enabled.Value = this.Enabled.Value;
        config.EnabledKeybinding.Value = this.EnabledKeybinding.Value;
        config.Location.X.Value = this.Location.X.Value;
        config.Location.Y.Value = this.Location.Y.Value;
        config.Size.X.Value = this.Size.X.Value;
        config.Size.Y.Value = this.Size.Y.Value;
        config.BuildDirection.Value = this.BuildDirection.Value;
        config.Opacity.Value = this.Opacity.Value;
        config.BackgroundColor.Value = this.BackgroundColor.Value;
        config.TextColor.Value = this.TextColor.Value;
        config.FontFace.Value = this.FontFace.Value;
        config.CustomFontPath.Value = this.CustomFontPath.Value;
        config.FontSize.Value = this.FontSize.Value;
    }
}