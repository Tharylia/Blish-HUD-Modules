namespace Estreya.BlishHUD.Browser;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Humanizer.Localisation;
using Microsoft.Xna.Framework.Input;
using Shared.Settings;
using System.Collections.Generic;

public class ModuleSettings : BaseModuleSettings
{
    public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(ModifierKeys.Alt, Keys.B)) { }

    public override void Unload()
    {
        base.Unload();
    }
}