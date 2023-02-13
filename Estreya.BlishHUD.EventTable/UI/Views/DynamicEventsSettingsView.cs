namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.EventTable.Controls;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Documents;

    public class DynamicEventsSettingsView : BaseSettingsView
    {
        private readonly ModuleSettings _moduleSettings;
        //private StandardWindow _manageEventsWindow;

        public DynamicEventsSettingsView(ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, SettingEventState settingEventState, BitmapFont font = null) : base(apiManager, iconState, translationState, settingEventState, font)
        {
            this._moduleSettings = moduleSettings;
        }

        protected override void BuildView(FlowPanel parent)
        {
            this.RenderBoolSetting(parent, _moduleSettings.ShowDynamicEventsOnMap);

            //this.RenderIntSetting(parent, _moduleSettings.ReminderPosition.X);
            //this.RenderIntSetting(parent, _moduleSettings.ReminderPosition.Y);
            //this.RenderFloatSetting(parent, _moduleSettings.ReminderDuration);
            //this.RenderFloatSetting(parent, _moduleSettings.ReminderOpacity);

            //this.RenderEmptyLine(parent);

            //this.RenderButton(parent, this.TranslationState.GetTranslation("reminderSettingsView-manageReminders-btn","Manage Reminders"), () =>
            //{
            //    if (this._manageEventsWindow == null)
            //    {
            //        Texture2D windowBackground = this.IconState.GetIcon(@"textures\setting_window_background.png");

            //        Rectangle settingsWindowSize = new Rectangle(35, 26, 1100, 714);
            //        int contentRegionPaddingY = settingsWindowSize.Y - 15;
            //        int contentRegionPaddingX = settingsWindowSize.X;
            //        Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 6, settingsWindowSize.Height - contentRegionPaddingY);

            //        this._manageEventsWindow = new StandardWindow(windowBackground, settingsWindowSize, contentRegion)
            //        {
            //            Parent = GameService.Graphics.SpriteScreen,
            //            Title = "Manage Events",
            //            SavesPosition = true,
            //            Id = $"{this.GetType().Name}_7dc52c82-67ae-4cfb-9fe3-a16a8b30892c"
            //        };
            //    }

            //    if (_manageEventsWindow.CurrentView != null)
            //    {
            //        var manageEventView = _manageEventsWindow.CurrentView as ManageEventsView;
            //        manageEventView.EventChanged -= this.ManageView_EventChanged;
            //    }

            //    var view = new ManageEventsView(this._getEvents(), null, () => _moduleSettings.ReminderDisabledForEvents.Value, this.APIManager, this.IconState, this.TranslationState);
            //    view.EventChanged += this.ManageView_EventChanged;

            //    _manageEventsWindow.Show(view);
            //});

            //this.RenderButton(parent, this.TranslationState.GetTranslation("reminderSettingsView-testReminder-btn","Test Reminder"), () =>
            //{
            //    var reminder = new EventNotification(new Models.Event()
            //    {
            //        Name = "Test Event",
            //        Icon = "textures/maintenance.png",
            //    }, "Test description!", _moduleSettings.ReminderPosition.X.Value, _moduleSettings.ReminderPosition.Y.Value, this.IconState)
            //    {
            //        BackgroundOpacity = _moduleSettings.ReminderOpacity.Value
            //    };

            //    reminder.Show(TimeSpan.FromSeconds(_moduleSettings.ReminderDuration.Value));
            //});
        }

        private void ManageView_EventChanged(object sender, EventChangedArgs e)
        {
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }

        protected override void Unload()
        {
            base.Unload();

            //if (this._manageEventsWindow?.CurrentView != null)
            //{
            //    (this._manageEventsWindow.CurrentView as ManageEventsView).EventChanged -= this.ManageView_EventChanged;
            //}

            //this._manageEventsWindow?.Dispose();
            //this._manageEventsWindow = null;
        }
    }
}
