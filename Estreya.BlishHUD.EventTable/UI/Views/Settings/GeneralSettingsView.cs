namespace Estreya.BlishHUD.EventTable.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    public class GeneralSettingsView : BaseSettingsView
    {
        public GeneralSettingsView(ModuleSettings settings) : base(settings)
        {
        }

        protected override void BuildView(Panel parent)
        {
#if DEBUG
            this.RenderButton(parent, "Open Website", () => Process.Start(EventTableModule.WEBSITE_MODULE_URL));
            this.RenderEmptyLine(parent);
#endif

            this.RenderSetting(parent, this.ModuleSettings.GlobalEnabled);
            this.RenderSetting(parent, this.ModuleSettings.GlobalEnabledHotkey);
#if DEBUG
            this.RenderSetting(parent, this.ModuleSettings.DebugEnabled);
#endif
            this.RenderSetting(parent, this.ModuleSettings.RegisterCornerIcon);
            this.RenderEmptyLine(parent);
            this.RenderSetting(parent, this.ModuleSettings.HideOnOpenMap);
            this.RenderSetting(parent, this.ModuleSettings.HideOnMissingMumbleTicks);
            this.RenderSetting(parent, this.ModuleSettings.HideInCombat);
            this.RenderSetting(parent, this.ModuleSettings.HideInWvW);
            this.RenderSetting(parent, this.ModuleSettings.HideInPvP);
            this.RenderSetting(parent, this.ModuleSettings.ShowTooltips);
            this.RenderSetting(parent, this.ModuleSettings.HandleLeftClick);
            this.RenderSetting(parent, this.ModuleSettings.LeftClickAction);
            this.RenderSetting(parent, this.ModuleSettings.DirectlyTeleportToWaypoint);
            this.RenderSetting(parent, this.ModuleSettings.MapKeybinding);
            this.RenderSetting(parent, this.ModuleSettings.ShowContextMenuOnClick);
            this.RenderSetting(parent, this.ModuleSettings.BuildDirection);
            if (EventTableModule.ModuleInstance.Debug)
            {
                this.RenderEmptyLine(parent);
                this.RenderButton(parent, "Test Error", () => this.ShowError("New error" + new Random().Next()));
                this.RenderTextbox(parent, "Finish Event", "Event.Key", val =>
                 {
                     EventTableModule.ModuleInstance.EventCategories.SelectMany(ec => ec.Events.Where(ev => ev.Key == val)).ToList().ForEach(ev => ev.Hide());
                 });
                this.RenderTextbox(parent, "Finish Category", "EventCategory.Key", val =>
                {
                    EventTableModule.ModuleInstance.EventCategories.Where(ec => ec.Key == val).ToList().ForEach(ev => ev.Hide());
                });
                this.RenderEmptyLine(parent);
                this.RenderButton(parent, "Clear States", async () =>
                {
                    await EventTableModule.ModuleInstance.ClearStates();
                    EventTable.Controls.ScreenNotification.ShowNotification("States cleared");
                });
                this.RenderButton(parent, "Reload States", async () =>
                {
                    await EventTableModule.ModuleInstance.ReloadStates();
                    EventTable.Controls.ScreenNotification.ShowNotification("States reloaded");
                });
            }
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
