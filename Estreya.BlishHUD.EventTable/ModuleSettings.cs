namespace Estreya.BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Controls;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.Models.Drawers;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
    using Newtonsoft.Json;
    using SharpDX;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class ModuleSettings : BaseModuleSettings
    {
        #region Global Settings
        public SettingEntry<KeyBinding> MapKeybinding { get; private set; }
        #endregion

        #region Events
        private const string EVENT_SETTINGS = "event-settings";
        public SettingCollection EventSettings { get; private set; }
        #endregion

        private const string EVENT_AREA_SETTINGS = "event-area-settings";
        public SettingCollection EventAreaSettings { get; private set; }
        public SettingEntry<List<string>> EventAreaNames { get; private set; }

        public SettingEntry<bool> RemindersEnabled { get; private set; }
        public EventReminderPositition ReminderPosition { get; private set; }
        public SettingEntry<float> ReminderDuration { get; private set; }

        public SettingEntry<float> ReminderOpacity { get; private set; }

        /// <summary>
        /// Contains a list of event setting keys for which NO reminder should be displayed.
        /// </summary>
        public SettingEntry<List<string>> ReminderDisabledForEvents { get; set; }

        public SettingEntry<bool> ShowDynamicEventsOnMap { get; private set; }

        public SettingEntry<bool> ShowDynamicEventInWorld { get; private set; }

        public SettingEntry<bool> ShowDynamicEventsInWorldOnlyWhenInside { get; private set; }

        public SettingEntry<bool> IgnoreZAxisOnDynamicEventsInWorld { get; private set; }

        public SettingEntry<int> DynamicEventsRenderDistance { get; private set; }

        public SettingEntry<List<string>> DisabledDynamicEventIds { get; private set; }

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
            this.MapKeybinding = this.GlobalSettings.DefineSetting(nameof(this.MapKeybinding), new KeyBinding(Microsoft.Xna.Framework.Input.Keys.M), () => "Open Map Hotkey", () => "Defines the key used to open the fullscreen map.");
            this.MapKeybinding.SettingChanged += this.SettingChanged;
            this.MapKeybinding.Value.Enabled = true;
            this.MapKeybinding.Value.BlockSequenceFromGw2 = false;

            this.RemindersEnabled = this.GlobalSettings.DefineSetting(nameof(this.RemindersEnabled), true, () => "Reminders Enabled", () => "Whether the module should display alerts before an event starts.");
            this.ReminderPosition = new EventReminderPositition()
            {
                X = this.GlobalSettings.DefineSetting($"ReminderPositionX", 200, () => "Location X", () => "Defines the position of reminders on the x axis."),
                Y = this.GlobalSettings.DefineSetting($"ReminderPositionY", 200, () => "Location Y", () => "Defines the position of reminders on the y axis.")
            };

            var reminderDurationMin = 1;
            var reminderDurationMax = 15;
            this.ReminderDuration = this.GlobalSettings.DefineSetting(nameof(this.ReminderDuration), 5f, () => "Reminder Duration", () => $"Defines the reminder duration.");
            this.ReminderDuration.SetRange(reminderDurationMin, reminderDurationMax);

            this.ReminderDisabledForEvents = this.GlobalSettings.DefineSetting(nameof(this.ReminderDisabledForEvents), new List<string>(), () => "Reminder disabled for Events", () => "Defines the events for which NO reminder should be displayed.");

            this.ReminderOpacity = this.GlobalSettings.DefineSetting(nameof(this.ReminderOpacity), 0.5f, () => "Reminder Opacity", () => "Defines the background opacity for reminders.");
            this.ReminderOpacity.SetRange(0.1f, 1f);

            this.ShowDynamicEventsOnMap = this.GlobalSettings.DefineSetting(nameof(this.ShowDynamicEventsOnMap), false, () => "Show Dynamic Events on Map", () => "Whether the dynamic events of the map should be shown.");

            this.ShowDynamicEventInWorld = this.GlobalSettings.DefineSetting(nameof(this.ShowDynamicEventInWorld), false, () => "Show Dynamic Events in World", () => "Whether dynamic events should be shown inside the world.");
            this.ShowDynamicEventInWorld.SettingChanged += this.ShowDynamicEventInWorld_SettingChanged;

            this.ShowDynamicEventsInWorldOnlyWhenInside = this.GlobalSettings.DefineSetting(nameof(this.ShowDynamicEventsInWorldOnlyWhenInside), true, () => "Show only when inside.", () => "Whether the dynamic events inside the world should only show up when the player is inside.");
            this.ShowDynamicEventsInWorldOnlyWhenInside.SettingChanged += this.ShowDynamicEventsInWorldOnlyWhenInside_SettingChanged;

            this.IgnoreZAxisOnDynamicEventsInWorld = this.GlobalSettings.DefineSetting(nameof(this.IgnoreZAxisOnDynamicEventsInWorld), true, () => "Ignore Z Axis", () => "Defines whether the z axis should be ignored when calculating the visibility of in world events.");

            this.DynamicEventsRenderDistance = this.GlobalSettings.DefineSetting(nameof(this.DynamicEventsRenderDistance), 300, () => "Dynamic Event Render Distance", () => "Defines the distance in which dynamic events should be rendered");
            this.DynamicEventsRenderDistance.SetRange(50, 500);

            this.DisabledDynamicEventIds = this.GlobalSettings.DefineSetting(nameof(this.DisabledDynamicEventIds), new List<string>(), () => "Disabled Dynamic Events", () => "Defines which dynamic events are disabled.");

            this.HandleEnabledStates();
        }

        private void ShowDynamicEventInWorld_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            this.HandleEnabledStates();
        }

        private void ShowDynamicEventsInWorldOnlyWhenInside_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            this.HandleEnabledStates();
        }

        private void HandleEnabledStates()
        {
            this.ShowDynamicEventsInWorldOnlyWhenInside.SetDisabled(!this.ShowDynamicEventInWorld.Value);
            this.IgnoreZAxisOnDynamicEventsInWorld.SetDisabled(!this.ShowDynamicEventInWorld.Value || !this.ShowDynamicEventsInWorldOnlyWhenInside.Value);
            this.DynamicEventsRenderDistance.SetDisabled(!this.ShowDynamicEventInWorld.Value || this.ShowDynamicEventsInWorldOnlyWhenInside.Value);
        }

        public void CheckDrawerSizeAndPosition(EventAreaConfiguration configuration)
        {
            base.CheckDrawerSizeAndPosition(configuration);
        }

        public void CheckGlobalSizeAndPosition()
        {
            int maxResX = (int)(GameService.Graphics.Resolution.X / GameService.Graphics.UIScaleMultiplier);
            int maxResY = (int)(GameService.Graphics.Resolution.Y / GameService.Graphics.UIScaleMultiplier);

            int minLocationX = 0;
            int maxLocationX = maxResX - EventNotification.NOTIFICATION_WIDTH;
            int minLocationY = 0;
            int maxLocationY = maxResY - EventNotification.NOTIFICATION_HEIGHT;

            this.ReminderPosition?.X.SetRange(minLocationX, maxLocationX);
            this.ReminderPosition?.Y.SetRange(minLocationY, maxLocationY);
        }

        public EventAreaConfiguration AddDrawer(string name, List<EventCategory> eventCategories)
        {
            DrawerConfiguration drawer = base.AddDrawer(name);

            var leftClickAction = this.DrawerSettings.DefineSetting($"{name}-leftClickAction", Models.LeftClickAction.CopyWaypoint, () => "Left Click Action", () => "Defines the action which is executed when left clicking.");
            var showTooltips = this.DrawerSettings.DefineSetting($"{name}-showTooltips", true, () => "Show Tooltips", () => "Whether a tooltip should be displayed when hovering.");

            var timespan = this.DrawerSettings.DefineSetting($"{name}-timespan", 120, () => "Timespan", () => "Defines the timespan the event drawer covers.");
            timespan.SetRange(60, 240);

            var historySplit = this.DrawerSettings.DefineSetting($"{name}-historySplit", 50, () => "History Split", () => "Defines how much history the timespan should contain.");
            historySplit.SetRange(0, 75);

            var drawBorders = this.DrawerSettings.DefineSetting($"{name}-drawBorders", false, () => "Draw Borders", () => "Whether the events should be rendered with borders.");
            var useFillers = this.DrawerSettings.DefineSetting($"{name}-useFillers", true, () => "Use Filler Events", () => "Whether the empty spaces should be filled by filler events.");
            var fillerTextColor = this.DrawerSettings.DefineSetting($"{name}-fillerTextColor", this.DefaultGW2Color, () => "Filler Text Color", () => "Defines the text color used by filler events.");

            var acceptWaypointPrompt = this.DrawerSettings.DefineSetting($"{name}-acceptWaypointPrompt", true, () => "Accept Waypoint Prompt", () => "Whether the waypoint prompt should be accepted automatically when performing an automated teleport.");

            var completionAction = this.DrawerSettings.DefineSetting($"{name}-completionAction", EventCompletedAction.Crossout, () => "Completion Action", () => "Defines the action to perform if an event has been completed.");

            var disabledEventKeys = this.DrawerSettings.DefineSetting($"{name}-disabledEventKeys",new List<string>(), () => "Active Event Keys", () => "Defines the active event keys.");

            var eventHeight = this.DrawerSettings.DefineSetting($"{name}-eventHeight", 30, () => "Event Height", () => "Defines the height of the individual event rows.");
            eventHeight.SetRange(5, 30);

            var eventOrder = this.DrawerSettings.DefineSetting($"{name}-eventOrder", new List<string>(eventCategories.Select(x => x.Key)), () => "Event Order", () => "Defines the order of events.");

            var eventOpacity = this.DrawerSettings.DefineSetting($"{name}-eventOpacity", 1f, () => "Event Opacity", () => "Defines the opacity of the individual events.");
            eventOpacity.SetRange(0.1f, 1f);

            var drawShadows = this.DrawerSettings.DefineSetting($"{name}-drawShadows", false, () => "Draw Shadows", () => "Whether the text should have shadows");

            var shadowColor = this.DrawerSettings.DefineSetting($"{name}-shadowColor", this.DefaultGW2Color, () => "Shadow Color", () => "Defines the color of the shadows");

            var drawShadowsForFiller = this.DrawerSettings.DefineSetting($"{name}-drawShadowsForFiller", false, () => "Draw Shadows for Filler", () => "Whether the filler text should have shadows");

            var fillerShadowColor = this.DrawerSettings.DefineSetting($"{name}-fillerShadowColor", this.DefaultGW2Color, () => "Filler Shadow Color", () => "Defines the color of the shadows for fillers");


            return new EventAreaConfiguration()
            {
                Name = drawer.Name,
                Enabled = drawer.Enabled,
                EnabledKeybinding = drawer.EnabledKeybinding,
                BuildDirection = drawer.BuildDirection,
                BackgroundColor = drawer.BackgroundColor,
                FontSize = drawer.FontSize,
                TextColor = drawer.TextColor,
                Location = drawer.Location,
                Opacity = drawer.Opacity,
                Size = drawer.Size,
                LeftClickAction = leftClickAction,
                ShowTooltips = showTooltips,
                DrawBorders = drawBorders,
                HistorySplit = historySplit,
                TimeSpan = timespan,
                UseFiller = useFillers,
                FillerTextColor = fillerTextColor,
                AcceptWaypointPrompt = acceptWaypointPrompt,
                DisabledEventKeys = disabledEventKeys,
                CompletionAcion = completionAction,
                EventHeight = eventHeight,
                EventOrder = eventOrder,
                EventOpacity = eventOpacity,
                DrawShadows = drawShadows,
                ShadowColor= shadowColor,
                DrawShadowsForFiller = drawShadowsForFiller,
                FillerShadowColor = fillerShadowColor
            };
        }

        public new void RemoveDrawer(string name)
        {
            base.RemoveDrawer(name);

            this.DrawerSettings.UndefineSetting($"{name}-leftClickAction");
            this.DrawerSettings.UndefineSetting($"{name}-showTooltips");
            this.DrawerSettings.UndefineSetting($"{name}-timespan");
            this.DrawerSettings.UndefineSetting($"{name}-historySplit");
            this.DrawerSettings.UndefineSetting($"{name}-drawBorders");
            this.DrawerSettings.UndefineSetting($"{name}-useFillers");
            this.DrawerSettings.UndefineSetting($"{name}-fillerTextColor");
            this.DrawerSettings.UndefineSetting($"{name}-acceptWaypointPrompt");
            this.DrawerSettings.UndefineSetting($"{name}-completionAction");
            this.DrawerSettings.UndefineSetting($"{name}-disabledEventKeys");
            this.DrawerSettings.UndefineSetting($"{name}-eventHeight");
            this.DrawerSettings.UndefineSetting($"{name}-eventOrder");
            this.DrawerSettings.UndefineSetting($"{name}-eventOpacity");
            this.DrawerSettings.UndefineSetting($"{name}-drawShadows");
            this.DrawerSettings.UndefineSetting($"{name}-shadowColor");
            this.DrawerSettings.UndefineSetting($"{name}-drawShadowsForFiller");
            this.DrawerSettings.UndefineSetting($"{name}-fillerShadowColor");
        }

        public override void UpdateLocalization(TranslationState translationState)
        {
            base.UpdateLocalization(translationState);

            var mapKeybindingDisplayNameDefault = this.MapKeybinding.DisplayName;
            var mapKeybindingDescriptionDefault = this.MapKeybinding.Description;
            this.MapKeybinding.GetDisplayNameFunc = () => translationState.GetTranslation("setting-mapKeybinding-name", mapKeybindingDisplayNameDefault);
            this.MapKeybinding.GetDescriptionFunc = () => translationState.GetTranslation("setting-mapKeybinding-description", mapKeybindingDescriptionDefault);

            var remindersEnabledDisplayNameDefault = this.RemindersEnabled.DisplayName;
            var remindersEnabledDescriptionDefault = this.RemindersEnabled.Description;
            this.RemindersEnabled.GetDisplayNameFunc = () => translationState.GetTranslation("setting-remindersEnabled-name", remindersEnabledDisplayNameDefault);
            this.RemindersEnabled.GetDescriptionFunc = () => translationState.GetTranslation("setting-remindersEnabled-description", remindersEnabledDescriptionDefault);

            var reminderPositionXDisplayNameDefault = this.ReminderPosition.X.DisplayName;
            var reminderPositionXDescriptionDefault = this.ReminderPosition.X.Description;
            this.ReminderPosition.X.GetDisplayNameFunc = () => translationState.GetTranslation("setting-reminderPositionX-name", reminderPositionXDisplayNameDefault);
            this.ReminderPosition.X.GetDescriptionFunc = () => translationState.GetTranslation("setting-reminderPositionX-description", reminderPositionXDescriptionDefault);

            var reminderPositionYDisplayNameDefault = this.ReminderPosition.Y.DisplayName;
            var reminderPositionYDescriptionDefault = this.ReminderPosition.Y.Description;
            this.ReminderPosition.Y.GetDisplayNameFunc = () => translationState.GetTranslation("setting-reminderPositionY-name", reminderPositionYDisplayNameDefault);
            this.ReminderPosition.Y.GetDescriptionFunc = () => translationState.GetTranslation("setting-reminderPositionY-description", reminderPositionYDescriptionDefault);

            var reminderDurationDisplayNameDefault = this.ReminderDuration.DisplayName;
            var reminderDurationDescriptionDefault = this.ReminderDuration.Description;
            this.ReminderDuration.GetDisplayNameFunc = () => translationState.GetTranslation("setting-reminderDuration-name", reminderDurationDisplayNameDefault);
            this.ReminderDuration.GetDescriptionFunc = () => translationState.GetTranslation("setting-reminderDuration-description", reminderDurationDescriptionDefault);

            var reminderOpacityDisplayNameDefault = this.ReminderOpacity.DisplayName;
            var reminderOpacityDescriptionDefault = this.ReminderOpacity.Description;
            this.ReminderOpacity.GetDisplayNameFunc = () => translationState.GetTranslation("setting-reminderOpacity-name", reminderOpacityDisplayNameDefault);
            this.ReminderOpacity.GetDescriptionFunc = () => translationState.GetTranslation("setting-reminderOpacity-description", reminderOpacityDescriptionDefault);
        }

        public void UpdateDrawerLocalization(EventAreaConfiguration drawerConfiguration, TranslationState translationState)
        {
            base.UpdateDrawerLocalization(drawerConfiguration, translationState);

            var leftClickActionDisplayNameDefault = drawerConfiguration.LeftClickAction.DisplayName;
            var leftClickActionDescriptionDefault = drawerConfiguration.LeftClickAction.Description;
            drawerConfiguration.LeftClickAction.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerLeftClickAction-name", leftClickActionDisplayNameDefault);
            drawerConfiguration.LeftClickAction.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerLeftClickAction-description", leftClickActionDescriptionDefault);

            var showTooltipsDisplayNameDefault = drawerConfiguration.ShowTooltips.DisplayName;
            var showTooltipsDescriptionDefault = drawerConfiguration.ShowTooltips.Description;
            drawerConfiguration.ShowTooltips.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerShowTooltips-name", showTooltipsDisplayNameDefault);
            drawerConfiguration.ShowTooltips.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerShowTooltips-description", showTooltipsDescriptionDefault);

            var timespanDisplayNameDefault = drawerConfiguration.TimeSpan.DisplayName;
            var timespanDescriptionDefault = drawerConfiguration.TimeSpan.Description;
            drawerConfiguration.TimeSpan.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerTimespan-name", timespanDisplayNameDefault);
            drawerConfiguration.TimeSpan.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerTimespan-description", timespanDescriptionDefault);

            var historySplitDisplayNameDefault = drawerConfiguration.HistorySplit.DisplayName;
            var historySplitDescriptionDefault = drawerConfiguration.HistorySplit.Description;
            drawerConfiguration.HistorySplit.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerHistorySplit-name", historySplitDisplayNameDefault);
            drawerConfiguration.HistorySplit.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerHistorySplit-description", historySplitDescriptionDefault);

            var drawBordersDisplayNameDefault = drawerConfiguration.DrawBorders.DisplayName;
            var drawBordersDescriptionDefault = drawerConfiguration.DrawBorders.Description;
            drawerConfiguration.DrawBorders.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerDrawBorders-name", drawBordersDisplayNameDefault);
            drawerConfiguration.DrawBorders.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerDrawBorders-description", drawBordersDescriptionDefault);

            var useFillersDisplayNameDefault = drawerConfiguration.UseFiller.DisplayName;
            var useFillersDescriptionDefault = drawerConfiguration.UseFiller.Description;
            drawerConfiguration.UseFiller.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerUseFillers-name", useFillersDisplayNameDefault);
            drawerConfiguration.UseFiller.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerUseFillers-description", useFillersDescriptionDefault);

            var fillerTextColorDisplayNameDefault = drawerConfiguration.FillerTextColor.DisplayName;
            var fillerTextColorDescriptionDefault = drawerConfiguration.FillerTextColor.Description;
            drawerConfiguration.FillerTextColor.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerFillerTextColor-name", fillerTextColorDisplayNameDefault);
            drawerConfiguration.FillerTextColor.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerFillerTextColor-description", fillerTextColorDescriptionDefault);

            var acceptWaypointPromptDisplayNameDefault = drawerConfiguration.AcceptWaypointPrompt.DisplayName;
            var acceptWaypointPromptDescriptionDefault = drawerConfiguration.AcceptWaypointPrompt.Description;
            drawerConfiguration.AcceptWaypointPrompt.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerAcceptWaypointPrompt-name", acceptWaypointPromptDisplayNameDefault);
            drawerConfiguration.AcceptWaypointPrompt.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerAcceptWaypointPrompt-description", acceptWaypointPromptDescriptionDefault);

            var completionActionDisplayNameDefault = drawerConfiguration.CompletionAcion.DisplayName;
            var completionActionDescriptionDefault = drawerConfiguration.CompletionAcion.Description;
            drawerConfiguration.CompletionAcion.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerCompletionAction-name", completionActionDisplayNameDefault);
            drawerConfiguration.CompletionAcion.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerCompletionAction-description", completionActionDescriptionDefault);

            var eventHeightDisplayNameDefault = drawerConfiguration.EventHeight.DisplayName;
            var eventHeightDescriptionDefault = drawerConfiguration.EventHeight.Description;
            drawerConfiguration.EventHeight.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerEventHeight-name", eventHeightDisplayNameDefault);
            drawerConfiguration.EventHeight.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerEventHeight-description", eventHeightDescriptionDefault);

            var eventOpacityDisplayNameDefault = drawerConfiguration.EventOpacity.DisplayName;
            var eventOpacityDescriptionDefault = drawerConfiguration.EventOpacity.Description;
            drawerConfiguration.EventOpacity.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerEventOpacity-name", eventOpacityDisplayNameDefault);
            drawerConfiguration.EventOpacity.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerEventOpacity-description", eventOpacityDescriptionDefault);
        }

        public override void Unload()
        {
            base.Unload();
            this.ShowDynamicEventInWorld.SettingChanged -= this.ShowDynamicEventInWorld_SettingChanged;
            this.ShowDynamicEventsInWorldOnlyWhenInside.SettingChanged -= this.ShowDynamicEventsInWorldOnlyWhenInside_SettingChanged;
        }
    }
}
