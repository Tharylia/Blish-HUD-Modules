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

    public class ReminderSettingsView : BaseSettingsView
    {
        private readonly ModuleSettings _moduleSettings;
        private readonly Func<List<EventCategory>> _getEvents;
        private StandardWindow _manageEventsWindow;

        public ReminderSettingsView(ModuleSettings moduleSettings, Func<List<EventCategory>> getEvents, Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, SettingEventState settingEventState, BitmapFont font = null) : base(apiManager, iconState, translationState, settingEventState, font)
        {
            this._moduleSettings = moduleSettings;
            this._getEvents = getEvents;
        }

        protected override void BuildView(FlowPanel parent)
        {
            this.RenderBoolSetting(parent, _moduleSettings.RemindersEnabled);

            this.RenderIntSetting(parent, _moduleSettings.ReminderPosition.X);
            this.RenderIntSetting(parent, _moduleSettings.ReminderPosition.Y);
            this.RenderFloatSetting(parent, _moduleSettings.ReminderDuration);
            this.RenderFloatSetting(parent, _moduleSettings.ReminderOpacity);

            this.RenderEmptyLine(parent);

            this.RenderButton(parent, "Manage Reminders", () =>
            {
                if (this._manageEventsWindow == null)
                {
                    Texture2D windowBackground = this.IconState.GetIcon(@"textures\setting_window_background.png");

                    Rectangle settingsWindowSize = new Rectangle(35, 26, 1100, 714);
                    int contentRegionPaddingY = settingsWindowSize.Y - 15;
                    int contentRegionPaddingX = settingsWindowSize.X;
                    Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 6, settingsWindowSize.Height - contentRegionPaddingY);

                    this._manageEventsWindow = new StandardWindow(windowBackground, settingsWindowSize, contentRegion)
                    {
                        Parent = GameService.Graphics.SpriteScreen,
                        Title = "Manage Events",
                        SavesPosition = true,
                        Id = $"{this.GetType().Name}_7dc52c82-67ae-4cfb-9fe3-a16a8b30892c"
                    };
                }

                if (_manageEventsWindow.CurrentView != null)
                {
                    var manageEventView = _manageEventsWindow.CurrentView as ManageEventsView;
                    manageEventView.EventChanged -= this.ManageView_EventChanged;
                }

                var view = new ManageEventsView(this._getEvents(), null, () => _moduleSettings.ReminderDisabledForEvents.Value, this.APIManager, this.IconState, this.TranslationState);
                view.EventChanged += this.ManageView_EventChanged;

                _manageEventsWindow.Show(view);
            });

            this.RenderButton(parent, "Test Reminder", () =>
            {
                var reminder = new EventNotification(new Models.Event()
                {
                    Name = "Test Event",
                    Icon = "textures/maintenance.png",
                }, "Test description!", _moduleSettings.ReminderPosition.X.Value, _moduleSettings.ReminderPosition.Y.Value, this.IconState)
                {
                    BackgroundOpacity = _moduleSettings.ReminderOpacity.Value
                };

                reminder.Show(TimeSpan.FromSeconds(_moduleSettings.ReminderDuration.Value));
            });

            //var lastChild = parent.Children.Last();

            //var managePanel = new FlowPanel()
            //{
            //    Parent = parent,
            //    ShowBorder = true,
            //    CanScroll = true,
            //    Width = parent.ContentRegion.Width - 50,
            //    HeightSizingMode = SizingMode.Fill,
            //    FlowDirection = ControlFlowDirection.LeftToRight
            //};

            //foreach (var ec in _getEvents())
            //{
            //    foreach (var ev in ec.Events)
            //    {
            //        var eventDetailButton = new EventDetailsButton()
            //        {
            //            Parent = managePanel,
            //            Event = ev,
            //            Icon = this.IconState.GetIcon(ev.Icon),
            //            Text = ev.Name,
            //        };

            //        GlowButton toggleButton = new GlowButton()
            //        {
            //            Parent = eventDetailButton,
            //            Checked = !_moduleSettings.ReminderDisabledForEvents.Value.Contains(ev.SettingKey),
            //        };

            //        this.UpdateToggleButton(toggleButton);

            //        toggleButton.CheckedChanged += (s, eventArgs) =>
            //        {
            //            _moduleSettings.ReminderDisabledForEvents.Value = eventArgs.Checked
            //                ? new List<string>(_moduleSettings.ReminderDisabledForEvents.Value) { ev.SettingKey }
            //                : new List<string>(_moduleSettings.ReminderDisabledForEvents.Value.Where(s => s != ev.SettingKey));

            //            this.UpdateToggleButton(toggleButton);
            //        };

            //        toggleButton.Click += (s, eventArgs) =>
            //        {
            //            toggleButton.Checked = !toggleButton.Checked;
            //        };
            //    }
            //}

            //managePanel.Height = managePanel.Height--;
        }

        private void ManageView_EventChanged(object sender, EventChangedArgs e)
        {
            this._moduleSettings.ReminderDisabledForEvents.Value = e.NewState
                ? new List<string>(this._moduleSettings.ReminderDisabledForEvents.Value.Where(s => s != e.EventSettingKey))
                : new List<string>(this._moduleSettings.ReminderDisabledForEvents.Value) { e.EventSettingKey };
        }

        private void UpdateToggleButton(GlowButton button)
        {
            GameService.Graphics.QueueMainThreadRender((graphicDevice) =>
            {
                button.Icon = button.Checked
                    ? this.IconState.GetIcon("784259.png")
                    : this.IconState.GetIcon("784261.png");
            });
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }

        protected override void Unload()
        {
            base.Unload();

            if (this._manageEventsWindow?.CurrentView != null)
            {
                (this._manageEventsWindow.CurrentView as ManageEventsView).EventChanged -= this.ManageView_EventChanged;
            }

            this._manageEventsWindow?.Dispose();
            this._manageEventsWindow = null;
        }
    }
}
