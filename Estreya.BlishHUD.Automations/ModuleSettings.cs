namespace Estreya.BlishHUD.Automations;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Shared.Settings;

public class ModuleSettings : BaseModuleSettings
{
    public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding())
    {
    }
}