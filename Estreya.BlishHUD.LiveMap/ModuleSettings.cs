namespace Estreya.BlishHUD.LiveMap;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Shared.Settings;

public class ModuleSettings : BaseModuleSettings
{
    public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding())
    {
        this.RegisterCornerIcon.Value = false;

        this.HideCommander = settings.DefineSetting(nameof(this.HideCommander), false, () => "Hide Commander", () => "Whether the commander tag should be hidden on the live map.");
        this.StreamerModeEnabled = settings.DefineSetting(nameof(this.StreamerModeEnabled), true, () => "Streamer Mode Enabled", () => "Whether the module should stop sending the position when a streaming program is detected.");

        this.FollowOnMap = settings.DefineSetting(nameof(this.FollowOnMap), true, () => "Follow on Map", () => "Whether the map should follow the player if opened via the module.");
        this.SendGroupInformation = settings.DefineSetting(nameof(this.SendGroupInformation), true, () => "Send Group Information", () => "Whether the module should publish your current group informations.");
    }

    public SettingEntry<bool> HideCommander { get; private set; }

    public SettingEntry<bool> StreamerModeEnabled { get; private set; }

    public SettingEntry<bool> FollowOnMap { get; private set; }

    public SettingEntry<bool> SendGroupInformation { get; private set; }
}