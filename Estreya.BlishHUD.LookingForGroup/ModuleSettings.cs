namespace Estreya.BlishHUD.LookingForGroup;

using Blish_HUD;
using Blish_HUD.Input;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework.Input;
using Shared.Models.Drawers;
using Shared.Services;
using Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

public class ModuleSettings : BaseModuleSettings
{
    
    public ModuleSettings(SettingCollection settings, SemVer.Version moduleVersion) : base(settings, moduleVersion, new KeyBinding(ModifierKeys.Alt, Keys.E))
    {
    }
}