namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

public class EventTimersSettingsView : BaseSettingsView
{
    private readonly ModuleSettings _moduleSettings;
    private readonly Func<Task<List<EventCategory>>> _getAllEvents;
    private readonly AccountService _accountService;
    private Shared.Controls.StandardWindow _manageEventsWindow;

    public EventTimersSettingsView(ModuleSettings moduleSettings, Func<Task<List<Models.EventCategory>>> getAllEvents, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, AccountService accountService) : base(apiManager, iconService, translationService, settingEventService)
    {
        this._moduleSettings = moduleSettings;
        this._getAllEvents = getAllEvents;
        this._accountService = accountService;
    }

    protected override void BuildView(FlowPanel parent)
    {
        this.RenderButtonAsync(parent, this.TranslationService.GetTranslation("eventTimersSettingsView-btn-manageEvents", "Manage Events"), async () =>
        {
            if (this._manageEventsWindow == null)
            {
                this._manageEventsWindow = WindowUtil.CreateStandardWindow(this._moduleSettings, "Manage Events", this.GetType(), Guid.Parse("328bf66c-364e-40ae-9ffc-140e002afb32"), this.IconService);
                this._manageEventsWindow.Width = ManageEventsView.BEST_WIDTH;
            }

            if (this._manageEventsWindow.CurrentView != null)
            {
                ManageEventsView manageEventView = this._manageEventsWindow.CurrentView as ManageEventsView;
                manageEventView.EventChanged -= this.ManageView_EventChanged;
            }

            var allEvents = await this._getAllEvents();
            ManageEventsView view = new ManageEventsView(allEvents, null, () => this._moduleSettings.DisabledEventTimerSettingKeys.Value, this._moduleSettings, this._accountService, this.APIManager, this.IconService, this.TranslationService);
            view.EventChanged += this.ManageView_EventChanged;

            this._manageEventsWindow.Show(view);
        });

        this.RenderEmptyLine(parent);

        this.RenderBoolSetting(parent, this._moduleSettings.ShowEventTimersOnMap);
        this.RenderBoolSetting(parent, this._moduleSettings.ShowEventTimersInWorld);

        this.RenderEmptyLine(parent);

        this.RenderIntSetting(parent, this._moduleSettings.EventTimersRenderDistance);
    }

    private void ManageView_EventChanged(object sender, ManageEventsView.EventChangedArgs e)
    {
        this._moduleSettings.DisabledEventTimerSettingKeys.Value = e.NewState
            ? new List<string>(this._moduleSettings.DisabledEventTimerSettingKeys.Value.Where(x => x != e.EventSettingKey))
            : new List<string>(this._moduleSettings.DisabledEventTimerSettingKeys.Value) { e.EventSettingKey };
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);

    protected override void Unload()
    {
        base.Unload();

        this._manageEventsWindow?.Dispose();
        this._manageEventsWindow = null;
    }
}
