namespace Estreya.BlishHUD.WebhookUpdater
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.WebhookUpdater.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using static Blish_HUD.ContentService;

    public class ModuleSettings : BaseModuleSettings
    {
        public SettingEntry<UpdateMode> UpdateMode { get; private set; }
        public SettingEntry<int> UpdateInterval { get; private set; } 
        public SettingEntry<bool> UpdateOnlyOnUrlOrDataChange { get; private set; }
        public SettingEntry<string> WebhookUrl { get; private set; }
        public SettingEntry<string> WebhookStringContent { get; private set; }

        public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.W)) { }

        protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
        {
            this.UpdateMode = globalSettingCollection.DefineSetting(nameof(this.UpdateMode), Models.UpdateMode.Interval, () => "Update Mode", () => "Defines the mode how updated are triggered.");
            this.UpdateMode.SettingChanged += this.UpdateMode_SettingChanged;

            this.UpdateInterval = globalSettingCollection.DefineSetting(nameof(this.UpdateInterval), 5000, () => "Update Interval", () => "Defines the interval between updated if mode is interval. Min: 100ms - Max: 60s");
            this.UpdateInterval.SetRange(100, 60000);

            this.UpdateOnlyOnUrlOrDataChange = globalSettingCollection.DefineSetting(nameof(this.UpdateOnlyOnUrlOrDataChange), true, () => "Update only when changed", () => "Whether the webhook should only be called if the url or the data changed.");

            this.WebhookUrl = globalSettingCollection.DefineSetting(nameof(this.WebhookUrl), string.Empty, () => "Webhook Url", () => "Defines the webhook url used to push data. Uses handlebars template of GameService.Gw2Mumble");
            this.WebhookStringContent = globalSettingCollection.DefineSetting(nameof(this.WebhookStringContent), string.Empty, () => "Webhook String Content", () => "Defines the webhook string content getting pushed. Uses handlebars template of GameService.Gw2Mumble");

            this.HandleSettingsEnabled();
        }

        private void UpdateMode_SettingChanged(object sender, ValueChangedEventArgs<UpdateMode> e)
        {
            this.HandleSettingsEnabled();
        }

        private void HandleSettingsEnabled()
        {
            this.UpdateInterval.SetDisabled(this.UpdateMode.Value != Models.UpdateMode.Interval);
        }

        public override void Unload()
        {
            base.Unload();
            this.UpdateMode.SettingChanged -= this.UpdateMode_SettingChanged;
        }
    }
}
