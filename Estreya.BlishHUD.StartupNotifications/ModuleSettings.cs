namespace Estreya.BlishHUD.StartupNotifications;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Controls;
using Shared.Settings;

public class ModuleSettings : BaseModuleSettings
{
    /// <summary>
    /// The duration each notification is shown
    /// </summary>
    public SettingEntry<int> Duration { get; set; }

    public SettingEntry<ScreenNotification.NotificationType> Type { get; set; }

    public SettingEntry<bool> AwaitEach { get; set; }

    public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding())
    {
        this.RegisterCornerIcon.Value = false;
    }

    protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
    {
        this.Duration = globalSettingCollection.DefineSetting(nameof(this.Duration), 5, () => "Duration", () => "The duration each notification should be shown. (Range: 1-30 seconds)");
        this.Duration.SetRange(1, 30);

        this.Type = globalSettingCollection.DefineSetting(nameof(this.Type), ScreenNotification.NotificationType.Info, () => "Type", () => "The type each notification should be shown as.");
        this.Type.SetIncluded(ScreenNotification.NotificationType.Info, ScreenNotification.NotificationType.Warning, ScreenNotification.NotificationType.Error);

        this.AwaitEach = globalSettingCollection.DefineSetting(nameof(this.AwaitEach), true, () => "Await Each", () => "If each notification should be awaited before the next one is shown.");
    }
}