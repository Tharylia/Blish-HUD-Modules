namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Models;
using Flurl.Http;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using Services;
using Shared.Controls;
using Shared.Services;
using Shared.UI.Views;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Image = Blish_HUD.Controls.Image;
using StandardWindow = Shared.Controls.StandardWindow;

public class DynamicEventsSettingsView : BaseSettingsView
{
    private readonly DynamicEventService _dynamicEventService;
    private readonly IFlurlClient _flurlClient;
    private readonly ModuleSettings _moduleSettings;

    private Texture2D _dynamicEventsInWorldImage;
    private StandardWindow _manageEventsWindow;
    private StandardWindow _editEventWindow; 

    public DynamicEventsSettingsView(DynamicEventService dynamicEventService, ModuleSettings moduleSettings, IFlurlClient flurlClient, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService) : base(apiManager, iconService, translationService, settingEventService)
    {
        this._dynamicEventService = dynamicEventService;
        this._moduleSettings = moduleSettings;
        this._flurlClient = flurlClient;
    }

    protected override void BuildView(FlowPanel parent)
    {
        this.RenderBoolSetting(parent, this._moduleSettings.ShowDynamicEventsOnMap);
        this.RenderBoolSetting(parent, this._moduleSettings.ShowDynamicEventInWorld, async (oldVal, newVal) =>
        {
            if (!newVal)
            {
                return true;
            }

            ConfirmDialog confirmationDialog = new ConfirmDialog($"Activate \"{this._moduleSettings.ShowDynamicEventInWorld.DisplayName}\"?",
                $"You are in the process of activating \"{this._moduleSettings.ShowDynamicEventInWorld.DisplayName}\".\n" +
                $"This setting will add event boundaries inside your view (only when applicable events are on your map).\n\n" +
                $"Do you want to continue?",
                this.IconService);
            DialogResult result = await confirmationDialog.ShowDialog();

            return result == DialogResult.OK;
        });
        this.RenderBoolSetting(parent, this._moduleSettings.ShowDynamicEventsInWorldOnlyWhenInside);
        this.RenderBoolSetting(parent, this._moduleSettings.IgnoreZAxisOnDynamicEventsInWorld);
        this.RenderIntSetting(parent, this._moduleSettings.DynamicEventsRenderDistance);

        this.RenderButton(parent, this.TranslationService.GetTranslation("dynamicEventsSettingsView-btn-manageEvents", "Manage Events"), () =>
        {
            this._manageEventsWindow ??= WindowUtil.CreateStandardWindow(this._moduleSettings, "Manage Events", this.GetType(), Guid.Parse("7dc52c82-67ae-4cfb-9fe3-a16a8b30892c"), this.IconService);

            if (this._manageEventsWindow.CurrentView != null)
            {
                ManageDynamicEventsSettingsView manageEventView = this._manageEventsWindow.CurrentView as ManageDynamicEventsSettingsView;
                manageEventView.EventChanged -= this.ManageView_EventChanged;
            }

            ManageDynamicEventsSettingsView view = new ManageDynamicEventsSettingsView(this._dynamicEventService, () => this._moduleSettings.DisabledDynamicEventIds.Value, this._moduleSettings, this.APIManager, this.IconService, this.TranslationService);
            view.EventChanged += this.ManageView_EventChanged;

            this._manageEventsWindow.Show(view);
        });

        this.RenderButton(parent, this.TranslationService.GetTranslation("dynamicEventsSettingsView-btn-addEvent", "Add Event"), () =>
        {
            this._editEventWindow ??= WindowUtil.CreateStandardWindow(this._moduleSettings, "Add Dynamic Event", this.GetType(), Guid.Parse("d174b7f7-adf6-4e90-8928-4e581ffa1d71"), this.IconService);

            if (this._editEventWindow.CurrentView != null)
            {
                EditDynamicEventView editEventView = this._editEventWindow.CurrentView as EditDynamicEventView;
                editEventView.SaveClicked -= this.EditEventView_SaveClicked;
                editEventView.CloseRequested -= this.EditEventView_CloseRequested;
            }

            EditDynamicEventView view = new EditDynamicEventView( null, this.APIManager, this.IconService, this.TranslationService);
            view.SaveClicked += this.EditEventView_SaveClicked;
            view.CloseRequested += this.EditEventView_CloseRequested;

            this._editEventWindow.Show(view);
        });

        if (this._dynamicEventsInWorldImage != null)
        {
            this.RenderEmptyLine(parent, 100);
            this.RenderLabel(parent, this.TranslationService.GetTranslation("dynamicEventsSettingsView-lbl-imageOfDynamicEventsInWorld", "Image of dynamic events inside the game world:"));
            Image image = new Image(this._dynamicEventsInWorldImage) { Parent = parent };
        }
    }

    private void EditEventView_CloseRequested(object sender, EventArgs e)
    {
        this._editEventWindow.Hide();
    }

    private async Task EditEventView_SaveClicked(object sender, DynamicEvent e)
    {
        await this._dynamicEventService.AddCustomEvent(dynamicEvent: e);

        await this._dynamicEventService.NotifyCustomEventsUpdated();
    }

    private void ManageView_EventChanged(object sender, ManageEventsView.EventChangedArgs e)
    {
        this._moduleSettings.DisabledDynamicEventIds.Value = e.NewState
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
            using Stream stream = await this._flurlClient.Request("https://files.estreya.de/blish-hud/event-table/images/dynamic-events-in-world.png").GetStreamAsync();
            using Bitmap bitmap = ImageUtil.ResizeImage(System.Drawing.Image.FromStream(stream), 500, 400);
            using MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            await Task.Run(() =>
            {
                using GraphicsDeviceContext ctx = GameService.Graphics.LendGraphicsDeviceContext();
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

        this._editEventWindow?.Dispose();
        this._editEventWindow = null;

        this._dynamicEventsInWorldImage?.Dispose();
        this._dynamicEventsInWorldImage = null;
    }
}