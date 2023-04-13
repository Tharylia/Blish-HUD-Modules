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
    using Estreya.BlishHUD.Shared.Utils;
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
        private StandardWindow _manageReminderTimesWindow;
        private static Models.Event _globalChangeTempEvent = new Models.Event();

        static ReminderSettingsView()
        {
            _globalChangeTempEvent.UpdateReminderTimes(new TimeSpan[] { TimeSpan.Zero });
        }

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

            this.RenderButton(parent, this.TranslationState.GetTranslation("reminderSettingsView-manageReminders-btn", "Manage Reminders"), () =>
            {
                this._manageEventsWindow ??= WindowUtil.CreateStandardWindow("Manage Events", this.GetType(), Guid.Parse("37e3f99c-f413-469c-b0f5-e2e6e31e4789"), this.IconState);

                if (_manageEventsWindow.CurrentView != null)
                {
                    var manageEventView = _manageEventsWindow.CurrentView as ManageEventsView;
                    manageEventView.EventChanged -= this.ManageView_EventChanged;
                }

                var view = new ManageEventsView(this._getEvents(), new Dictionary<string, object>()
                {
                    { "customActions", new List<ManageEventsView.CustomActionDefinition>()
                        {
                            new ManageEventsView.CustomActionDefinition()
                            {
                                Name = "Change Times",
                                Tooltip = "Click to change the times at which reminders happen.",
                                Icon = "1466345.png",
                                Action = (ev) => {
                                    this.ManageReminderTimes(ev);
                                }
                            }
                        }
                    }
                }, () => _moduleSettings.ReminderDisabledForEvents.Value, this._moduleSettings, this.APIManager, this.IconState, this.TranslationState);
                view.EventChanged += this.ManageView_EventChanged;

                _manageEventsWindow.Show(view);
            });

            this.RenderButton(parent, this.TranslationState.GetTranslation("reminderSettingsView-testReminder-btn", "Test Reminder"), () =>
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

            this.RenderButton(parent, "Change all Reminder Times", () =>
            {
                this.ManageReminderTimes(_globalChangeTempEvent);
            });

            this.RenderEmptyLine(parent);

            this.RenderBoolSetting(parent, _moduleSettings.HideRemindersOnMissingMumbleTicks);
            this.RenderBoolSetting(parent, _moduleSettings.HideRemindersOnOpenMap);
            this.RenderBoolSetting(parent, _moduleSettings.HideRemindersInCombat);
            this.RenderBoolSetting(parent, _moduleSettings.HideRemindersInPvE_OpenWorld);
            this.RenderBoolSetting(parent, _moduleSettings.HideRemindersInPvE_Competetive);
            this.RenderBoolSetting(parent, _moduleSettings.HideRemindersInWvW);
            this.RenderBoolSetting(parent, _moduleSettings.HideRemindersInPvP);
        }

        private void ManageView_EventChanged(object sender, ManageEventsView.EventChangedArgs e)
        {
            this._moduleSettings.ReminderDisabledForEvents.Value = e.NewState
                ? new List<string>(this._moduleSettings.ReminderDisabledForEvents.Value.Where(s => s != e.EventSettingKey))
                : new List<string>(this._moduleSettings.ReminderDisabledForEvents.Value) { e.EventSettingKey };
        }

        private void ManageReminderTimes(Models.Event ev)
        {
            this._manageReminderTimesWindow ??= WindowUtil.CreateStandardWindow("Manage Reminder Times", this.GetType(), Guid.Parse("930702ac-bf87-416c-b5ba-cdf9e0266bf7"), this.IconState, this.IconState.GetIcon("1466345.png"));
            this._manageReminderTimesWindow.Size = new Point(450, this._manageReminderTimesWindow.Height);

            if (this._manageReminderTimesWindow?.CurrentView is ManageReminderTimesView mrtv)
            {
                // Unload events
                mrtv.CancelClicked -= this.ManageReminderTimesView_CancelClicked;
                mrtv.SaveClicked -= this.ManageReminderTimesView_SaveClicked;
            }

            var view = new ManageReminderTimesView(ev, this.APIManager, this.IconState, this.TranslationState);
            view.CancelClicked += this.ManageReminderTimesView_CancelClicked;
            view.SaveClicked += this.ManageReminderTimesView_SaveClicked;

            //this._manageReminderTimesWindow.Subtitle = ev.Name;
            this._manageReminderTimesWindow.Show(view);
        }

        private void ManageReminderTimesView_SaveClicked(object sender, (Models.Event Event, List<TimeSpan> ReminderTimes) e)
        {
            if (e.Event == _globalChangeTempEvent)
            {
                var allEvents = this._getEvents().SelectMany(ec => ec.Events).Where(ev => !ev.Filler);
                foreach (var ev in allEvents)
                {
                    this._moduleSettings.ReminderTimesOverride.Value[ev.SettingKey] = e.ReminderTimes;
                    ev.UpdateReminderTimes(e.ReminderTimes.ToArray());
                }

                this._moduleSettings.ReminderTimesOverride.Value = new Dictionary<string, List<TimeSpan>>(this._moduleSettings.ReminderTimesOverride.Value);
                _globalChangeTempEvent.UpdateReminderTimes(e.ReminderTimes.ToArray());
            }
            else
            {
                this._moduleSettings.ReminderTimesOverride.Value[e.Event.SettingKey] = e.ReminderTimes;
                this._moduleSettings.ReminderTimesOverride.Value = new Dictionary<string, List<TimeSpan>>(this._moduleSettings.ReminderTimesOverride.Value);
                e.Event.UpdateReminderTimes(e.ReminderTimes.ToArray());
            }

            this._manageReminderTimesWindow?.Hide();
        }

        private void ManageReminderTimesView_CancelClicked(object sender, EventArgs e)
        {
            this._manageReminderTimesWindow?.Hide();
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

            if (this._manageReminderTimesWindow?.CurrentView is ManageReminderTimesView mrtv)
            {
                // Unload events
                mrtv.CancelClicked -= this.ManageReminderTimesView_CancelClicked;
                mrtv.SaveClicked -= this.ManageReminderTimesView_SaveClicked;
            }

            this._manageReminderTimesWindow?.Dispose();
            this._manageReminderTimesWindow = null;
        }
    }
}
