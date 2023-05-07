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
    using Estreya.BlishHUD.Shared.Services;
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
        private const string EVENT_AREA_SETTINGS = "event-area-settings";
        private SettingCollection EventAreaSettings { get; set; }
        public SettingEntry<List<string>> EventAreaNames { get; private set; }

        public SettingEntry<KeyBinding> MapKeybinding { get; private set; }

        public SettingEntry<bool> RemindersEnabled { get; private set; }
        public EventReminderPositition ReminderPosition { get; private set; }
        public SettingEntry<float> ReminderDuration { get; private set; }

        public SettingEntry<float> ReminderOpacity { get; private set; }

        /// <summary>
        /// Contains a list of event setting keys for which NO reminder should be displayed.
        /// </summary>
        public SettingEntry<List<string>> ReminderDisabledForEvents { get; private set; }

        public SettingEntry<Dictionary<string, List<TimeSpan>>> ReminderTimesOverride { get; private set; }

        public SettingEntry<bool> ShowDynamicEventsOnMap { get; private set; }

        public SettingEntry<bool> ShowDynamicEventInWorld { get; private set; }

        public SettingEntry<bool> ShowDynamicEventsInWorldOnlyWhenInside { get; private set; }

        public SettingEntry<bool> IgnoreZAxisOnDynamicEventsInWorld { get; private set; }

        public SettingEntry<int> DynamicEventsRenderDistance { get; private set; }

        public SettingEntry<List<string>> DisabledDynamicEventIds { get; private set; }

        public SettingEntry<MenuEventSortMode> MenuEventSortMode { get; private set; }

        public SettingEntry<bool> HideRemindersOnMissingMumbleTicks { get; private set; }
        public SettingEntry<bool> HideRemindersInCombat { get; private set; }
        public SettingEntry<bool> HideRemindersOnOpenMap { get; private set; }
        public SettingEntry<bool> HideRemindersInPvE_OpenWorld { get; private set; }
        public SettingEntry<bool> HideRemindersInPvE_Competetive { get; private set; }
        public SettingEntry<bool> HideRemindersInWvW { get; private set; }
        public SettingEntry<bool> HideRemindersInPvP { get; private set; }

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

            this.ReminderTimesOverride = this.GlobalSettings.DefineSetting(nameof(this.ReminderTimesOverride), new Dictionary<string, List<TimeSpan>>(), () => "Reminder Times Override", () => "Defines the overridden times for reminders per event.");

            this.ReminderOpacity = this.GlobalSettings.DefineSetting(nameof(this.ReminderOpacity), 0.5f, () => "Reminder Opacity", () => "Defines the background opacity for reminders.");
            this.ReminderOpacity.SetRange(0.1f, 1f);

            this.ShowDynamicEventsOnMap = this.GlobalSettings.DefineSetting(nameof(this.ShowDynamicEventsOnMap), false, () => "Show Dynamic Events on Map", () => "Whether the dynamic events of the map should be shown.");

            this.ShowDynamicEventInWorld = this.GlobalSettings.DefineSetting(nameof(this.ShowDynamicEventInWorld), false, () => "Show Dynamic Events in World", () => "Whether dynamic events should be shown inside the world.");
            this.ShowDynamicEventInWorld.SettingChanged += this.ShowDynamicEventInWorld_SettingChanged;

            this.ShowDynamicEventsInWorldOnlyWhenInside = this.GlobalSettings.DefineSetting(nameof(this.ShowDynamicEventsInWorldOnlyWhenInside), true, () => "Show only when inside", () => "Whether the dynamic events inside the world should only show up when the player is inside.");
            this.ShowDynamicEventsInWorldOnlyWhenInside.SettingChanged += this.ShowDynamicEventsInWorldOnlyWhenInside_SettingChanged;

            this.IgnoreZAxisOnDynamicEventsInWorld = this.GlobalSettings.DefineSetting(nameof(this.IgnoreZAxisOnDynamicEventsInWorld), true, () => "Ignore Z Axis", () => "Defines whether the z axis should be ignored when calculating the visibility of in world events.");

            this.DynamicEventsRenderDistance = this.GlobalSettings.DefineSetting(nameof(this.DynamicEventsRenderDistance), 300, () => "Dynamic Event Render Distance", () => "Defines the distance in which dynamic events should be rendered.");
            this.DynamicEventsRenderDistance.SetRange(50, 500);

            this.DisabledDynamicEventIds = this.GlobalSettings.DefineSetting(nameof(this.DisabledDynamicEventIds), new List<string>(), () => "Disabled Dynamic Events", () => "Defines which dynamic events are disabled.");

            this.MenuEventSortMode = this.GlobalSettings.DefineSetting(nameof(this.MenuEventSortMode), Models.MenuEventSortMode.Default, () => "Menu Event Sort Mode", () => "Defines the mode by which the events in menu views are sorted by.");

            this.HideRemindersOnOpenMap = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersOnOpenMap), false, () => "Hide Reminders on open Map", () => "Whether the reminders should hide when the map is open.");

            this.HideRemindersOnMissingMumbleTicks = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersOnMissingMumbleTicks), true, () => "Hide Reminders on Cutscenes", () => "Whether the reminders should hide when cutscenes are played.");

            this.HideRemindersInCombat = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersInCombat), false, () => "Hide Reminders in Combat", () => "Whether the reminders should hide when in combat.");

            this.HideRemindersInPvE_OpenWorld = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersInPvE_OpenWorld), false, () => "Hide Reminders in PvE (Open World)", () => "Whether the reminders should hide when in PvE (Open World).");

            this.HideRemindersInPvE_Competetive = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersInPvE_Competetive), false, () => "Hide Reminders in PvE (Competetive)", () => "Whether the reminders should hide when in PvE (Competetive).");

            this.HideRemindersInWvW = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersInWvW), false, () => "Hide Reminders in WvW", () => "Whether the reminders should hide when in world vs. world.");

            this.HideRemindersInPvP = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersInPvP), false, () => "Hide Reminders in PvP", () => "Whether the reminders should hide when in player vs. player.");

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
            var enableHistorySplitScrolling = this.DrawerSettings.DefineSetting($"{name}-enableHistorySplitScrolling", false, () => "Enable History Split Scrolling", () => "Defines if scrolling inside the event area temporary moves the history split until the mouse leaves the area.");

            var historySplitScrollingSpeed = this.DrawerSettings.DefineSetting($"{name}-historySplitScrollingSpeed", 1, () => "History Split Scrolling Speed", () => "Defines the speed when scrolling inside the event area.");
            historySplitScrollingSpeed.SetRange(1, 10);

            var drawBorders = this.DrawerSettings.DefineSetting($"{name}-drawBorders", false, () => "Draw Borders", () => "Whether the events should be rendered with borders.");
            var useFillers = this.DrawerSettings.DefineSetting($"{name}-useFillers", true, () => "Use Filler Events", () => "Whether the empty spaces should be filled by filler events.");
            var fillerTextColor = this.DrawerSettings.DefineSetting($"{name}-fillerTextColor", this.DefaultGW2Color, () => "Filler Text Color", () => "Defines the text color used by filler events.");

            var acceptWaypointPrompt = this.DrawerSettings.DefineSetting($"{name}-acceptWaypointPrompt", true, () => "Accept Waypoint Prompt", () => "Whether the waypoint prompt should be accepted automatically when performing an automated teleport.");

            var completionAction = this.DrawerSettings.DefineSetting($"{name}-completionAction", EventCompletedAction.Crossout, () => "Completion Action", () => "Defines the action to perform if an event has been completed.");

            var disabledEventKeys = this.DrawerSettings.DefineSetting($"{name}-disabledEventKeys", new List<string>(), () => "Active Event Keys", () => "Defines the active event keys.");

            var eventHeight = this.DrawerSettings.DefineSetting($"{name}-eventHeight", 30, () => "Event Height", () => "Defines the height of the individual event rows.");
            eventHeight.SetRange(5, 30);

            var eventOrder = this.DrawerSettings.DefineSetting($"{name}-eventOrder", new List<string>(eventCategories.Select(x => x.Key)), () => "Event Order", () => "Defines the order of events.");

            var eventBackgroundOpacity = this.DrawerSettings.DefineSetting($"{name}-eventBackgroundOpacity", 1f, () => "Event Background Opacity", () => "Defines the opacity of the individual event backgrounds.");
            eventBackgroundOpacity.SetRange(0.1f, 1f);

            var drawShadows = this.DrawerSettings.DefineSetting($"{name}-drawShadows", false, () => "Draw Shadows", () => "Whether the text should have shadows");

            var shadowColor = this.DrawerSettings.DefineSetting($"{name}-shadowColor", this.DefaultGW2Color, () => "Shadow Color", () => "Defines the color of the shadows");

            var drawShadowsForFiller = this.DrawerSettings.DefineSetting($"{name}-drawShadowsForFiller", false, () => "Draw Shadows for Filler", () => "Whether the filler text should have shadows");

            var fillerShadowColor = this.DrawerSettings.DefineSetting($"{name}-fillerShadowColor", this.DefaultGW2Color, () => "Filler Shadow Color", () => "Defines the color of the shadows for fillers");

            var drawInterval = this.DrawerSettings.DefineSetting($"{name}-drawInterval", DrawInterval.FAST, () => "Draw Interval", () => "Defines the refresh rate of the drawer.");

            var limitToCurrentMap = this.DrawerSettings.DefineSetting($"{name}-limitToCurrentMap", false, () => "Limit to current Map", () => "Whether the drawer should only show events from the current map.");

            var allowUnspecifiedMap = this.DrawerSettings.DefineSetting($"{name}-allowUnspecifiedMap", true, () => "Allow from unspecified Maps", () => "Whether the table should show events which do not have a map id specified.");

            var timeLineOpacity = this.DrawerSettings.DefineSetting($"{name}-timeLineOpacity", 1f, () => "Timeline Opacity", () => "Defines the opacity of the time line bar.");
            timeLineOpacity.SetRange(0.1f, 1f);

            var eventTextOpacity = this.DrawerSettings.DefineSetting($"{name}-eventTextOpacity", 1f, () => "Event Text Opacity", () => "Defines the opacity of the event text.");
            eventTextOpacity.SetRange(0.1f, 1f);

            var fillerTextOpacity = this.DrawerSettings.DefineSetting($"{name}-fillerTextOpacity", 1f, () => "Filler Text Opacity", () => "Defines the opacity of filler event text.");
            fillerTextOpacity.SetRange(0.1f, 1f);

            var shadowOpacity = this.DrawerSettings.DefineSetting($"{name}-shadowOpacity", 1f, () => "Shadow Opacity", () => "Defines the opacity for shadows.");
            shadowOpacity.SetRange(0.1f, 1f);

            var fillerShadowOpacity = this.DrawerSettings.DefineSetting($"{name}-fillerShadowOpacity", 1f, () => "Filler Shadow Opacity", () => "Defines the opacity for filler shadows.");
            fillerShadowOpacity.SetRange(0.1f, 1f);

            var completedEventsBackgroundOpacity = this.DrawerSettings.DefineSetting($"{name}-completedEventsBackgroundOpacity", 0.5f, () => "Completed Events Background Opacity", () => "Defines the background opacity of completed events. Only works in combination with CompletionAction = Change Opacity");
            completedEventsBackgroundOpacity.SetRange(0.1f, 0.9f);

            var completedEventsTextOpacity = this.DrawerSettings.DefineSetting($"{name}-completedEventsTextOpacity", 1f, () => "Completed Events Text Opacity", () => "Defines the text opacity of completed events. Only works in combination with CompletionAction = Change Opacity");
            completedEventsBackgroundOpacity.SetRange(0f, 1f);

            var completedEventsInvertTextColor = this.DrawerSettings.DefineSetting($"{name}-completedEventsInvertTextColor", true, () => "Completed Events Invert Textcolor", () => "Specified if completed events should have their text color inverted. Only works in combination with CompletionAction = Change Opacity");

            var hideOnOpenMap = this.DrawerSettings.DefineSetting($"{name}-hideOnOpenMap", true, () => "Hide on open Map", () => "Whether the area should hide when the map is open.");

            var hideOnMissingMumbleTicks = this.DrawerSettings.DefineSetting($"{name}-hideOnMissingMumbleTicks", true, () => "Hide on Cutscenes", () => "Whether the area should hide when cutscenes are played.");

            var hideInCombat = this.DrawerSettings.DefineSetting($"{name}-hideInCombat", false, () => "Hide in Combat", () => "Whether the area should hide when in combat.");

            var hideInPvE_OpenWorld = this.DrawerSettings.DefineSetting($"{name}-hideInPvE_OpenWorld", false, () => "Hide in PvE (Open World)", () => "Whether the area should hide when in PvE (Open World).");

            var hideInPvE_Competetive = this.DrawerSettings.DefineSetting($"{name}-hideInPvE_Competetive", false, () => "Hide in PvE (Competetive)", () => "Whether the area should hide when in PvE (Competetive).");

            var hideInWvW = this.DrawerSettings.DefineSetting($"{name}-hideInWvW", false, () => "Hide in WvW", () => "Whether the area should hide when in world vs. world.");

            var hideInPvP = this.DrawerSettings.DefineSetting($"{name}-hideInPvP", false, () => "Hide in PvP", () => "Whether the area should hide when in player vs. player.");

            var showCategoryNames = this.DrawerSettings.DefineSetting($"{name}-showCategoryNames", false, () => "Show Category Names", () => "Defines if the category names should be shown before the event bars.");

            var categoryNameColor = this.DrawerSettings.DefineSetting($"{name}-categoryNameColor", this.DefaultGW2Color, () => "Category Name Color", () => "Defines the color of the category names.");

            var enableColorGradients = this.DrawerSettings.DefineSetting($"{name}-enableColorGradients", false, () => "Enable Color Gradients", () => "Defines if supported events should have a smoother color gradient from and to the next event.");

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
                EnableHistorySplitScrolling = enableHistorySplitScrolling,
                HistorySplitScrollingSpeed = historySplitScrollingSpeed,
                TimeSpan = timespan,
                UseFiller = useFillers,
                FillerTextColor = fillerTextColor,
                AcceptWaypointPrompt = acceptWaypointPrompt,
                DisabledEventKeys = disabledEventKeys,
                CompletionAction = completionAction,
                EventHeight = eventHeight,
                EventOrder = eventOrder,
                EventBackgroundOpacity = eventBackgroundOpacity,
                DrawShadows = drawShadows,
                ShadowColor = shadowColor,
                DrawShadowsForFiller = drawShadowsForFiller,
                FillerShadowColor = fillerShadowColor,
                DrawInterval = drawInterval,
                LimitToCurrentMap = limitToCurrentMap,
                AllowUnspecifiedMap = allowUnspecifiedMap,
                TimeLineOpacity = timeLineOpacity,
                EventTextOpacity = eventTextOpacity,
                FillerTextOpacity = fillerTextOpacity,
                ShadowOpacity = shadowOpacity,
                FillerShadowOpacity = fillerShadowOpacity,
                CompletedEventsBackgroundOpacity = completedEventsBackgroundOpacity,
                CompletedEventsTextOpacity = completedEventsTextOpacity,
                CompletedEventsInvertTextColor = completedEventsInvertTextColor,
                HideInCombat = hideInCombat,
                HideOnMissingMumbleTicks = hideOnMissingMumbleTicks,
                HideOnOpenMap = hideOnOpenMap,
                HideInPvE_Competetive = hideInPvE_Competetive,
                HideInPvE_OpenWorld = hideInPvE_OpenWorld,
                HideInPvP = hideInPvP,
                HideInWvW = hideInWvW,
                ShowCategoryNames = showCategoryNames,
                CategoryNameColor = categoryNameColor,
                EnableColorGradients = enableColorGradients,
            };
        }

        public void CheckDrawerSettings(EventAreaConfiguration configuration, List<EventCategory> categories)
        {
            var notOrderedEventCategories = categories.Where(ec => !configuration.EventOrder.Value.Contains(ec.Key)).ToDictionary(ec => categories.IndexOf(ec), ec => ec);
            foreach (var notOrderedEventCategory in notOrderedEventCategories)
            {
                configuration.EventOrder.Value.Insert(notOrderedEventCategory.Key, notOrderedEventCategory.Value.Key);
            }

            if (notOrderedEventCategories.Count > 0)
            {
                configuration.EventOrder.Value = new List<string>(configuration.EventOrder.Value);
            }
        }

        public new void RemoveDrawer(string name)
        {
            base.RemoveDrawer(name);

            this.DrawerSettings.UndefineSetting($"{name}-leftClickAction");
            this.DrawerSettings.UndefineSetting($"{name}-showTooltips");
            this.DrawerSettings.UndefineSetting($"{name}-timespan");
            this.DrawerSettings.UndefineSetting($"{name}-historySplit");
            this.DrawerSettings.UndefineSetting($"{name}-enableHistorySplitScrolling");
            this.DrawerSettings.UndefineSetting($"{name}-historySplitScrollingSpeed");
            this.DrawerSettings.UndefineSetting($"{name}-drawBorders");
            this.DrawerSettings.UndefineSetting($"{name}-useFillers");
            this.DrawerSettings.UndefineSetting($"{name}-fillerTextColor");
            this.DrawerSettings.UndefineSetting($"{name}-acceptWaypointPrompt");
            this.DrawerSettings.UndefineSetting($"{name}-completionAction");
            this.DrawerSettings.UndefineSetting($"{name}-disabledEventKeys");
            this.DrawerSettings.UndefineSetting($"{name}-eventHeight");
            this.DrawerSettings.UndefineSetting($"{name}-eventOrder");
            this.DrawerSettings.UndefineSetting($"{name}-eventBackgroundOpacity");
            this.DrawerSettings.UndefineSetting($"{name}-drawShadows");
            this.DrawerSettings.UndefineSetting($"{name}-shadowColor");
            this.DrawerSettings.UndefineSetting($"{name}-drawShadowsForFiller");
            this.DrawerSettings.UndefineSetting($"{name}-fillerShadowColor");
            this.DrawerSettings.UndefineSetting($"{name}-drawInterval");
            this.DrawerSettings.UndefineSetting($"{name}-limitToCurrentMap");
            this.DrawerSettings.UndefineSetting($"{name}-allowUnspecifiedMap");
            this.DrawerSettings.UndefineSetting($"{name}-timeLineOpacity");
            this.DrawerSettings.UndefineSetting($"{name}-eventTextOpacity");
            this.DrawerSettings.UndefineSetting($"{name}-fillerTextOpacity");
            this.DrawerSettings.UndefineSetting($"{name}-shadowOpacity");
            this.DrawerSettings.UndefineSetting($"{name}-fillerShadowOpacity");
            this.DrawerSettings.UndefineSetting($"{name}-completedEventsBackgroundOpacity");
            this.DrawerSettings.UndefineSetting($"{name}-completedEventsTextOpacity");
            this.DrawerSettings.UndefineSetting($"{name}-completedEventsInvertTextColor");
            this.DrawerSettings.UndefineSetting($"{name}-hideOnOpenMap");
            this.DrawerSettings.UndefineSetting($"{name}-hideOnMissingMumbleTicks");
            this.DrawerSettings.UndefineSetting($"{name}-hideInCombat");
            this.DrawerSettings.UndefineSetting($"{name}-hideInPvE_OpenWorld");
            this.DrawerSettings.UndefineSetting($"{name}-hideInPvE_Competetive");
            this.DrawerSettings.UndefineSetting($"{name}-hideInWvW");
            this.DrawerSettings.UndefineSetting($"{name}-hideInPvP");
            this.DrawerSettings.UndefineSetting($"{name}-showCategoryNames");
            this.DrawerSettings.UndefineSetting($"{name}-categoryNameColor");
            this.DrawerSettings.UndefineSetting($"{name}-enableColorGradients");
        }

        public override void UpdateLocalization(TranslationService translationService)
        {
            base.UpdateLocalization(translationService);

            var mapKeybindingDisplayNameDefault = this.MapKeybinding.DisplayName;
            var mapKeybindingDescriptionDefault = this.MapKeybinding.Description;
            this.MapKeybinding.GetDisplayNameFunc = () => translationService.GetTranslation("setting-mapKeybinding-name", mapKeybindingDisplayNameDefault);
            this.MapKeybinding.GetDescriptionFunc = () => translationService.GetTranslation("setting-mapKeybinding-description", mapKeybindingDescriptionDefault);

            var remindersEnabledDisplayNameDefault = this.RemindersEnabled.DisplayName;
            var remindersEnabledDescriptionDefault = this.RemindersEnabled.Description;
            this.RemindersEnabled.GetDisplayNameFunc = () => translationService.GetTranslation("setting-remindersEnabled-name", remindersEnabledDisplayNameDefault);
            this.RemindersEnabled.GetDescriptionFunc = () => translationService.GetTranslation("setting-remindersEnabled-description", remindersEnabledDescriptionDefault);

            var reminderPositionXDisplayNameDefault = this.ReminderPosition.X.DisplayName;
            var reminderPositionXDescriptionDefault = this.ReminderPosition.X.Description;
            this.ReminderPosition.X.GetDisplayNameFunc = () => translationService.GetTranslation("setting-reminderPositionX-name", reminderPositionXDisplayNameDefault);
            this.ReminderPosition.X.GetDescriptionFunc = () => translationService.GetTranslation("setting-reminderPositionX-description", reminderPositionXDescriptionDefault);

            var reminderPositionYDisplayNameDefault = this.ReminderPosition.Y.DisplayName;
            var reminderPositionYDescriptionDefault = this.ReminderPosition.Y.Description;
            this.ReminderPosition.Y.GetDisplayNameFunc = () => translationService.GetTranslation("setting-reminderPositionY-name", reminderPositionYDisplayNameDefault);
            this.ReminderPosition.Y.GetDescriptionFunc = () => translationService.GetTranslation("setting-reminderPositionY-description", reminderPositionYDescriptionDefault);

            var reminderDurationDisplayNameDefault = this.ReminderDuration.DisplayName;
            var reminderDurationDescriptionDefault = this.ReminderDuration.Description;
            this.ReminderDuration.GetDisplayNameFunc = () => translationService.GetTranslation("setting-reminderDuration-name", reminderDurationDisplayNameDefault);
            this.ReminderDuration.GetDescriptionFunc = () => translationService.GetTranslation("setting-reminderDuration-description", reminderDurationDescriptionDefault);

            var reminderOpacityDisplayNameDefault = this.ReminderOpacity.DisplayName;
            var reminderOpacityDescriptionDefault = this.ReminderOpacity.Description;
            this.ReminderOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-reminderOpacity-name", reminderOpacityDisplayNameDefault);
            this.ReminderOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-reminderOpacity-description", reminderOpacityDescriptionDefault);

            var showDynamicEventsOnMapDisplayNameDefault = this.ShowDynamicEventsOnMap.DisplayName;
            var showDynamicEventsOnMapDescriptionDefault = this.ShowDynamicEventsOnMap.Description;
            this.ShowDynamicEventsOnMap.GetDisplayNameFunc = () => translationService.GetTranslation("setting-showDynamicEventsOnMap-name", showDynamicEventsOnMapDisplayNameDefault);
            this.ShowDynamicEventsOnMap.GetDescriptionFunc = () => translationService.GetTranslation("setting-showDynamicEventsOnMap-description", showDynamicEventsOnMapDescriptionDefault);

            var showDynamicEventInWorldDisplayNameDefault = this.ShowDynamicEventInWorld.DisplayName;
            var showDynamicEventInWorldDescriptionDefault = this.ShowDynamicEventInWorld.Description;
            this.ShowDynamicEventInWorld.GetDisplayNameFunc = () => translationService.GetTranslation("setting-showDynamicEventInWorld-name", showDynamicEventInWorldDisplayNameDefault);
            this.ShowDynamicEventInWorld.GetDescriptionFunc = () => translationService.GetTranslation("setting-showDynamicEventInWorld-description", showDynamicEventInWorldDescriptionDefault);

            var showDynamicEventsInWorldOnlyWhenInsideDisplayNameDefault = this.ShowDynamicEventsInWorldOnlyWhenInside.DisplayName;
            var showDynamicEventsInWorldOnlyWhenInsideDescriptionDefault = this.ShowDynamicEventsInWorldOnlyWhenInside.Description;
            this.ShowDynamicEventsInWorldOnlyWhenInside.GetDisplayNameFunc = () => translationService.GetTranslation("setting-showDynamicEventsInWorldOnlyWhenInside-name", showDynamicEventsInWorldOnlyWhenInsideDisplayNameDefault);
            this.ShowDynamicEventsInWorldOnlyWhenInside.GetDescriptionFunc = () => translationService.GetTranslation("setting-showDynamicEventsInWorldOnlyWhenInside-description", showDynamicEventsInWorldOnlyWhenInsideDescriptionDefault);

            var ignoreZAxisOnDynamicEventsInWorldDisplayNameDefault = this.IgnoreZAxisOnDynamicEventsInWorld.DisplayName;
            var ignoreZAxisOnDynamicEventsInWorldDescriptionDefault = this.IgnoreZAxisOnDynamicEventsInWorld.Description;
            this.IgnoreZAxisOnDynamicEventsInWorld.GetDisplayNameFunc = () => translationService.GetTranslation("setting-ignoreZAxisOnDynamicEventsInWorld-name", ignoreZAxisOnDynamicEventsInWorldDisplayNameDefault);
            this.IgnoreZAxisOnDynamicEventsInWorld.GetDescriptionFunc = () => translationService.GetTranslation("setting-ignoreZAxisOnDynamicEventsInWorld-description", ignoreZAxisOnDynamicEventsInWorldDescriptionDefault);

            var dynamicEventsRenderDistanceDisplayNameDefault = this.DynamicEventsRenderDistance.DisplayName;
            var dynamicEventsRenderDistanceDescriptionDefault = this.DynamicEventsRenderDistance.Description;
            this.DynamicEventsRenderDistance.GetDisplayNameFunc = () => translationService.GetTranslation("setting-dynamicEventsRenderDistance-name", dynamicEventsRenderDistanceDisplayNameDefault);
            this.DynamicEventsRenderDistance.GetDescriptionFunc = () => translationService.GetTranslation("setting-dynamicEventsRenderDistance-description", dynamicEventsRenderDistanceDescriptionDefault);

            var menuEventSortModeDisplayNameDefault = this.MenuEventSortMode.DisplayName;
            var menuEventSortModeDescriptionDefault = this.MenuEventSortMode.Description;
            this.MenuEventSortMode.GetDisplayNameFunc = () => translationService.GetTranslation("setting-menuEventSortMode-name", menuEventSortModeDisplayNameDefault);
            this.MenuEventSortMode.GetDescriptionFunc = () => translationService.GetTranslation("setting-menuEventSortMode-description", menuEventSortModeDescriptionDefault);

            var hideRemindersOnOpenMapDisplayNameDefault = this.HideRemindersOnOpenMap.DisplayName;
            var hideRemindersOnOpenMapDescriptionDefault = this.HideRemindersOnOpenMap.Description;
            this.HideRemindersOnOpenMap.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersOnOpenMap-name", hideRemindersOnOpenMapDisplayNameDefault);
            this.HideRemindersOnOpenMap.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersOnOpenMap-description", hideRemindersOnOpenMapDescriptionDefault);

            var hideRemindersOnMissingMumbleTicksDisplayNameDefault = this.HideRemindersOnMissingMumbleTicks.DisplayName;
            var hideRemindersOnMissingMumbleTicksDescriptionDefault = this.HideRemindersOnMissingMumbleTicks.Description;
            this.HideRemindersOnMissingMumbleTicks.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersOnMissingMumbleTicks-name", hideRemindersOnMissingMumbleTicksDisplayNameDefault);
            this.HideRemindersOnMissingMumbleTicks.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersOnMissingMumbleTicks-description", hideRemindersOnMissingMumbleTicksDescriptionDefault);

            var hideRemindersInCombatDisplayNameDefault = this.HideRemindersInCombat.DisplayName;
            var hideRemindersInCombatDescriptionDefault = this.HideRemindersInCombat.Description;
            this.HideRemindersInCombat.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersInCombat-name", hideRemindersInCombatDisplayNameDefault);
            this.HideRemindersInCombat.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersInCombat-description", hideRemindersInCombatDescriptionDefault);

            var hideRemindersInPvE_OpenWorldDisplayNameDefault = this.HideRemindersInPvE_OpenWorld.DisplayName;
            var hideRemindersInPvE_OpenWorldDescriptionDefault = this.HideRemindersInPvE_OpenWorld.Description;
            this.HideRemindersInPvE_OpenWorld.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersInPvE_OpenWorld-name", hideRemindersInPvE_OpenWorldDisplayNameDefault);
            this.HideRemindersInPvE_OpenWorld.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersInPvE_OpenWorld-description", hideRemindersInPvE_OpenWorldDescriptionDefault);

            var hideRemindersInPvE_CompetetiveDisplayNameDefault = this.HideRemindersInPvE_Competetive.DisplayName;
            var hideRemindersInPvE_CompetetiveDescriptionDefault = this.HideRemindersInPvE_Competetive.Description;
            this.HideRemindersInPvE_Competetive.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersInPvE_Competetive-name", hideRemindersInPvE_CompetetiveDisplayNameDefault);
            this.HideRemindersInPvE_Competetive.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersInPvE_Competetive-description", hideRemindersInPvE_CompetetiveDescriptionDefault);

            var hideRemindersInWvWDisplayNameDefault = this.HideRemindersInWvW.DisplayName;
            var hideRemindersInWvWDescriptionDefault = this.HideRemindersInWvW.Description;
            this.HideRemindersInWvW.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersInWvW-name", hideRemindersInWvWDisplayNameDefault);
            this.HideRemindersInWvW.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersInWvW-description", hideRemindersInWvWDescriptionDefault);

            var hideRemindersInPvPDisplayNameDefault = this.HideRemindersInPvP.DisplayName;
            var hideRemindersInPvPDescriptionDefault = this.HideRemindersInPvP.Description;
            this.HideRemindersInPvP.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersInPvP-name", hideRemindersInPvPDisplayNameDefault);
            this.HideRemindersInPvP.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersInPvP-description", hideRemindersInPvPDescriptionDefault);
        }

        public void UpdateDrawerLocalization(EventAreaConfiguration drawerConfiguration, TranslationService translationService)
        {
            base.UpdateDrawerLocalization(drawerConfiguration, translationService);

            var leftClickActionDisplayNameDefault = drawerConfiguration.LeftClickAction.DisplayName;
            var leftClickActionDescriptionDefault = drawerConfiguration.LeftClickAction.Description;
            drawerConfiguration.LeftClickAction.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerLeftClickAction-name", leftClickActionDisplayNameDefault);
            drawerConfiguration.LeftClickAction.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerLeftClickAction-description", leftClickActionDescriptionDefault);

            var showTooltipsDisplayNameDefault = drawerConfiguration.ShowTooltips.DisplayName;
            var showTooltipsDescriptionDefault = drawerConfiguration.ShowTooltips.Description;
            drawerConfiguration.ShowTooltips.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerShowTooltips-name", showTooltipsDisplayNameDefault);
            drawerConfiguration.ShowTooltips.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerShowTooltips-description", showTooltipsDescriptionDefault);

            var timespanDisplayNameDefault = drawerConfiguration.TimeSpan.DisplayName;
            var timespanDescriptionDefault = drawerConfiguration.TimeSpan.Description;
            drawerConfiguration.TimeSpan.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerTimespan-name", timespanDisplayNameDefault);
            drawerConfiguration.TimeSpan.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerTimespan-description", timespanDescriptionDefault);

            var historySplitDisplayNameDefault = drawerConfiguration.HistorySplit.DisplayName;
            var historySplitDescriptionDefault = drawerConfiguration.HistorySplit.Description;
            drawerConfiguration.HistorySplit.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHistorySplit-name", historySplitDisplayNameDefault);
            drawerConfiguration.HistorySplit.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHistorySplit-description", historySplitDescriptionDefault);

            var enableHistorySplitScrollingDisplayNameDefault = drawerConfiguration.EnableHistorySplitScrolling.DisplayName;
            var enableHistorySplitScrollingDescriptionDefault = drawerConfiguration.EnableHistorySplitScrolling.Description;
            drawerConfiguration.EnableHistorySplitScrolling.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEnableHistorySplitScrolling-name", enableHistorySplitScrollingDisplayNameDefault);
            drawerConfiguration.EnableHistorySplitScrolling.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEnableHistorySplitScrolling-description", enableHistorySplitScrollingDescriptionDefault);

            var historySplitScrollingSpeedDisplayNameDefault = drawerConfiguration.HistorySplitScrollingSpeed.DisplayName;
            var historySplitScrollingSpeedDescriptionDefault = drawerConfiguration.HistorySplitScrollingSpeed.Description;
            drawerConfiguration.HistorySplitScrollingSpeed.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHistorySplitScrollingSpeed-name", historySplitScrollingSpeedDisplayNameDefault);
            drawerConfiguration.HistorySplitScrollingSpeed.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHistorySplitScrollingSpeed-description", historySplitScrollingSpeedDescriptionDefault);

            var drawBordersDisplayNameDefault = drawerConfiguration.DrawBorders.DisplayName;
            var drawBordersDescriptionDefault = drawerConfiguration.DrawBorders.Description;
            drawerConfiguration.DrawBorders.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerDrawBorders-name", drawBordersDisplayNameDefault);
            drawerConfiguration.DrawBorders.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerDrawBorders-description", drawBordersDescriptionDefault);

            var useFillersDisplayNameDefault = drawerConfiguration.UseFiller.DisplayName;
            var useFillersDescriptionDefault = drawerConfiguration.UseFiller.Description;
            drawerConfiguration.UseFiller.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerUseFillers-name", useFillersDisplayNameDefault);
            drawerConfiguration.UseFiller.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerUseFillers-description", useFillersDescriptionDefault);

            var fillerTextColorDisplayNameDefault = drawerConfiguration.FillerTextColor.DisplayName;
            var fillerTextColorDescriptionDefault = drawerConfiguration.FillerTextColor.Description;
            drawerConfiguration.FillerTextColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerFillerTextColor-name", fillerTextColorDisplayNameDefault);
            drawerConfiguration.FillerTextColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerFillerTextColor-description", fillerTextColorDescriptionDefault);

            var acceptWaypointPromptDisplayNameDefault = drawerConfiguration.AcceptWaypointPrompt.DisplayName;
            var acceptWaypointPromptDescriptionDefault = drawerConfiguration.AcceptWaypointPrompt.Description;
            drawerConfiguration.AcceptWaypointPrompt.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerAcceptWaypointPrompt-name", acceptWaypointPromptDisplayNameDefault);
            drawerConfiguration.AcceptWaypointPrompt.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerAcceptWaypointPrompt-description", acceptWaypointPromptDescriptionDefault);

            var completionActionDisplayNameDefault = drawerConfiguration.CompletionAction.DisplayName;
            var completionActionDescriptionDefault = drawerConfiguration.CompletionAction.Description;
            drawerConfiguration.CompletionAction.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerCompletionAction-name", completionActionDisplayNameDefault);
            drawerConfiguration.CompletionAction.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerCompletionAction-description", completionActionDescriptionDefault);

            var eventHeightDisplayNameDefault = drawerConfiguration.EventHeight.DisplayName;
            var eventHeightDescriptionDefault = drawerConfiguration.EventHeight.Description;
            drawerConfiguration.EventHeight.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEventHeight-name", eventHeightDisplayNameDefault);
            drawerConfiguration.EventHeight.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEventHeight-description", eventHeightDescriptionDefault);

            var eventBackgroundOpacityDisplayNameDefault = drawerConfiguration.EventBackgroundOpacity.DisplayName;
            var eventBackgroundOpacityDescriptionDefault = drawerConfiguration.EventBackgroundOpacity.Description;
            drawerConfiguration.EventBackgroundOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEventBackgroundOpacity-name", eventBackgroundOpacityDisplayNameDefault);
            drawerConfiguration.EventBackgroundOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEventBackgroundOpacity-description", eventBackgroundOpacityDescriptionDefault);

            var drawShadowsDisplayNameDefault = drawerConfiguration.DrawShadows.DisplayName;
            var drawShadowsDescriptionDefault = drawerConfiguration.DrawShadows.Description;
            drawerConfiguration.DrawShadows.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerDrawShadows-name", drawShadowsDisplayNameDefault);
            drawerConfiguration.DrawShadows.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerDrawShadows-description", drawShadowsDescriptionDefault);

            var shadowColorDisplayNameDefault = drawerConfiguration.ShadowColor.DisplayName;
            var shadowColorDescriptionDefault = drawerConfiguration.ShadowColor.Description;
            drawerConfiguration.ShadowColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerShadowColor-name", shadowColorDisplayNameDefault);
            drawerConfiguration.ShadowColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerShadowColor-description", shadowColorDescriptionDefault);

            var drawShadowsForFillerDisplayNameDefault = drawerConfiguration.DrawShadowsForFiller.DisplayName;
            var drawShadowsForFillerDescriptionDefault = drawerConfiguration.DrawShadowsForFiller.Description;
            drawerConfiguration.DrawShadowsForFiller.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerDrawShadowsForFiller-name", drawShadowsForFillerDisplayNameDefault);
            drawerConfiguration.DrawShadowsForFiller.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerDrawShadowsForFiller-description", drawShadowsForFillerDescriptionDefault);

            var fillerShadowColorDisplayNameDefault = drawerConfiguration.FillerShadowColor.DisplayName;
            var fillerShadowColorDescriptionDefault = drawerConfiguration.FillerShadowColor.Description;
            drawerConfiguration.FillerShadowColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerFillerShadowColor-name", fillerShadowColorDisplayNameDefault);
            drawerConfiguration.FillerShadowColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerFillerShadowColor-description", fillerShadowColorDescriptionDefault);

            var drawIntervalDisplayNameDefault = drawerConfiguration.DrawInterval.DisplayName;
            var drawIntervalDescriptionDefault = drawerConfiguration.DrawInterval.Description;
            drawerConfiguration.DrawInterval.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerDrawInterval-name", drawIntervalDisplayNameDefault);
            drawerConfiguration.DrawInterval.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerDrawInterval-description", drawIntervalDescriptionDefault);

            var limitToCurrentMapDisplayNameDefault = drawerConfiguration.LimitToCurrentMap.DisplayName;
            var limitToCurrentMapDescriptionDefault = drawerConfiguration.LimitToCurrentMap.Description;
            drawerConfiguration.LimitToCurrentMap.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerLimitToCurrentMap-name", limitToCurrentMapDisplayNameDefault);
            drawerConfiguration.LimitToCurrentMap.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerLimitToCurrentMap-description", limitToCurrentMapDescriptionDefault);

            var allowUnspecifiedMapDisplayNameDefault = drawerConfiguration.AllowUnspecifiedMap.DisplayName;
            var allowUnspecifiedMapDescriptionDefault = drawerConfiguration.AllowUnspecifiedMap.Description;
            drawerConfiguration.AllowUnspecifiedMap.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerAllowUnspecifiedMap-name", allowUnspecifiedMapDisplayNameDefault);
            drawerConfiguration.AllowUnspecifiedMap.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerAllowUnspecifiedMap-description", allowUnspecifiedMapDescriptionDefault);

            var timeLineOpacityDisplayNameDefault = drawerConfiguration.TimeLineOpacity.DisplayName;
            var timeLineOpacityDescriptionDefault = drawerConfiguration.TimeLineOpacity.Description;
            drawerConfiguration.TimeLineOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerTimeLineOpacity-name", timeLineOpacityDisplayNameDefault);
            drawerConfiguration.TimeLineOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerTimeLineOpacity-description", timeLineOpacityDescriptionDefault);

            var eventTextOpacityDisplayNameDefault = drawerConfiguration.EventTextOpacity.DisplayName;
            var eventTextOpacityDescriptionDefault = drawerConfiguration.EventTextOpacity.Description;
            drawerConfiguration.EventTextOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEventTextOpacity-name", eventTextOpacityDisplayNameDefault);
            drawerConfiguration.EventTextOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEventTextOpacity-description", eventTextOpacityDescriptionDefault);

            var fillerTextOpacityDisplayNameDefault = drawerConfiguration.FillerTextOpacity.DisplayName;
            var fillerTextOpacityDescriptionDefault = drawerConfiguration.FillerTextOpacity.Description;
            drawerConfiguration.FillerTextOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerFillerTextOpacity-name", fillerTextOpacityDisplayNameDefault);
            drawerConfiguration.FillerTextOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerFillerTextOpacity-description", fillerTextOpacityDescriptionDefault);

            var shadowOpacityDisplayNameDefault = drawerConfiguration.ShadowOpacity.DisplayName;
            var shadowOpacityDescriptionDefault = drawerConfiguration.ShadowOpacity.Description;
            drawerConfiguration.ShadowOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerShadowOpacity-name", shadowOpacityDisplayNameDefault);
            drawerConfiguration.ShadowOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerShadowOpacity-description", shadowOpacityDescriptionDefault);

            var fillerShadowOpacityDisplayNameDefault = drawerConfiguration.FillerShadowOpacity.DisplayName;
            var fillerShadowOpacityDescriptionDefault = drawerConfiguration.FillerShadowOpacity.Description;
            drawerConfiguration.FillerShadowOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerFillerShadowOpacity-name", fillerShadowOpacityDisplayNameDefault);
            drawerConfiguration.FillerShadowOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerFillerShadowOpacity-description", fillerShadowOpacityDescriptionDefault);

            var completedEventsBackgroundOpacityDisplayNameDefault = drawerConfiguration.CompletedEventsBackgroundOpacity.DisplayName;
            var completedEventsBackgroundOpacityDescriptionDefault = drawerConfiguration.CompletedEventsBackgroundOpacity.Description;
            drawerConfiguration.CompletedEventsBackgroundOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsBackgroundOpacity-name", completedEventsBackgroundOpacityDisplayNameDefault);
            drawerConfiguration.CompletedEventsBackgroundOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsBackgroundOpacity-description", completedEventsBackgroundOpacityDescriptionDefault);

            var completedEventsTextOpacityDisplayNameDefault = drawerConfiguration.CompletedEventsTextOpacity.DisplayName;
            var completedEventsTextOpacityDescriptionDefault = drawerConfiguration.CompletedEventsTextOpacity.Description;
            drawerConfiguration.CompletedEventsTextOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsTextOpacity-name", completedEventsTextOpacityDisplayNameDefault);
            drawerConfiguration.CompletedEventsTextOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsTextOpacity-description", completedEventsTextOpacityDescriptionDefault);

            var completedEventsInvertTextColorDisplayNameDefault = drawerConfiguration.CompletedEventsInvertTextColor.DisplayName;
            var completedEventsInvertTextColorDescriptionDefault = drawerConfiguration.CompletedEventsInvertTextColor.Description;
            drawerConfiguration.CompletedEventsInvertTextColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsInvertTextColor-name", completedEventsInvertTextColorDisplayNameDefault);
            drawerConfiguration.CompletedEventsInvertTextColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsInvertTextColor-description", completedEventsInvertTextColorDescriptionDefault);

            var hideOnOpenMapDisplayNameDefault = drawerConfiguration.HideOnOpenMap.DisplayName;
            var hideOnOpenMapDescriptionDefault = drawerConfiguration.HideOnOpenMap.Description;
            drawerConfiguration.HideOnOpenMap.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideOnOpenMap-name", hideOnOpenMapDisplayNameDefault);
            drawerConfiguration.HideOnOpenMap.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideOnOpenMap-description", hideOnOpenMapDescriptionDefault);

            var hideOnMissingMumbleTicksDisplayNameDefault = drawerConfiguration.HideOnMissingMumbleTicks.DisplayName;
            var hideOnMissingMumbleTicksDescriptionDefault = drawerConfiguration.HideOnMissingMumbleTicks.Description;
            drawerConfiguration.HideOnMissingMumbleTicks.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideOnMissingMumbleTicks-name", hideOnMissingMumbleTicksDisplayNameDefault);
            drawerConfiguration.HideOnMissingMumbleTicks.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideOnMissingMumbleTicks-description", hideOnMissingMumbleTicksDescriptionDefault);

            var hideInCombatDisplayNameDefault = drawerConfiguration.HideInCombat.DisplayName;
            var hideInCombatDescriptionDefault = drawerConfiguration.HideInCombat.Description;
            drawerConfiguration.HideInCombat.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideInCombat-name", hideInCombatDisplayNameDefault);
            drawerConfiguration.HideInCombat.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideInCombat-description", hideInCombatDescriptionDefault);

            var hideInPvE_OpenWorldDisplayNameDefault = drawerConfiguration.HideInPvE_OpenWorld.DisplayName;
            var hideInPvE_OpenWorldDescriptionDefault = drawerConfiguration.HideInPvE_OpenWorld.Description;
            drawerConfiguration.HideInPvE_OpenWorld.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideInPvE_OpenWorld-name", hideInPvE_OpenWorldDisplayNameDefault);
            drawerConfiguration.HideInPvE_OpenWorld.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideInPvE_OpenWorld-description", hideInPvE_OpenWorldDescriptionDefault);

            var hideInPvE_CompetetiveDisplayNameDefault = drawerConfiguration.HideInPvE_Competetive.DisplayName;
            var hideInPvE_CompetetiveDescriptionDefault = drawerConfiguration.HideInPvE_Competetive.Description;
            drawerConfiguration.HideInPvE_Competetive.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideInPvE_Competetive-name", hideInPvE_CompetetiveDisplayNameDefault);
            drawerConfiguration.HideInPvE_Competetive.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideInPvE_Competetive-description", hideInPvE_CompetetiveDescriptionDefault);

            var hideInWvWDisplayNameDefault = drawerConfiguration.HideInWvW.DisplayName;
            var hideInWvWDescriptionDefault = drawerConfiguration.HideInWvW.Description;
            drawerConfiguration.HideInWvW.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideInWvW-name", hideInWvWDisplayNameDefault);
            drawerConfiguration.HideInWvW.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideInWvW-description", hideInWvWDescriptionDefault);

            var hideInPvPDisplayNameDefault = drawerConfiguration.HideInPvP.DisplayName;
            var hideInPvPDescriptionDefault = drawerConfiguration.HideInPvP.Description;
            drawerConfiguration.HideInPvP.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideInPvP-name", hideInPvPDisplayNameDefault);
            drawerConfiguration.HideInPvP.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideInPvP-description", hideInPvPDescriptionDefault);

            var showCategoryNamesDisplayNameDefault = drawerConfiguration.ShowCategoryNames.DisplayName;
            var showCategoryNamesDescriptionDefault = drawerConfiguration.ShowCategoryNames.Description;
            drawerConfiguration.ShowCategoryNames.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerShowCategoryNames-name", showCategoryNamesDisplayNameDefault);
            drawerConfiguration.ShowCategoryNames.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerShowCategoryNames-description", showCategoryNamesDescriptionDefault);

            var categoryNameColorDisplayNameDefault = drawerConfiguration.CategoryNameColor.DisplayName;
            var categoryNameColorDescriptionDefault = drawerConfiguration.CategoryNameColor.Description;
            drawerConfiguration.CategoryNameColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerCategoryNameColor-name", categoryNameColorDisplayNameDefault);
            drawerConfiguration.CategoryNameColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerCategoryNameColor-description", categoryNameColorDescriptionDefault);

            var enableColorGradientsDisplayNameDefault = drawerConfiguration.EnableColorGradients.DisplayName;
            var enableColorGradientsDescriptionDefault = drawerConfiguration.EnableColorGradients.Description;
            drawerConfiguration.EnableColorGradients.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEnableColorGradients-name", enableColorGradientsDisplayNameDefault);
            drawerConfiguration.EnableColorGradients.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEnableColorGradients-description", enableColorGradientsDescriptionDefault);
        }

        public override void Unload()
        {
            base.Unload();
            this.ShowDynamicEventInWorld.SettingChanged -= this.ShowDynamicEventInWorld_SettingChanged;
            this.ShowDynamicEventsInWorldOnlyWhenInside.SettingChanged -= this.ShowDynamicEventsInWorldOnlyWhenInside_SettingChanged;
        }
    }
}
