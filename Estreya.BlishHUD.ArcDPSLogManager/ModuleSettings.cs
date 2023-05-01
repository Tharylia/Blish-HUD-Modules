namespace Estreya.BlishHUD.ArcDPSLogManager;

using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Models.Drawers;
using Estreya.BlishHUD.Shared.Settings;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Documents;

public class ModuleSettings : BaseModuleSettings
{
    public ModuleSettings(SettingCollection settings) : base(settings, new Blish_HUD.Input.KeyBinding())
    {
    }
}
