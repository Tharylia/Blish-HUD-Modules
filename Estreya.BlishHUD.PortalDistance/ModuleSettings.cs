namespace Estreya.BlishHUD.PortalDistance;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Shared.Settings;

public class ModuleSettings : BaseModuleSettings
{
    public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding()) { }

    protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
    {
        this.ManualKeyBinding = globalSettingCollection.DefineSetting(nameof(this.ManualKeyBinding), new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.P), () => "Manual KeyBinding", () => "Defines the key for manual activation of the distance measurement.");
        this.ManualKeyBinding.Value.Enabled = true;
        this.ManualKeyBinding.Value.BlockSequenceFromGw2 = true;
        this.ManualKeyBinding.Value.IgnoreWhenInTextField = true;

        this.UseArcDPS = globalSettingCollection.DefineSetting(nameof(this.UseArcDPS), true, () => "Use ArcDPS", () => "Whether the module tries to auto detect portal usage. Requires a restart. (YOU NEED TO STAND STILL UNTIL BUFF IS REGISTERED)");
    }

    public SettingEntry<KeyBinding> ManualKeyBinding { get; private set; }

    public SettingEntry<bool> UseArcDPS { get; private set; }
}