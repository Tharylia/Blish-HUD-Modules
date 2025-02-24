namespace Estreya.BlishHUD.EventTable.UI.Views.Wizard;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Controls;
using Estreya.BlishHUD.EventTable.Models;
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
    private bool _useFillers;

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
            .CreatePart("You are probably already seeing your first area created and ready to use on your screen.", b => { b.SetFontSize(Blish_HUD.ContentService.FontSize.Size18); })
            .CreatePart("\n \n", b => { })
            .CreatePart("Please change the settings below to fit your needs regarding event areas.", b => { b.SetFontSize(Blish_HUD.ContentService.FontSize.Size18); })
            .Build();
        welcomeLbl.Top = (int)(parent.ContentRegion.Height * 0.1f);
        welcomeLbl.Parent = parent;

        var useRemindersLbl = this.RenderLabel(parent, "Use Reminders:").TitleLabel;
        useRemindersLbl.Parent = parent;
        useRemindersLbl.AutoSizeWidth = false;
        useRemindersLbl.Width = this.LABEL_WIDTH;
        useRemindersLbl.Top = welcomeLbl.Bottom + 100;
        useRemindersLbl.Left = 150;

        this._useReminders = this._moduleSettings.RemindersEnabled.Value;
        var useRemindersCheckbox = this.RenderCheckbox(parent, new Microsoft.Xna.Framework.Point(useRemindersLbl.Right + 20, useRemindersLbl.Top), this._useReminders, onChangeAction: val =>
        {
            this._useReminders = val;
        });
        useRemindersCheckbox.BasicTooltipText = "Check this option if you would like to be reminded before an event starts.";

        var showReminderButton = this.RenderButtonAsync(parent, "Show Test Reminder", this.ShowTestReminder);
        showReminderButton.Top = useRemindersLbl.Bottom + 5;
        showReminderButton.Left = useRemindersLbl.Left;

        var buttons = this.GetButtonPanel(parent);

        buttons.Top = parent.ContentRegion.Bottom - 20 - buttons.Height;
        buttons.Left = parent.ContentRegion.Width / 2 - buttons.Width / 2;
    }

    private async Task ShowTestReminder()
    {
        var title = "Test Event";
        var message = $"Test starts in {TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(21).Add(TimeSpan.FromSeconds(23))).Humanize(6, minUnit: this._moduleSettings.ReminderMinTimeUnit.Value)}!";
        var icon = this.IconService.GetIcon("textures/maintenance.png");

        if (this._moduleSettings.ReminderType.Value is Models.Reminders.ReminderType.Control or Models.Reminders.ReminderType.Both)
        {
            EventNotification.ShowAsControl(title, message, icon, this.IconService, this._moduleSettings);

            var audioTask = EventNotification.PlaySound(this._audioService);
            await audioTask;
        }

        if (this._moduleSettings.ReminderType.Value is Models.Reminders.ReminderType.Windows or Models.Reminders.ReminderType.Both)
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

        return Task.CompletedTask;
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);
}
