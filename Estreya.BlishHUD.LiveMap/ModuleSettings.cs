namespace Estreya.BlishHUD.LiveMap;

using Blish_HUD.Settings;
using Estreya.BlishHUD.LiveMap.Models;
using Estreya.BlishHUD.Shared.Settings;

public class ModuleSettings: BaseModuleSettings
{
    public SettingEntry<PlayerFacingType> PlayerFacingType { get; private set; }
    public SettingEntry<PublishType> PublishType { get; private set; }

    public ModuleSettings(SettingCollection settings) : base(settings, new Blish_HUD.Input.KeyBinding())
    {
        this.RegisterCornerIcon.Value = false;

        this.PlayerFacingType = settings.DefineSetting(nameof(this.PlayerFacingType), Models.PlayerFacingType.Camera, () => "Player Facing Type", () => "Defines the type with which your player facing gets displayed.");
        this.PublishType = settings.DefineSetting(nameof(this.PublishType), Models.PublishType.Both, () => "Publish Type", () => "Defines the scope where your position should be published to.");
    }
}
