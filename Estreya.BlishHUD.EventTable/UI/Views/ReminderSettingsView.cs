namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Controls;
using Controls;
using Microsoft.Xna.Framework;
using Models;
using MonoGame.Extended.BitmapFonts;
using Shared.Services;
using Shared.UI.Views;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Event = Models.Event;
using StandardWindow = Shared.Controls.StandardWindow;
using System.Windows.Forms;
using Estreya.BlishHUD.Shared.Threading.Events;
using Humanizer;
using System.Runtime.CompilerServices;
using Estreya.BlishHUD.Shared.Controls.Input;
using Windows.UI.Notifications;
using static Humanizer.On;
using Windows.Data.Xml.Dom;
using Estreya.BlishHUD.Shared.Extensions;
using System.IO;
using Blish_HUD.ArcDps.Models;
using Estreya.BlishHUD.Shared.Services.Audio;
using System.Threading;
using Estreya.BlishHUD.Shared.Threading;
using System.Data.Odbc;

public class ReminderSettingsView : BaseSettingsView
{
    private static readonly Event _globalChangeTempEvent = new Event();
    private readonly Func<List<EventCategory>> _getEvents;
    private readonly Func<List<string>> _getAreaNames;
    private readonly AccountService _accountService;
    private readonly AudioService _audioService;
    private readonly ModuleSettings _moduleSettings;
    private StandardWindow _manageEventsWindow;
    private StandardWindow _manageReminderTimesWindow;

    public event AsyncEventHandler SyncEnabledEventsToAreas;

    private SynchronizedCollection<EventNotification> _activeTestNotifications;

    static ReminderSettingsView()
    {
        _globalChangeTempEvent.Key = "test";
        _globalChangeTempEvent.Load(new EventCategory()
        {
            Key = "reminderSettingsView"
        }, () => throw new NotImplementedException());

        _globalChangeTempEvent.UpdateReminderTimes(new[]
        {
            TimeSpan.Zero
        });
    }

    public ReminderSettingsView(ModuleSettings moduleSettings, Func<List<EventCategory>> getEvents, Func<List<string>> getAreaNames, AccountService accountService, AudioService audioService, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService) : base(apiManager, iconService, translationService, settingEventService)
    {
        this._moduleSettings = moduleSettings;
        this._getEvents = getEvents;
        this._getAreaNames = getAreaNames;
        this._accountService = accountService;
        this._audioService = audioService;
        this.CONTROL_WIDTH = 500;
    }

    protected override void BuildView(FlowPanel parent)
    {
        var manageFlowPanel = new FlowPanel()
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleLeftToRight
        };

        this.RenderButton(manageFlowPanel, this.TranslationService.GetTranslation("reminderSettingsView-btn-manageReminders", "Manage Reminders"), () =>
        {
            if (this._manageEventsWindow == null)
            {
                this._manageEventsWindow = WindowUtil.CreateStandardWindow(this._moduleSettings, "Manage Events", this.GetType(), Guid.Parse("37e3f99c-f413-469c-b0f5-e2e6e31e4789"), this.IconService);
                this._manageEventsWindow.Width = ManageEventsView.BEST_WIDTH;
            }

            if (this._manageEventsWindow.CurrentView != null)
            {
                ManageEventsView manageEventView = this._manageEventsWindow.CurrentView as ManageEventsView;
                manageEventView.EventChanged -= this.ManageView_EventChanged;
            }

            ManageEventsView view = new ManageEventsView(this._getEvents(), new Dictionary<string, object>
            {
                {
                    "customActions", new List<ManageEventsView.CustomActionDefinition>
                    {
                        new ManageEventsView.CustomActionDefinition
                        {
                            Name = this.TranslationService.GetTranslation("reminderSettingsView-btn-changeTimes-title", "Change Times"),
                            Tooltip = this.TranslationService.GetTranslation("reminderSettingsView-btn-changeTimes-tooltip", "Click to change the times at which reminders happen."),
                            Icon = "1466345.png",
                            Action = this.ManageReminderTimes
                        },
                        new ManageEventsView.CustomActionDefinition
                        {
                            Name= this.TranslationService.GetTranslation("reminderSettingsView-btn-uploadEventSoundFile-title", "Upload Sound File"),
                            Tooltip = this.TranslationService.GetTranslation("reminderSettingsView-btn-uploadEventSoundFile-tooltip", "Click to upload a specific sound file for this event."),
                            Icon = "156764.png",
                            Action = this.UploadEventSoundFile
                        }
                    }
                }
            }, () => this._moduleSettings.ReminderDisabledForEvents.Value, this._moduleSettings, this._accountService, this.APIManager, this.IconService, this.TranslationService);
            view.EventChanged += this.ManageView_EventChanged;

            this._manageEventsWindow.Show(view);
        });

        this.RenderButtonAsync(manageFlowPanel, this.TranslationService.GetTranslation("reminderSettingsView-btn-uploadRemindersSoundFile", "Upload Sound File"), async () =>
        {
            var ofd = new AsyncFileDialog<OpenFileDialog>(new OpenFileDialog
            {
                Filter = "wav files (*.wav)|*.wav",
                Multiselect = false,
                CheckFileExists = true
            });

            var result = await ofd.ShowAsync();
            if (result != DialogResult.OK) return;

            await this._audioService.UploadFile(ofd.Dialog.FileName, EventNotification.GetSoundFileName(), EventNotification.GetAudioServiceBaseSubfolder());

        });

        var testRemindersFlowPanel = new FlowPanel()
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleLeftToRight
        };

        var addTestReminder = async (bool permanentControl, bool awaitAudio) =>
        {
            var title = "Test Event";
            var message = $"Test starts in {TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(21).Add(TimeSpan.FromSeconds(23))).Humanize(6, minUnit: this._moduleSettings.ReminderMinTimeUnit.Value)}!";
            var icon = this.IconService.GetIcon("textures/maintenance.png");

            if (this._moduleSettings.ReminderType.Value is Models.Reminders.ReminderType.Control or Models.Reminders.ReminderType.Both)
            {
                if (permanentControl)
                {
                    EventNotification reminder = EventNotification.ShowAsControlTest(title, message, icon, this.IconService, this._moduleSettings);
                    this._activeTestNotifications.Add(reminder);
                }
                else
                {
                    EventNotification.ShowAsControl(title, message, icon, this.IconService, this._moduleSettings);
                }

                var audioTask = EventNotification.PlaySound(this._audioService, _globalChangeTempEvent);
                if (awaitAudio)
                {
                    await audioTask;
                }
            }

            if (this._moduleSettings.ReminderType.Value is Models.Reminders.ReminderType.Windows or Models.Reminders.ReminderType.Both)
        {
#if !WINE
                await EventNotification.ShowAsWindowsNotification(title, message, icon);
#else
                Shared.Controls.ScreenNotification.ShowNotification("OS Notifications are not supported in WINE", Shared.Controls.ScreenNotification.NotificationType.Error, duration: 5);
#endif
            }
        };

        this.RenderButtonAsync(testRemindersFlowPanel, this.TranslationService.GetTranslation("reminderSettingsView-btn-addTestReminderPermanent", "Add Test Reminder"), async () =>
        {
            await addTestReminder(false, false);
        });

        this.RenderButtonAsync(testRemindersFlowPanel, this.TranslationService.GetTranslation("reminderSettingsView-btn-addTestReminderPermanent", "Add Test Reminder (Permanent)"), async () =>
        {
            await addTestReminder(true, false);
        });

        this.RenderButton(testRemindersFlowPanel, this.TranslationService.GetTranslation("reminderSettingsView-btn-clearTestReminder", "Clear Permanent Test Reminders"), () =>
        {
            foreach (var notification in this._activeTestNotifications)
            {
                notification.Dispose();
            }
        });

        var changeTimesFlowPanel = new FlowPanel()
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleLeftToRight
        };

        this.RenderButton(changeTimesFlowPanel, this.TranslationService.GetTranslation("reminderSettingsView-btn-changeAllTimes", "Change all Reminder Times"), () =>
        {
            this.ManageReminderTimes(_globalChangeTempEvent);
        });

        this.RenderButton(changeTimesFlowPanel, this.TranslationService.GetTranslation("reminderSettingsView-btn-resetAllTimes", "Reset all Reminder Times"), () =>
        {
            this.ManageReminderTimesView_SaveClicked(this, (_globalChangeTempEvent, new List<TimeSpan>()
            {
                TimeSpan.FromMinutes(10)
            }, false));
        });

        this.RenderButtonAsync(parent, this.TranslationService.GetTranslation("reminderSettingsView-btn-syncEnabledEventsToAreas", "Sync enabled Events to Areas"), async () =>
        {
            var confirmDialog = new ConfirmDialog(
                "Synchronizing",
                "You are in the process of synchronizing the enabled events of reminders to all event areas.\n\nThis will override all previously configured enabled/disabled settings in event areas.",
                this.IconService)
            {
                SelectedButtonIndex = 1 // Preselect cancel
            };

            var confirmResult = await confirmDialog.ShowDialog();
            if (confirmResult != DialogResult.OK) return;

            await (this.SyncEnabledEventsToAreas?.Invoke(this) ?? Task.FromException(new NotImplementedException()));

            Blish_HUD.Controls.ScreenNotification.ShowNotification("Synchronization complete!");
        });

        this.RenderEmptyLine(parent);

        this.RenderGeneralSettings(parent);

        this.RenderEmptyLine(parent);

        this.RenderLocationAndSizeSettings(parent);

        this.RenderEmptyLine(parent);

        this.RenderLayoutSettings(parent);

        this.RenderEmptyLine(parent);

        this.RenderVisibilitySettings(parent);

        this.RenderEmptyLine(parent);

        this.RenderTextAndColorSettings(parent);

        this.RenderEmptyLine(parent);

        this.RenderBehaviorSettings(parent);
    }

    private void RenderGeneralSettings(FlowPanel parent)
    {
        FlowPanel groupPanel = new FlowPanel
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - (int)(parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = false,
            Title = this.TranslationService.GetTranslation("reminderSettingsView-group-general", "General")
        };

        this.RenderBoolSetting(groupPanel, this._moduleSettings.RemindersEnabled);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderLocationAndSizeSettings(FlowPanel parent)
    {
        FlowPanel groupPanel = new FlowPanel
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - (int)(parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = true,
            Title = this.TranslationService.GetTranslation("reminderSettingsView-group-locationAndSize", "Location & Size")
        };

        this.RenderIntSetting(groupPanel, this._moduleSettings.ReminderPosition.X);
        this.RenderIntSetting(groupPanel, this._moduleSettings.ReminderPosition.Y);

        this.RenderEmptyLine(groupPanel);

        this.RenderIntSetting(groupPanel, this._moduleSettings.ReminderSize.X);
        this.RenderIntSetting(groupPanel, this._moduleSettings.ReminderSize.Y);
        this.RenderIntSetting(groupPanel, this._moduleSettings.ReminderSize.Icon);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderLayoutSettings(FlowPanel parent)
    {
        FlowPanel groupPanel = new FlowPanel
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - (int)(parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = true,
            Title = this.TranslationService.GetTranslation("reminderSettingsView-group-layout", "Layout")
        };

        this.RenderEnumSetting(groupPanel, this._moduleSettings.ReminderStackDirection);
        this.RenderEnumSetting(groupPanel, this._moduleSettings.ReminderOverflowStackDirection);

        this.RenderEmptyLine(groupPanel);

        this.RenderEnumSetting(groupPanel, this._moduleSettings.ReminderMinTimeUnit);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderVisibilitySettings(FlowPanel parent)
    {
        FlowPanel groupPanel = new FlowPanel
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - (int)(parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = true,
            Title = this.TranslationService.GetTranslation("reminderSettingsView-group-visibility", "Visibility")
        };

        this.RenderBoolSetting(groupPanel, this._moduleSettings.HideRemindersOnMissingMumbleTicks);
        this.RenderBoolSetting(groupPanel, this._moduleSettings.HideRemindersOnOpenMap);
        this.RenderBoolSetting(groupPanel, this._moduleSettings.HideRemindersInCombat);
        this.RenderBoolSetting(groupPanel, this._moduleSettings.HideRemindersInPvE_OpenWorld);
        this.RenderBoolSetting(groupPanel, this._moduleSettings.HideRemindersInPvE_Competetive);
        this.RenderBoolSetting(groupPanel, this._moduleSettings.HideRemindersInWvW);
        this.RenderBoolSetting(groupPanel, this._moduleSettings.HideRemindersInPvP);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderTextAndColorSettings(FlowPanel parent)
    {
        FlowPanel groupPanel = new FlowPanel
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - (int)(parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = true,
            Title = this.TranslationService.GetTranslation("reminderSettingsView-group-textAndColor", "Text & Color")
        };

        this.RenderEnumSetting(groupPanel, this._moduleSettings.ReminderFonts.TitleSize);
        this.RenderEnumSetting(groupPanel, this._moduleSettings.ReminderFonts.MessageSize);

        this.RenderEmptyLine(groupPanel);

        this.RenderColorSetting(groupPanel, this._moduleSettings.ReminderColors.Background);
        this.RenderColorSetting(groupPanel, this._moduleSettings.ReminderColors.TitleText);
        this.RenderColorSetting(groupPanel, this._moduleSettings.ReminderColors.MessageText);
        this.RenderFloatSetting(groupPanel, this._moduleSettings.ReminderBackgroundOpacity);
        this.RenderFloatSetting(groupPanel, this._moduleSettings.ReminderTitleOpacity);
        this.RenderFloatSetting(groupPanel, this._moduleSettings.ReminderMessageOpacity);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderBehaviorSettings(FlowPanel parent)
    {
        FlowPanel groupPanel = new FlowPanel
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - (int)(parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = true,
            Title = this.TranslationService.GetTranslation("reminderSettingsView-group-behaviors", "Behaviors")
        };

        this.RenderFloatSetting(groupPanel, this._moduleSettings.ReminderDuration);

        this.RenderEmptyLine(groupPanel);

        this.RenderEnumSetting(groupPanel, this._moduleSettings.ReminderType);

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, this._moduleSettings.DisableRemindersWhenEventFinished);

        this.RenderEmptyLine(groupPanel);

        this.RenderDisableRemindersWhenEventFinishedArea(groupPanel);

        this.RenderEmptyLine(groupPanel);

        this.RenderEnumSetting(groupPanel, this._moduleSettings.ReminderLeftClickAction);
        this.RenderBoolSetting(groupPanel, this._moduleSettings.AcceptWaypointPrompt);
        this.RenderEnumSetting(groupPanel, this._moduleSettings.ReminderRightClickAction);
        this.RenderEnumSetting(groupPanel, this._moduleSettings.ReminderWaypointSendingChannel);
        this.RenderEnumSetting(groupPanel, this._moduleSettings.ReminderWaypointSendingGuild);
        this.RenderEnumSetting(groupPanel, this._moduleSettings.ReminderEventChatFormat);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderDisableRemindersWhenEventFinishedArea(FlowPanel parent)
    {
        var panel = new Blish_HUD.Controls.Panel()
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
        };

        var label = this.RenderLabel(panel, this._moduleSettings.DisableRemindersWhenEventFinishedArea.DisplayName).TitleLabel;
        var values = new List<string>() { ModuleSettings.ANY_AREA_NAME };
        values.AddRange(this._getAreaNames());
        var dropdown = this.RenderDropdown(panel, this.CONTROL_LOCATION, this.CONTROL_WIDTH, values.ToArray(), this._moduleSettings.DisableRemindersWhenEventFinishedArea.Value, newVal =>
        {
            this._moduleSettings.DisableRemindersWhenEventFinishedArea.Value = newVal;
        });
        dropdown.BasicTooltipText = this._moduleSettings.DisableRemindersWhenEventFinishedArea.Description;
    }

    private void ManageView_EventChanged(object sender, ManageEventsView.EventChangedArgs e)
    {
        this._moduleSettings.ReminderDisabledForEvents.Value = e.NewState
            ? new List<string>(this._moduleSettings.ReminderDisabledForEvents.Value.Where(s => s != e.EventSettingKey))
            : new List<string>(this._moduleSettings.ReminderDisabledForEvents.Value) { e.EventSettingKey };
    }

    private async Task UploadEventSoundFile(Event ev)
    {
        try
        {
            var ofd = new AsyncFileDialog<OpenFileDialog>(new OpenFileDialog
            {
                Filter = "wav files (*.wav)|*.wav",
                Multiselect = false,
                CheckFileExists = true
            });

            var result = await ofd.ShowAsync();
            if (result != DialogResult.OK) return;

            await this._audioService.UploadFile(ofd.Dialog.FileName, ev.SettingKey, EventNotification.GetAudioServiceEventsSubfolder());
        }
        catch (Exception ex)
        {
            this.ShowError(ex.Message);
        }
    }

    private Task ManageReminderTimes(Event ev)
    {
        this._manageReminderTimesWindow ??= WindowUtil.CreateStandardWindow(this._moduleSettings, "Manage Reminder Times", this.GetType(), Guid.Parse("930702ac-bf87-416c-b5ba-cdf9e0266bf7"), this.IconService, this.IconService.GetIcon("1466345.png"));
        this._manageReminderTimesWindow.Size = new Point(450, this._manageReminderTimesWindow.Height);

        if (this._manageReminderTimesWindow?.CurrentView is ManageReminderTimesView mrtv)
        {
            // Unload events
            mrtv.CancelClicked -= this.ManageReminderTimesView_CancelClicked;
            mrtv.SaveClicked -= this.ManageReminderTimesView_SaveClicked;
        }

        ManageReminderTimesView view = new ManageReminderTimesView(ev, ev == _globalChangeTempEvent, this.APIManager, this.IconService, this.TranslationService);
        view.CancelClicked += this.ManageReminderTimesView_CancelClicked;
        view.SaveClicked += this.ManageReminderTimesView_SaveClicked;

        //this._manageReminderTimesWindow.Subtitle = ev.Name;
        this._manageReminderTimesWindow.Show(view);

        return Task.CompletedTask;
    }

    private void ManageReminderTimesView_SaveClicked(object sender, (Event Event, List<TimeSpan> ReminderTimes, bool KeepCustomized) e)
    {
        if (e.Event == _globalChangeTempEvent)
        {
            IEnumerable<Event> allEvents = this._getEvents().SelectMany(ec => ec.Events).Where(ev => !ev.Filler);
            foreach (Event ev in allEvents)
            {
                if (this._moduleSettings.ReminderTimesOverride.Value.ContainsKey(ev.SettingKey) && e.KeepCustomized) continue;

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
        this._activeTestNotifications = new SynchronizedCollection<EventNotification>();
        return Task.FromResult(true);
    }

    protected override void Unload()
    {
        base.Unload();

        if (this._activeTestNotifications != null)
        {
            foreach (var notification in this._activeTestNotifications)
            {
                notification.Dispose();
            }

            this._activeTestNotifications.Clear();
            this._activeTestNotifications = null;
        }

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