namespace Estreya.BlishHUD.EventTable.UI.Views.Wizard;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Controls;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.EventTable.Models.Reminders;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Services.Audio;
using Estreya.BlishHUD.Shared.UI.Views;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class WizardRemindersView : WizardView
{
    private bool _useReminders;
    private ReminderType _reminderType;

    private readonly ModuleSettings _moduleSettings;
    private readonly AudioService _audioService;

    protected override bool TestConfigurationsAvailable => true;

    public WizardRemindersView(ModuleSettings moduleSettings, AudioService audioService, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService)
    {
        this._moduleSettings = moduleSettings;
        this._audioService = audioService;
    }

    protected override void InternalBuild(Panel parent)
    {
        var welcomeLbl = new FormattedLabelBuilder().SetWidth(parent.ContentRegion.Width).Wrap().AutoSizeHeight().SetHorizontalAlignment(HorizontalAlignment.Center)
            .CreatePart("Event Reminders", b => { b.SetFontSize(Blish_HUD.ContentService.FontSize.Size24); })
            .CreatePart("\n \n", b => { })
            .CreatePart("The module is able to send you reminders for upcoming events.", b => { b.SetFontSize(Blish_HUD.ContentService.FontSize.Size18); })
            .CreatePart("\n \n", b => { })
            .CreatePart("Please change the settings below to fit your needs regarding event reminders.", b => { b.SetFontSize(Blish_HUD.ContentService.FontSize.Size18); })
            .Build();
        welcomeLbl.Top = (int)(parent.ContentRegion.Height * 0.1f);
        welcomeLbl.Parent = parent;

        var useRemindersLbl = this.RenderLabel(parent, "Use Reminders:").TitleLabel;
        useRemindersLbl.Parent = parent;
        useRemindersLbl.AutoSizeWidth = false;
        useRemindersLbl.Width = this.LABEL_WIDTH;
        useRemindersLbl.Top = welcomeLbl.Bottom + 100;
        useRemindersLbl.Left = 150;

        Dropdown<string> reminderTypeDropdown = null;

        this._useReminders = this._moduleSettings.RemindersEnabled.Value;
        var useRemindersCheckbox = this.RenderCheckbox(parent, new Microsoft.Xna.Framework.Point(useRemindersLbl.Right + 20, useRemindersLbl.Top), this._useReminders, onChangeAction: val =>
        {
            this._useReminders = val;
            if (reminderTypeDropdown != null)
            {
                reminderTypeDropdown.Enabled = this._useReminders;
            }
        });
        useRemindersCheckbox.BasicTooltipText = "Check this option if you would like to be reminded before an event starts.";

        var reminderTypeLbl = this.RenderLabel(parent, "Reminder Display Type:").TitleLabel;
        reminderTypeLbl.Parent = parent;
        reminderTypeLbl.AutoSizeWidth = false;
        reminderTypeLbl.Width = this.LABEL_WIDTH;
        reminderTypeLbl.Top = useRemindersLbl.Bottom + 5;
        reminderTypeLbl.Left = 150;

        this._reminderType = this._moduleSettings.ReminderType.Value;
        reminderTypeDropdown = this.RenderDropdown<ReminderType>(parent, new Microsoft.Xna.Framework.Point(reminderTypeLbl.Right + 20, reminderTypeLbl.Top), 150, this._reminderType, (ReminderType[])Enum.GetValues(typeof(ReminderType)), onChangeAction: val =>
        {
            this._reminderType = val;
        });
        reminderTypeDropdown.BasicTooltipText = "Select the display option for the reminders.\n\nWindows Notifications are not able to be displayed in parallel. If you have a lot of events starting it will take a long time to clear the queue.";

        var showReminderButton = this.RenderButtonAsync(parent, "Show Test Reminder", this.ShowTestReminder);
        showReminderButton.Top = reminderTypeLbl.Bottom + 5;
        showReminderButton.Left = reminderTypeLbl.Left;

        var buttons = this.GetButtonPanel(parent);

        buttons.Top = parent.ContentRegion.Bottom - 20 - buttons.Height;
        buttons.Left = parent.ContentRegion.Width / 2 - buttons.Width / 2;
    }

    private async Task ShowTestReminder()
    {
        var title = "Test Event";
        var message = $"Test starts in {TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(21).Add(TimeSpan.FromSeconds(23))).Humanize(6, minUnit: this._moduleSettings.ReminderMinTimeUnit.Value)}!";
        var icon = this.IconService.GetIcon("textures/maintenance.png");

        if (this._reminderType is Models.Reminders.ReminderType.Control or Models.Reminders.ReminderType.Both)
        {
            EventNotification.ShowAsControl(title, message, icon, this.IconService, this._moduleSettings);

            var audioTask = EventNotification.PlaySound(this._audioService);
            await audioTask;
        }

        if (this._reminderType is Models.Reminders.ReminderType.Windows or Models.Reminders.ReminderType.Both)
        {
#if !WINE
            await EventNotification.ShowAsWindowsNotification(title, message, icon);
#else
            Shared.Controls.ScreenNotification.ShowNotification("OS Notifications are not supported in WINE", Shared.Controls.ScreenNotification.NotificationType.Error, duration: 5);
#endif
        }
    }

    protected override Task ApplyConfigurations()
    {
        this._moduleSettings.ReminderType.Value = this._reminderType;
        this._moduleSettings.RemindersEnabled.Value = this._useReminders;

        return Task.CompletedTask;
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);
}
