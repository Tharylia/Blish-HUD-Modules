namespace Estreya.BlishHUD.Automations;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Shared.Settings;

public class ModuleSettings : BaseModuleSettings
{
    public ModuleSettings(SettingCollection settings, SemVer.Version moduleVersion) : base(settings, moduleVersion, new KeyBinding())
    {
    }
}