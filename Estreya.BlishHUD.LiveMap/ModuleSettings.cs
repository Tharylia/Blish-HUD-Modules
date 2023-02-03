namespace Estreya.BlishHUD.LiveMap;

using Blish_HUD.Settings;
using Estreya.BlishHUD.LiveMap.Models;
using Estreya.BlishHUD.Shared.Settings;

public class ModuleSettings: BaseModuleSettings
{
    public SettingEntry<PlayerFacingType> PlayerFacingType { get; private set; }
    public SettingEntry<PublishType> PublishType { get; private set; }

    public SettingEntry<bool> HideCommander { get; private set; }

    public SettingEntry<bool> StreamerModeEnabled { get; private set; }

    public ModuleSettings(SettingCollection settings) : base(settings, new Blish_HUD.Input.KeyBinding())
    {
        this.RegisterCornerIcon.Value = false;

        this.PlayerFacingType = settings.DefineSetting(nameof(this.PlayerFacingType), Models.PlayerFacingType.Camera, () => "Player Facing Type", () => "Defines the type with which your player facing gets displayed.");
        this.PublishType = settings.DefineSetting(nameof(this.PublishType), Models.PublishType.Both, () => "Publish Type", () => "Defines the scope where your position should be published to.");
        this.HideCommander = settings.DefineSetting(nameof(this.HideCommander), false, () => "Hide Commander", () => "Whether the commander tag should be hidden on the live map.");
        this.StreamerModeEnabled = settings.DefineSetting(nameof(this.StreamerModeEnabled), false, () => "Streamer Mode Enabled", () => "Whether the module should stop sending the position when a streaming program is detected.");
    }
}
