namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.EventTable.Controls;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.Services;
    using Estreya.BlishHUD.Shared.Controls;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Services;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Estreya.BlishHUD.Shared.Utils;
    using Flurl.Http;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Documents;

    public class DynamicEventsSettingsView : BaseSettingsView
    {
        private readonly DynamicEventService _dynamicEventService;
        private readonly ModuleSettings _moduleSettings;
        private readonly IFlurlClient _flurlClient;
        private StandardWindow _manageEventsWindow;

        private Texture2D _dynamicEventsInWorldImage;

        public DynamicEventsSettingsView(DynamicEventService dynamicEventService, ModuleSettings moduleSettings, IFlurlClient flurlClient, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, BitmapFont font = null) : base(apiManager, iconService, translationService, settingEventService, font)
        {
            this._dynamicEventService = dynamicEventService;
            this._moduleSettings = moduleSettings;
            this._flurlClient = flurlClient;
        }

        protected override void BuildView(FlowPanel parent)
        {
            this.RenderBoolSetting(parent, _moduleSettings.ShowDynamicEventsOnMap);
            this.RenderBoolSetting(parent, _moduleSettings.ShowDynamicEventInWorld, async (oldVal, newVal) =>
            {
                if (!newVal) return true;

                var confirmationDialog = new ConfirmDialog($"Activate \"{_moduleSettings.ShowDynamicEventInWorld.DisplayName}\"?",
                    $"You are in the process of activating \"{_moduleSettings.ShowDynamicEventInWorld.DisplayName}\".\n" +
                    $"This setting will add event boundaries inside your view (only when applicable events are on your map).\n\n" +
                    $"Do you want to continue?",
                    this.IconService);
                var result = await confirmationDialog.ShowDialog();

                return result == System.Windows.Forms.DialogResult.OK;
            });
            this.RenderBoolSetting(parent, _moduleSettings.ShowDynamicEventsInWorldOnlyWhenInside);
            this.RenderBoolSetting(parent, _moduleSettings.IgnoreZAxisOnDynamicEventsInWorld);
            this.RenderIntSetting(parent, _moduleSettings.DynamicEventsRenderDistance);

            this.RenderButton(parent, this.TranslationService.GetTranslation("dynamicEventsSettingsView-manageEvents-btn", "Manage Events"), () =>
            {
                this._manageEventsWindow ??= WindowUtil.CreateStandardWindow("Manage Events", this.GetType(), Guid.Parse("7dc52c82-67ae-4cfb-9fe3-a16a8b30892c"), this.IconService);

                if (_manageEventsWindow.CurrentView != null)
                {
                    var manageEventView = _manageEventsWindow.CurrentView as ManageDynamicEventsSettingsView;
                    manageEventView.EventChanged -= this.ManageView_EventChanged;
                }

                var view = new ManageDynamicEventsSettingsView(this._dynamicEventService, () => this._moduleSettings.DisabledDynamicEventIds.Value, this.APIManager, this.IconService, this.TranslationService);
                view.EventChanged += this.ManageView_EventChanged;

                _manageEventsWindow.Show(view);
            });

            if (this._dynamicEventsInWorldImage != null)
            {
                this.RenderEmptyLine(parent, 100);
                this.RenderLabel(parent, "Image of dynamic events inside the game world:");
                var image = new Image(this._dynamicEventsInWorldImage);
                image.Parent = parent;
            }
        }

        private void ManageView_EventChanged(object sender, ManageEventsView.EventChangedArgs e)
        {
            this._moduleSettings.DisabledDynamicEventIds.Value = e.NewService
                ? new List<string>(this._moduleSettings.DisabledDynamicEventIds.Value.Where(s => s != e.EventSettingKey))
                : new List<string>(this._moduleSettings.DisabledDynamicEventIds.Value) { e.EventSettingKey };
        }


        protected override async Task<bool> InternalLoad(IProgress<string> progress)
        {
            await this.TryLoadingDynamicEventsInWorldImage();
            return true;
        }

        private async Task TryLoadingDynamicEventsInWorldImage()
        {
            try
            {
                var stream = await this._flurlClient.Request("https://files.estreya.de/blish-hud/event-table/images/dynamic-events-in-world.png").GetStreamAsync();
                var bitmap = ImageUtil.ResizeImage(System.Drawing.Image.FromStream(stream), 500, 400);
                using MemoryStream memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                await Task.Run(() =>
                {
                    using var ctx = GameService.Graphics.LendGraphicsDeviceContext();
                    this._dynamicEventsInWorldImage = Texture2D.FromStream(ctx.GraphicsDevice, memoryStream);
                });
            }
            catch (Exception)
            {
            }
        }

        protected override void Unload()
        {
            base.Unload();

            if (this._manageEventsWindow?.CurrentView != null)
            {
                (this._manageEventsWindow.CurrentView as ManageDynamicEventsSettingsView).EventChanged -= this.ManageView_EventChanged;
            }

            this._manageEventsWindow?.Dispose();
            this._manageEventsWindow = null;

            this._dynamicEventsInWorldImage?.Dispose();
            this._dynamicEventsInWorldImage = null;
        }
    }
}
