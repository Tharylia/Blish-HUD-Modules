namespace Estreya.BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.Shared.Models.Drawers;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class ModuleSettings : BaseModuleSettings
    { 

        #region Global Settings
        public SettingEntry<bool> AutomaticallyUpdateEventFile { get; private set; }
        public SettingEntry<KeyBinding> MapKeybinding { get; private set; }
        #endregion

        #region Events
        private const string EVENT_SETTINGS = "event-settings";
        public SettingCollection EventSettings { get; private set; }
        #endregion

        private const string EVENT_AREA_SETTINGS = "event-area-settings";
        public SettingCollection EventAreaSettings { get; private set; }
        public SettingEntry<List<string>> EventAreaNames { get; private set; }

        public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.E))
        {
        }

        protected override void InitializeAdditionalSettings(SettingCollection settings)
        {
            this.EventAreaSettings = settings.AddSubCollection(EVENT_AREA_SETTINGS);

            this.EventAreaNames = this.EventAreaSettings.DefineSetting(nameof(this.EventAreaNames), new List<string>(), () => "Event Area Names", () => "Defines the event area names.");
        }

        protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
        {
            this.AutomaticallyUpdateEventFile = this.GlobalSettings.DefineSetting(nameof(this.AutomaticallyUpdateEventFile), true, () => "Automatically Update Event File", () => "Whether the module should automatically update the event file.");
            this.AutomaticallyUpdateEventFile.SettingChanged += this.SettingChanged;

            this.MapKeybinding = this.GlobalSettings.DefineSetting(nameof(this.MapKeybinding), new KeyBinding(Microsoft.Xna.Framework.Input.Keys.M), () => "Open Map Hotkey", () => "Defines the key used to open the fullscreen map.");
            this.MapKeybinding.SettingChanged += this.SettingChanged;
            this.MapKeybinding.Value.Enabled = true;
            this.MapKeybinding.Value.BlockSequenceFromGw2 = false;
        }

        public EventAreaConfiguration AddDrawer(string name, List<EventCategory> eventCategories)
        {
            DrawerConfiguration drawer = base.AddDrawer(name);

            var showContextMenu = this.DrawerSettings.DefineSetting($"{name}-showContextMenu", true, () => "Show Context Menu", () => "Whether a context menu should be displayed when right clicking.");
            var leftClickAction = this.DrawerSettings.DefineSetting($"{name}-leftClickAction", Models.LeftClickAction.CopyWaypoint, () => "Left Click Action", () => "Defines the action which is executed when left clicking.");
            var showTooltips = this.DrawerSettings.DefineSetting($"{name}-showTooltips", true, () => "Show Tooltips", () => "Whether a tooltip should be displayed when hovering.");

            var timespan = this.DrawerSettings.DefineSetting($"{name}-timespan", 120, () => "Timespan", () => "Defines the timespan the event drawer covers.");

            var historySplit = this.DrawerSettings.DefineSetting($"{name}-historySplit", 50, () => "History Split", () => "Defines how much history the timespan should contain.");
            historySplit.SetRange(0, 75);

            var drawBorders = this.DrawerSettings.DefineSetting($"{name}-drawBorders", false, () => "Draw Borders", () => "Whether the events should be rendered with borders.");
            var useFillers = this.DrawerSettings.DefineSetting($"{name}-useFillers", true, () => "Use Filler Events", () => "Whether the empty spaces should be filled by filler events.");
            var fillerTextColor = this.DrawerSettings.DefineSetting($"{name}-fillerTextColor", this.DefaultGW2Color, () => "Filler Text Color", () => "Defines the text color used by filler events.");

            var acceptWaypointPrompt = this.DrawerSettings.DefineSetting($"{name}-acceptWaypointPrompt", true, () => "Accept Waypoint Prompt", () => "Whether the waypoint prompt should be accepted automatically when performing an automated teleport.");

            var completionAction = this.DrawerSettings.DefineSetting($"{name}-completionAction", EventCompletedAction.Crossout, () => "Completion Action", () => "Defines the action to perform if an event has been completed.");

            var activeEventKeys = this.DrawerSettings.DefineSetting($"{name}-activeEventKeys", eventCategories.SelectMany(ec => ec.Events.Select(ev => ev.SettingKey)).ToList(), () => "Active Event Keys", () => "Defines the active event keys.");

            return new EventAreaConfiguration()
            {
                Name = drawer.Name,
                Enabled = drawer.Enabled,
                BuildDirection = drawer.BuildDirection,
                BackgroundColor = drawer.BackgroundColor,
                FontSize = drawer.FontSize,
                TextColor = drawer.TextColor,
                Location = drawer.Location,
                Opacity = drawer.Opacity,
                Size = drawer.Size,
                ShowContextMenu = showContextMenu,
                LeftClickAction = leftClickAction,
                ShowTooltips = showTooltips,
                DrawBorders = drawBorders,
                HistorySplit = historySplit,
                TimeSpan = timespan,
                UseFiller = useFillers,
                FillerTextColor = fillerTextColor,
                AcceptWaypointPrompt = acceptWaypointPrompt,
                ActiveEventKeys = activeEventKeys,
                CompletionAcion = completionAction
            };
        }

        public new void RemoveDrawer(string name)
        {
            base.RemoveDrawer(name);

            this.DrawerSettings.UndefineSetting($"{name}-showContextMenu");
            this.DrawerSettings.UndefineSetting($"{name}-leftClickAction");
            this.DrawerSettings.UndefineSetting($"{name}-showTooltips");
            this.DrawerSettings.UndefineSetting($"{name}-timespan");
            this.DrawerSettings.UndefineSetting($"{name}-historySplit");
            this.DrawerSettings.UndefineSetting($"{name}-drawBorders");
            this.DrawerSettings.UndefineSetting($"{name}-useFillers");
            this.DrawerSettings.UndefineSetting($"{name}-acceptWaypointPrompt");
            this.DrawerSettings.UndefineSetting($"{name}-completionAction");
            this.DrawerSettings.UndefineSetting($"{name}-activeEventKeys");
        }

        public override void UpdateLocalization(TranslationState translationState)
        {
            base.UpdateLocalization(translationState);

            var automaticallyUpdateEventFileDisplayNameDefault = this.AutomaticallyUpdateEventFile.DisplayName;
            var automaticallyUpdateEventFileDescriptionDefault = this.AutomaticallyUpdateEventFile.Description;
            this.AutomaticallyUpdateEventFile.GetDisplayNameFunc = () => translationState.GetTranslation("setting-automaticallyUpdateEventFile-name", automaticallyUpdateEventFileDisplayNameDefault);
            this.AutomaticallyUpdateEventFile.GetDescriptionFunc = () => translationState.GetTranslation("setting-automaticallyUpdateEventFile-description", automaticallyUpdateEventFileDescriptionDefault);

            var mapKeybindingDisplayNameDefault = this.MapKeybinding.DisplayName;
            var mapKeybindingDescriptionDefault = this.MapKeybinding.Description;
            this.MapKeybinding.GetDisplayNameFunc = () => translationState.GetTranslation("setting-mapKeybinding-name", mapKeybindingDisplayNameDefault);
            this.MapKeybinding.GetDescriptionFunc = () => translationState.GetTranslation("setting-mapKeybinding-description", mapKeybindingDescriptionDefault);
        }

        public void UpdateDrawerLocalization(EventAreaConfiguration drawerConfiguration, TranslationState translationState)
        {
            base.UpdateDrawerLocalization(drawerConfiguration, translationState);
        }
    }
}
