namespace Estreya.BlishHUD.ScrollingCombatText
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Settings;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using static Blish_HUD.ContentService;

    public class ModuleSettings : BaseModuleSettings
    {
        private static readonly Logger Logger = Logger.GetLogger<ModuleSettings>();

        #region Scrolling Text Areas

        #endregion


        public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.S)) { }

        protected override void InitializeAdditionalSettings(SettingCollection settings)
        {
        }

        public void AddScrollingTextArea()
        {

        }

        public override void Unload()
        {
            base.Unload();
        }
    }
}
