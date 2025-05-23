﻿namespace Estreya.BlishHUD.EventTable;

using Blish_HUD;
using Blish_HUD.Input;
using Blish_HUD.Settings;
using Controls;
using Estreya.BlishHUD.EventTable.Models.Reminders;
using Estreya.BlishHUD.Shared.Extensions;
using Gw2Sharp.WebApi.V2.Models;
using Humanizer.Localisation;
using Microsoft.Xna.Framework.Input;
using Models;
using Shared.Models.Drawers;
using Shared.Services;
using Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

public class ModuleSettings : BaseModuleSettings
{
    private const string EVENT_AREA_SETTINGS = "event-area-settings";

    public const string ANY_AREA_NAME = "Any";

    public ModuleSettings(SettingCollection settings, SemVer.Version moduleVersion) : base(settings, moduleVersion, new KeyBinding()) { }

    private SettingCollection EventAreaSettings { get; set; }
    public SettingEntry<List<string>> EventAreaNames { get; private set; }

    public SettingEntry<KeyBinding> MapKeybinding { get; private set; }

    public SettingEntry<bool> RemindersEnabled { get; private set; }
    public EventReminderPosition ReminderPosition { get; private set; }
    public EventReminderSize ReminderSize { get; private set; }
    public EventReminderFonts ReminderFonts { get; private set; }
    public EventReminderColors ReminderColors { get; private set; }
    public SettingEntry<float> ReminderDuration { get; private set; }

    public SettingEntry<float> ReminderBackgroundOpacity { get; private set; }

    public SettingEntry<float> ReminderTitleOpacity { get; private set; }

    public SettingEntry<float> ReminderMessageOpacity { get; private set; }

    /// <summary>
    ///     Contains a list of event setting keys for which NO reminder should be displayed.
    /// </summary>
    public SettingEntry<List<string>> ReminderDisabledForEvents { get; private set; }

    public SettingEntry<Dictionary<string, List<TimeSpan>>> ReminderTimesOverride { get; private set; }

    public SettingEntry<LeftClickAction> ReminderLeftClickAction { get; private set; }
    public SettingEntry<EventReminderRightClickAction> ReminderRightClickAction { get; private set; }
    public SettingEntry<TimeUnit> ReminderMinTimeUnit { get; private set; }
    public SettingEntry<EventReminderStackDirection> ReminderStackDirection { get; private set; }
    public SettingEntry<EventReminderStackDirection> ReminderOverflowStackDirection { get; private set; }
    public SettingEntry<ReminderType> ReminderType { get; private set; }
    public SettingEntry<bool> DisableRemindersWhenEventFinished { get; private set; }
    public SettingEntry<string> DisableRemindersWhenEventFinishedArea { get; private set; }
    public SettingEntry<bool> AcceptWaypointPrompt { get; private set; }
    public SettingEntry<Shared.Models.GameIntegration.Chat.ChatChannel> ReminderWaypointSendingChannel { get; private set; }
    public SettingEntry<Shared.Models.GameIntegration.Guild.GuildNumber> ReminderWaypointSendingGuild { get; private set; }
    public SettingEntry<EventChatFormat> ReminderEventChatFormat { get; private set; }
    public SettingEntry<bool> ShowEventTimersOnMap { get; private set; }
    public SettingEntry<bool> ShowEventTimersInWorld { get; private set; }
    public SettingEntry<KeyBinding> ShowEventTimersOnMapKeybinding { get; private set; }
    public SettingEntry<KeyBinding> ShowEventTimersInWorldKeybinding { get; private set; }
    public SettingEntry<int> EventTimersRenderDistance { get; private set; }
    public SettingEntry<List<string>> DisabledEventTimerSettingKeys { get; private set; }
    public SettingEntry<Color> EventTimersRemainingTextColor { get; private set; }
    public SettingEntry<Color> EventTimersStartsInTextColor { get; private set; }
    public SettingEntry<Color> EventTimersNextOccurenceTextColor { get; private set; }
    public SettingEntry<Color> EventTimersNameTextColor { get; private set; }
    public SettingEntry<Color> EventTimersDurationTextColor { get; private set; }
    public SettingEntry<Color> EventTimersRepeatTextColor { get; private set; }

    public SettingEntry<bool> ShowDynamicEventsOnMap { get; private set; }

    public SettingEntry<bool> ShowDynamicEventInWorld { get; private set; }

    public SettingEntry<bool> ShowDynamicEventsInWorldOnlyWhenInside { get; private set; }

    public SettingEntry<bool> IgnoreZAxisOnDynamicEventsInWorld { get; private set; }

    public SettingEntry<int> DynamicEventsRenderDistance { get; private set; }

    public SettingEntry<List<string>> DisabledDynamicEventIds { get; private set; }

    public SettingEntry<KeyBinding> ShowDynamicEventsOnMapKeybinding { get; private set; }
    public SettingEntry<KeyBinding> ShowDynamicEventsInWorldKeybinding { get; private set; }

    public SettingEntry<MenuEventSortMode> MenuEventSortMode { get; private set; }

    public SettingEntry<bool> HideRemindersOnMissingMumbleTicks { get; private set; }
    public SettingEntry<bool> HideRemindersInCombat { get; private set; }
    public SettingEntry<bool> HideRemindersOnOpenMap { get; private set; }
    public SettingEntry<bool> HideRemindersInPvE_OpenWorld { get; private set; }
    public SettingEntry<bool> HideRemindersInPvE_Competetive { get; private set; }
    public SettingEntry<bool> HideRemindersInWvW { get; private set; }
    public SettingEntry<bool> HideRemindersInPvP { get; private set; }

    protected override void InitializeAdditionalSettings(SettingCollection settings)
    {
        this.EventAreaSettings = settings.AddSubCollection(EVENT_AREA_SETTINGS);

        this.EventAreaNames = this.EventAreaSettings.DefineSetting(nameof(this.EventAreaNames), new List<string>(), () => "Event Area Names", () => "Defines the event area names.");

        this.EventAreaSettings.AddLoggingEvents();
    }

    protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
    {
        this.RegisterContext.GetDisplayNameFunc = () => "Allow Cross Module Interaction";
        this.RegisterContext.GetDescriptionFunc = () => "Allow other BlishHUD modules to add certain elements to this module (events, reminders, ...). Requires a restart.";

        this.MapKeybinding = this.GlobalSettings.DefineSetting(nameof(this.MapKeybinding), new KeyBinding(Keys.M), () => "Open Map Hotkey", () => "Defines the key used to open the fullscreen map.");
        this.MapKeybinding.Value.Enabled = true;
        this.MapKeybinding.Value.BlockSequenceFromGw2 = false;

        this.RemindersEnabled = this.GlobalSettings.DefineSetting(nameof(this.RemindersEnabled), true, () => "Reminders Enabled", () => "Whether the module should display alerts before an event starts.");
        this.ReminderPosition = new EventReminderPosition
        {
            X = this.GlobalSettings.DefineSetting("ReminderPositionX", 200, () => "Location X", () => "Defines the position of reminders on the x axis."),
            Y = this.GlobalSettings.DefineSetting("ReminderPositionY", 200, () => "Location Y", () => "Defines the position of reminders on the y axis.")
        };

        this.ReminderSize = new EventReminderSize
        {
            X = this.GlobalSettings.DefineSetting("ReminderSizeX", 350, () => "Size X", () => "Defines the size of reminders on the x axis."),
            Y = this.GlobalSettings.DefineSetting("ReminderSizeY", 96, () => "Size Y", () => "Defines the size of reminders on the y axis."),
            Icon = this.GlobalSettings.DefineSetting("ReminderSizeIcon", 64, () => "Icon Width/Height", () => "Defines the size of the reminder icon on the x and y axis."),
        };

        this.ReminderFonts = new EventReminderFonts
        {
            TitleSize = this.GlobalSettings.DefineSetting("ReminderFontsTitleSize", ContentService.FontSize.Size18, () => "Title Font Size", () => "Defines the size the reminder title font."),
            MessageSize = this.GlobalSettings.DefineSetting("ReminderFontsMessageSize", ContentService.FontSize.Size16, () => "Message Font Size", () => "Defines the size the reminder message font."),
        };

        this.ReminderColors = new EventReminderColors
        {
            Background = this.GlobalSettings.DefineSetting("ReminderColorsBackground", this.DefaultGW2Color, () => "Background Color", () => "Defines the color in which the background is drawn."),
            TitleText = this.GlobalSettings.DefineSetting("ReminderColorsTitleText", this.DefaultGW2Color, () => "Title Text Color", () => "Defines the text color in which the titles are drawn."),
            MessageText = this.GlobalSettings.DefineSetting("ReminderColorsMessageText", this.DefaultGW2Color, () => "Message Text Color", () => "Defines the text color in which the messages are drawn.")
        };

        int reminderDurationMin = 1;
        int reminderDurationMax = 30;
        this.ReminderDuration = this.GlobalSettings.DefineSetting(nameof(this.ReminderDuration), 5f, () => "Reminder Duration", () => "Defines the reminder duration.");
        this.ReminderDuration.SetRange(reminderDurationMin, reminderDurationMax);

        this.ReminderDisabledForEvents = this.GlobalSettings.DefineSetting(nameof(this.ReminderDisabledForEvents), new List<string>(), () => "Reminder disabled for Events", () => "Defines the events for which NO reminder should be displayed.");

        this.ReminderTimesOverride = this.GlobalSettings.DefineSetting(nameof(this.ReminderTimesOverride), new Dictionary<string, List<TimeSpan>>(), () => "Reminder Times Override", () => "Defines the overridden times for reminders per event.");

        this.ReminderBackgroundOpacity = this.GlobalSettings.DefineSetting(nameof(this.ReminderBackgroundOpacity), 0.5f, () => "Reminder Background Opacity", () => "Defines the background opacity for reminders.");
        this.ReminderBackgroundOpacity.SetRange(0.1f, 1f);

        this.ReminderTitleOpacity = this.GlobalSettings.DefineSetting(nameof(this.ReminderTitleOpacity), 1f, () => "Reminder Title Opacity", () => "Defines the title opacity for reminders.");
        this.ReminderTitleOpacity.SetRange(0.1f, 1f);

        this.ReminderMessageOpacity = this.GlobalSettings.DefineSetting(nameof(this.ReminderMessageOpacity), 1f, () => "Reminder Message Opacity", () => "Defines the message opacity for reminders.");
        this.ReminderMessageOpacity.SetRange(0.1f, 1f);

        this.ReminderLeftClickAction = this.GlobalSettings.DefineSetting(nameof(this.ReminderLeftClickAction), LeftClickAction.CopyWaypoint, () => "Reminder Left Click Action", () => "Defines the action to execute on a left click.");
        this.ReminderRightClickAction = this.GlobalSettings.DefineSetting(nameof(this.ReminderRightClickAction), EventReminderRightClickAction.Dismiss, () => "Reminder Right Click Action", () => "Defines the action to execute on a right click.");

        this.ReminderStackDirection = this.GlobalSettings.DefineSetting(nameof(this.ReminderStackDirection), Models.Reminders.EventReminderStackDirection.Down, () => "Reminder Stack Direction", () => "Defines the direction in which reminders stack.");
        this.ReminderOverflowStackDirection = this.GlobalSettings.DefineSetting(nameof(this.ReminderOverflowStackDirection), Models.Reminders.EventReminderStackDirection.Right, () => "Reminder Overflow Stack Direction", () => "Defines the direction in which reminders stack if they overflow off the screen.");

        this.ReminderMinTimeUnit = this.GlobalSettings.DefineSetting(nameof(this.ReminderMinTimeUnit), TimeUnit.Second, () => "Reminder min. Timeunit", () => "Defines what min. timeunit should be used for reminders.");
        this.ReminderMinTimeUnit.SetIncluded(TimeUnit.Second, TimeUnit.Minute, TimeUnit.Hour);

        this.ReminderType = this.GlobalSettings.DefineSetting(nameof(this.ReminderType), Models.Reminders.ReminderType.Control, () => "Reminder Type", () => "Defines what type the reminder should be displayed as.");

        this.DisableRemindersWhenEventFinished = this.GlobalSettings.DefineSetting(nameof(this.DisableRemindersWhenEventFinished), false, () => "Disable Reminders for finished Events", () => "Disabled the reminders for events with are either completed or hidden.");
        this.DisableRemindersWhenEventFinishedArea = this.GlobalSettings.DefineSetting(nameof(this.DisableRemindersWhenEventFinishedArea), ANY_AREA_NAME, () => "Check finished Events in Area", () => "Defines the area name which is checked for completed/finished events");

        this.AcceptWaypointPrompt = this.GlobalSettings.DefineSetting(nameof(this.AcceptWaypointPrompt), true, () => "Accept Waypoint Prompt", () => "Defines if the waypoint prompt should be auto accepted");

        this.ReminderWaypointSendingChannel = this.GlobalSettings.DefineSetting(nameof(this.ReminderWaypointSendingChannel), Shared.Models.GameIntegration.Chat.ChatChannel.Private, () => "Send Waypoint to Channel", () => "Defines the channel in which waypoints are pasted automatically.");
        this.ReminderWaypointSendingGuild = this.GlobalSettings.DefineSetting(nameof(this.ReminderWaypointSendingGuild), Shared.Models.GameIntegration.Guild.GuildNumber.Guild_1, () => "Send Waypoint to Guild", () => "Defines the guild in which waypoints are pasted automatically if guild is selected as channel.");
        this.ReminderEventChatFormat = this.GlobalSettings.DefineSetting(nameof(this.ReminderEventChatFormat), EventChatFormat.OnlyWaypoint, () => "Event Chat Format", () => "Defines the chat format to use when copying events.");

        this.ShowEventTimersOnMap = this.GlobalSettings.DefineSetting(nameof(this.ShowEventTimersOnMap), true, () => "Show Event Timers on Map", () => "Whether the event timers should be shown on the map.");
        this.ShowEventTimersInWorld = this.GlobalSettings.DefineSetting(nameof(this.ShowEventTimersInWorld), true, () => "Show Event Timers in World", () => "Whether event timers should be shown inside the world.");

        this.ShowEventTimersOnMapKeybinding = this.GlobalSettings.DefineSetting(nameof(this.ShowEventTimersOnMapKeybinding), new KeyBinding(), () => "Show on Map Keybinding", () => "Defines the key used to toggle the on map event timers.");
        this.ShowEventTimersOnMapKeybinding.Value.Enabled = true;
        this.ShowEventTimersOnMapKeybinding.Value.BlockSequenceFromGw2 = true;
        this.ShowEventTimersOnMapKeybinding.Value.Activated += this.ShowEventTimersOnMapKeybinding_Activated;

        this.ShowEventTimersInWorldKeybinding = this.GlobalSettings.DefineSetting(nameof(this.ShowEventTimersInWorldKeybinding), new KeyBinding(), () => "Show in World Keybinding", () => "Defines the key used to toggle the in world event timers.");
        this.ShowEventTimersInWorldKeybinding.Value.Enabled = true;
        this.ShowEventTimersInWorldKeybinding.Value.BlockSequenceFromGw2 = true;
        this.ShowEventTimersInWorldKeybinding.Value.Activated += this.ShowEventTimersInWorldKeybinding_Activated;

        this.EventTimersRenderDistance = this.GlobalSettings.DefineSetting(nameof(this.EventTimersRenderDistance), 75, () => "Event Timer Render Distance", () => "Defines the max render distance for in-world event timers.");
        this.EventTimersRenderDistance.SetRange(25, 500);

        this.DisabledEventTimerSettingKeys = this.GlobalSettings.DefineSetting(nameof(this.DisabledEventTimerSettingKeys), new List<string>(), () => "Disabled Event Timers", () => "Defines which event timers are disabled.");

        this.EventTimersRemainingTextColor = this.GlobalSettings.DefineSetting(nameof(this.EventTimersRemainingTextColor), this.DefaultGW2Color, () => "Remaining Text Color", () => "Defines the text color of the remaining section.");
        this.EventTimersStartsInTextColor = this.GlobalSettings.DefineSetting(nameof(this.EventTimersStartsInTextColor), this.DefaultGW2Color, () => "Starts in Text Color", () => "Defines the text color of the starts in section.");
        this.EventTimersRepeatTextColor = this.GlobalSettings.DefineSetting(nameof(this.EventTimersRepeatTextColor), this.DefaultGW2Color, () => "Repeat Text Color", () => "Defines the text color of the repeat section.");
        this.EventTimersDurationTextColor = this.GlobalSettings.DefineSetting(nameof(this.EventTimersDurationTextColor), this.DefaultGW2Color, () => "Duration Text Color", () => "Defines the text color of the duration section.");
        this.EventTimersNameTextColor = this.GlobalSettings.DefineSetting(nameof(this.EventTimersNameTextColor), this.DefaultGW2Color, () => "Name Text Color", () => "Defines the text color of the name section.");
        this.EventTimersNextOccurenceTextColor = this.GlobalSettings.DefineSetting(nameof(this.EventTimersNextOccurenceTextColor), this.DefaultGW2Color, () => "Next Occurence Text Color", () => "Defines the text color of the next occurence section.");

        this.ShowDynamicEventsOnMap = this.GlobalSettings.DefineSetting(nameof(this.ShowDynamicEventsOnMap), false, () => "Show Dynamic Events on Map", () => "Whether the dynamic events of the map should be shown.");

        this.ShowDynamicEventInWorld = this.GlobalSettings.DefineSetting(nameof(this.ShowDynamicEventInWorld), false, () => "Show Dynamic Events in World", () => "Whether dynamic events should be shown inside the world.");
        this.ShowDynamicEventInWorld.SettingChanged += this.ShowDynamicEventInWorld_SettingChanged;

        this.ShowDynamicEventsInWorldOnlyWhenInside = this.GlobalSettings.DefineSetting(nameof(this.ShowDynamicEventsInWorldOnlyWhenInside), true, () => "Show only when inside", () => "Whether the dynamic events inside the world should only show up when the player is inside.");
        this.ShowDynamicEventsInWorldOnlyWhenInside.SettingChanged += this.ShowDynamicEventsInWorldOnlyWhenInside_SettingChanged;

        this.IgnoreZAxisOnDynamicEventsInWorld = this.GlobalSettings.DefineSetting(nameof(this.IgnoreZAxisOnDynamicEventsInWorld), true, () => "Ignore Z Axis", () => "Defines whether the z axis should be ignored when calculating the visibility of in world events.");

        this.DynamicEventsRenderDistance = this.GlobalSettings.DefineSetting(nameof(this.DynamicEventsRenderDistance), 300, () => "Dynamic Event Render Distance", () => "Defines the distance in which dynamic events should be rendered.");
        this.DynamicEventsRenderDistance.SetRange(50, 500);

        this.DisabledDynamicEventIds = this.GlobalSettings.DefineSetting(nameof(this.DisabledDynamicEventIds), new List<string>(), () => "Disabled Dynamic Events", () => "Defines which dynamic events are disabled.");

        this.ShowDynamicEventsOnMapKeybinding = this.GlobalSettings.DefineSetting(nameof(this.ShowDynamicEventsOnMapKeybinding), new KeyBinding(), () => "Show on Map Keybinding", () => "Defines the key used to toggle the on map dynamic events.");
        this.ShowDynamicEventsOnMapKeybinding.Value.Enabled = true;
        this.ShowDynamicEventsOnMapKeybinding.Value.BlockSequenceFromGw2 = true;
        this.ShowDynamicEventsOnMapKeybinding.Value.Activated += this.ShowDynamicEventsOnMapKeybinding_Activated;

        this.ShowDynamicEventsInWorldKeybinding = this.GlobalSettings.DefineSetting(nameof(this.ShowDynamicEventsInWorldKeybinding), new KeyBinding(), () => "Show in World Keybinding", () => "Defines the key used to toggle the in world dynamic events.");
        this.ShowDynamicEventsInWorldKeybinding.Value.Enabled = true;
        this.ShowDynamicEventsInWorldKeybinding.Value.BlockSequenceFromGw2 = true;
        this.ShowDynamicEventsInWorldKeybinding.Value.Activated += this.ShowDynamicEventInWorldKeybinding_Activated;

        this.MenuEventSortMode = this.GlobalSettings.DefineSetting(nameof(this.MenuEventSortMode), Models.MenuEventSortMode.Default, () => "Menu Event Sort Mode", () => "Defines the mode by which the events are sorted by when displayed inside different settings windows/tabs. (e.g. Event Areas -> Manage Events)");

        this.HideRemindersOnOpenMap = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersOnOpenMap), false, () => "Hide Reminders on open Map", () => "Whether the reminders should hide when the map is open.");

        this.HideRemindersOnMissingMumbleTicks = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersOnMissingMumbleTicks), true, () => "Hide Reminders on Cutscenes", () => "Whether the reminders should hide when cutscenes are played.");

        this.HideRemindersInCombat = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersInCombat), false, () => "Hide Reminders in Combat", () => "Whether the reminders should hide when in combat.");

        this.HideRemindersInPvE_OpenWorld = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersInPvE_OpenWorld), false, () => "Hide Reminders in PvE (Open World)", () => "Whether the reminders should hide when in PvE (Open World).");

        this.HideRemindersInPvE_Competetive = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersInPvE_Competetive), false, () => "Hide Reminders in PvE (Competetive)", () => "Whether the reminders should hide when in PvE (Competetive).");

        this.HideRemindersInWvW = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersInWvW), false, () => "Hide Reminders in WvW", () => "Whether the reminders should hide when in world vs. world.");

        this.HideRemindersInPvP = this.GlobalSettings.DefineSetting(nameof(this.HideRemindersInPvP), false, () => "Hide Reminders in PvP", () => "Whether the reminders should hide when in player vs. player.");

        this.HandleEnabledStates();
    }

    private void ShowEventTimersInWorldKeybinding_Activated(object sender, EventArgs e)
    {
        this.ShowEventTimersInWorld.Value = !this.ShowEventTimersInWorld.Value;
    }

    private void ShowEventTimersOnMapKeybinding_Activated(object sender, EventArgs e)
    {
        this.ShowEventTimersOnMap.Value = !this.ShowEventTimersOnMap.Value;
    }

    private void ShowDynamicEventInWorldKeybinding_Activated(object sender, EventArgs e)
    {
        this.ShowDynamicEventInWorld.Value = !this.ShowDynamicEventInWorld.Value;
    }

    private void ShowDynamicEventsOnMapKeybinding_Activated(object sender, EventArgs e)
    {
       this.ShowDynamicEventsOnMap.Value = !this.ShowDynamicEventsOnMap.Value;
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

        if (!this.IsMaxResolutionValid(maxResX, maxResY))
        {
            this.Logger.Warn($"Max Global size and position resolution is invalid. X: {maxResX} - Y: {maxResY}");
            return;
        }

        int minLocationX = 0;
        int maxLocationX = maxResX - this.ReminderSize.X.Value;
        int minLocationY = 0;
        int maxLocationY = maxResY - this.ReminderSize.Y.Value;
        int minWidth = 0;
        int maxWidth = maxResX - this.ReminderPosition.X.Value;
        int minHeight = 0;
        int maxHeight = maxResY - this.ReminderPosition.Y.Value;

        this.ReminderPosition?.X.SetRange(minLocationX, maxLocationX);
        this.ReminderPosition?.Y.SetRange(minLocationY, maxLocationY);
        this.ReminderSize?.X.SetRange(minWidth, maxWidth);
        this.ReminderSize?.Y.SetRange(minHeight, maxHeight);
        this.ReminderSize?.Icon.SetRange(0, this.ReminderSize.Y.Value);
    }

    public EventAreaConfiguration AddDrawer(string name, List<EventCategory> eventCategories, KeyBinding enabledKeybinding = null)
    {
        DrawerConfiguration drawer = base.AddDrawer(name, enabledKeybindingDefault: enabledKeybinding);

        SettingEntry<LeftClickAction> leftClickAction = this.DrawerSettings.DefineSetting($"{name}-leftClickAction", LeftClickAction.CopyWaypoint, () => "Left Click Action", () => "Defines the action which is executed when left clicking.");
        SettingEntry<bool> showTooltips = this.DrawerSettings.DefineSetting($"{name}-showTooltips", true, () => "Show Tooltips", () => "Whether a tooltip should be displayed when hovering.");

        SettingEntry<int> timespan = this.DrawerSettings.DefineSetting($"{name}-timespan", 120, () => "Timespan", () => "Defines the timespan the event drawer covers.");
        timespan.SetRange(60, 240);

        SettingEntry<int> historySplit = this.DrawerSettings.DefineSetting($"{name}-historySplit", 50, () => "History Split", () => "Defines how much history the timespan should contain.");
        historySplit.SetRange(0, 75);
        SettingEntry<bool> enableHistorySplitScrolling = this.DrawerSettings.DefineSetting($"{name}-enableHistorySplitScrolling", false, () => "Enable History Split Scrolling", () => "Defines if scrolling inside the event area temporary moves the history split until the mouse leaves the area.");

        SettingEntry<int> historySplitScrollingSpeed = this.DrawerSettings.DefineSetting($"{name}-historySplitScrollingSpeed", 1, () => "History Split Scrolling Speed", () => "Defines the speed when scrolling inside the event area.");
        historySplitScrollingSpeed.SetRange(1, 10);

        SettingEntry<bool> drawBorders = this.DrawerSettings.DefineSetting($"{name}-drawBorders", false, () => "Draw Borders", () => "Whether the events should be rendered with borders.");
        SettingEntry<bool> useFillers = this.DrawerSettings.DefineSetting($"{name}-useFillers", true, () => "Use Filler Events", () => "Whether the empty spaces should be filled by filler events.");
        SettingEntry<Color> fillerTextColor = this.DrawerSettings.DefineSetting($"{name}-fillerTextColor", this.DefaultGW2Color, () => "Filler Text Color", () => "Defines the text color used by filler events.");

        SettingEntry<bool> acceptWaypointPrompt = this.DrawerSettings.DefineSetting($"{name}-acceptWaypointPrompt", true, () => "Accept Waypoint Prompt", () => "Whether the waypoint prompt should be accepted automatically when performing an automated teleport.");
        SettingEntry<bool> hideAfterWaypointNavigation = this.DrawerSettings.DefineSetting($"{name}-hideAfterWaypointNavigation", false, () => "Hide after Waypoint Navigation", () => "If the area should be hidden after a successfull waypoint navigation.");

        SettingEntry<Shared.Models.GameIntegration.Chat.ChatChannel> waypointSendingChannel = this.DrawerSettings.DefineSetting($"{name}-waypointSendingChannel", Shared.Models.GameIntegration.Chat.ChatChannel.Private, () => "Send Waypoint to Channel", () => "Defines the channel in which the waypoint is pasted automatically.");
        SettingEntry<Shared.Models.GameIntegration.Guild.GuildNumber> waypointSendingGuild = this.DrawerSettings.DefineSetting($"{name}-waypointSendingGuild", Shared.Models.GameIntegration.Guild.GuildNumber.Guild_1, () => "Send Waypoint to Guild", () => "Defines the guild in which the waypoint is pasted automatically if channel guild is selected.");
        SettingEntry<EventChatFormat> eventChatFormat = this.DrawerSettings.DefineSetting($"{name}-eventChatFormat", EventChatFormat.OnlyWaypoint, () => "Event Chat Format", () => "Defines the chat format when event waypoints are copied or pasted.");

        SettingEntry<EventCompletedAction> completionAction = this.DrawerSettings.DefineSetting($"{name}-completionAction", EventCompletedAction.Crossout, () => "Completion Action", () => "Defines the action to perform if an event has been completed.");
        SettingEntry<bool> enableLinkedCompletion = this.DrawerSettings.DefineSetting($"{name}-enableLinkedCompletion", true, () => "Enable Linked Completion", () => "Enables the completion of events that are linked to the completed event. (e.g. Auric Basin)");

        SettingEntry<List<string>> disabledEventKeys = this.DrawerSettings.DefineSetting($"{name}-disabledEventKeys", new List<string>(), () => "Active Event Keys", () => "Defines the active event keys.");

        SettingEntry<int> eventHeight = this.DrawerSettings.DefineSetting($"{name}-eventHeight", 30, () => "Event Height", () => "Defines the height of the individual event rows.");
        eventHeight.SetRange(5, 70);

        SettingEntry<List<string>> eventOrder = this.DrawerSettings.DefineSetting($"{name}-eventOrder", new List<string>(eventCategories.Select(x => x.Key)), () => "Event Order", () => "Defines the order of events.");

        SettingEntry<float> eventBackgroundOpacity = this.DrawerSettings.DefineSetting($"{name}-eventBackgroundOpacity", 1f, () => "Event Background Opacity", () => "Defines the opacity of the individual event backgrounds.");
        eventBackgroundOpacity.SetRange(0.1f, 1f);

        SettingEntry<bool> drawShadows = this.DrawerSettings.DefineSetting($"{name}-drawShadows", false, () => "Draw Shadows", () => "Whether the text should have shadows");

        SettingEntry<Color> shadowColor = this.DrawerSettings.DefineSetting($"{name}-shadowColor", this.DefaultGW2Color, () => "Shadow Color", () => "Defines the color of the shadows");

        SettingEntry<bool> drawShadowsForFiller = this.DrawerSettings.DefineSetting($"{name}-drawShadowsForFiller", false, () => "Draw Shadows for Filler", () => "Whether the filler text should have shadows");

        SettingEntry<Color> fillerShadowColor = this.DrawerSettings.DefineSetting($"{name}-fillerShadowColor", this.DefaultGW2Color, () => "Filler Shadow Color", () => "Defines the color of the shadows for fillers");

        SettingEntry<DrawInterval> drawInterval = this.DrawerSettings.DefineSetting($"{name}-drawInterval", DrawInterval.FAST, () => "Draw Interval", () => "Defines the refresh rate of the drawer.");

        SettingEntry<bool> limitToCurrentMap = this.DrawerSettings.DefineSetting($"{name}-limitToCurrentMap", false, () => "Limit to current Map", () => "Whether the drawer should only show events from the current map.");

        SettingEntry<bool> allowUnspecifiedMap = this.DrawerSettings.DefineSetting($"{name}-allowUnspecifiedMap", true, () => "Allow from unspecified Maps", () => "Whether the table should show events which do not have a map id specified.");

        SettingEntry<float> timeLineOpacity = this.DrawerSettings.DefineSetting($"{name}-timeLineOpacity", 1f, () => "Timeline Opacity", () => "Defines the opacity of the time line bar.");
        timeLineOpacity.SetRange(0.1f, 1f);

        SettingEntry<float> eventTextOpacity = this.DrawerSettings.DefineSetting($"{name}-eventTextOpacity", 1f, () => "Event Text Opacity", () => "Defines the opacity of the event text.");
        eventTextOpacity.SetRange(0.1f, 1f);

        SettingEntry<float> fillerTextOpacity = this.DrawerSettings.DefineSetting($"{name}-fillerTextOpacity", 1f, () => "Filler Text Opacity", () => "Defines the opacity of filler event text.");
        fillerTextOpacity.SetRange(0.1f, 1f);

        SettingEntry<float> shadowOpacity = this.DrawerSettings.DefineSetting($"{name}-shadowOpacity", 1f, () => "Shadow Opacity", () => "Defines the opacity for shadows.");
        shadowOpacity.SetRange(0.1f, 1f);

        SettingEntry<float> fillerShadowOpacity = this.DrawerSettings.DefineSetting($"{name}-fillerShadowOpacity", 1f, () => "Filler Shadow Opacity", () => "Defines the opacity for filler shadows.");
        fillerShadowOpacity.SetRange(0.1f, 1f);

        SettingEntry<float> completedEventsBackgroundOpacity = this.DrawerSettings.DefineSetting($"{name}-completedEventsBackgroundOpacity", 0.5f, () => "Completed Events Background Opacity", () => "Defines the background opacity of completed events. Only works in combination with CompletionAction = Change Opacity");
        completedEventsBackgroundOpacity.SetRange(0.1f, 0.9f);

        SettingEntry<float> completedEventsTextOpacity = this.DrawerSettings.DefineSetting($"{name}-completedEventsTextOpacity", 1f, () => "Completed Events Text Opacity", () => "Defines the text opacity of completed events. Only works in combination with CompletionAction = Change Opacity");
        completedEventsBackgroundOpacity.SetRange(0f, 1f);

        SettingEntry<bool> completedEventsInvertTextColor = this.DrawerSettings.DefineSetting($"{name}-completedEventsInvertTextColor", true, () => "Completed Events Invert Textcolor", () => "Specified if completed events should have their text color inverted. Only works in combination with CompletionAction = Change Opacity");

        SettingEntry<bool> hideOnOpenMap = this.DrawerSettings.DefineSetting($"{name}-hideOnOpenMap", true, () => "Hide on open Map", () => "Whether the area should hide when the map is open.");

        SettingEntry<bool> hideOnMissingMumbleTicks = this.DrawerSettings.DefineSetting($"{name}-hideOnMissingMumbleTicks", true, () => "Hide on Cutscenes", () => "Whether the area should hide when cutscenes are played.");

        SettingEntry<bool> hideInCombat = this.DrawerSettings.DefineSetting($"{name}-hideInCombat", false, () => "Hide in Combat", () => "Whether the area should hide when in combat.");

        SettingEntry<bool> hideInPvE_OpenWorld = this.DrawerSettings.DefineSetting($"{name}-hideInPvE_OpenWorld", false, () => "Hide in PvE (Open World)", () => "Whether the area should hide when in PvE (Open World).");

        SettingEntry<bool> hideInPvE_Competetive = this.DrawerSettings.DefineSetting($"{name}-hideInPvE_Competetive", false, () => "Hide in PvE (Competetive)", () => "Whether the area should hide when in PvE (Competetive).");

        SettingEntry<bool> hideInWvW = this.DrawerSettings.DefineSetting($"{name}-hideInWvW", false, () => "Hide in WvW", () => "Whether the area should hide when in world vs. world.");

        SettingEntry<bool> hideInPvP = this.DrawerSettings.DefineSetting($"{name}-hideInPvP", false, () => "Hide in PvP", () => "Whether the area should hide when in player vs. player.");

        SettingEntry<bool> showCategoryNames = this.DrawerSettings.DefineSetting($"{name}-showCategoryNames", false, () => "Show Category Names", () => "Defines if the category names should be shown before the event bars.");

        SettingEntry<Color> categoryNameColor = this.DrawerSettings.DefineSetting($"{name}-categoryNameColor", this.DefaultGW2Color, () => "Category Name Color", () => "Defines the color of the category names.");

        SettingEntry<bool> enableColorGradients = this.DrawerSettings.DefineSetting($"{name}-enableColorGradients", false, () => "Enable Color Gradients", () => "Defines if supported events should have a smoother color gradient from and to the next event.");

        SettingEntry<string> eventTimespanDaysFormatString = this.DrawerSettings.DefineSetting($"{name}-eventTimespanDaysFormatString", "DD\\.hh\\:mm\\:ss", () => "Days Format String", () => "Defines the format strings for timespans over 1 day.");
        SettingEntry<string> eventTimespanHoursFormatString = this.DrawerSettings.DefineSetting($"{name}-eventTimespanHoursFormatString", "hh\\:mm\\:ss", () => "Hours Format String", () => "Defines the format strings for timespans over 1 hours.");
        SettingEntry<string> eventTimespanMinutesFormatString = this.DrawerSettings.DefineSetting($"{name}-eventTimespanMinutesFormatString", "mm\\:ss", () => "Minutes Format String", () => "Defines the fallback format strings for timespans.");

        SettingEntry<string> eventAbsoluteTimeFormatString = this.DrawerSettings.DefineSetting($"{name}-eventAbsoluteTimeFormatString", "HH\\:mm", () => "Absolute Time Format String", () => "Defines the format strings for absolute time.");

        var showTopTimeline = this.DrawerSettings.DefineSetting($"{name}-showTopTimeline", false, () => "Show Top Timeline", () => "Defines whether the top timeline is visible.");
        var topTimelineTimeFormatString = this.DrawerSettings.DefineSetting($"{name}-topTimelineTimeFormatString", "HH\\:mm", () => "Top Timeline Time Format String", () => "Defines the format strings for absolute time.");
        var topTimelineBackgroundColor = this.DrawerSettings.DefineSetting($"{name}-topTimelineBackgroundColor", this.DefaultGW2Color, () => "Top Timeline Background Color", () => "Defines the background color of the top timeline.");
        var topTimelineLineColor = this.DrawerSettings.DefineSetting($"{name}-topTimelineLineColor", this.DefaultGW2Color, () => "Top Timeline Line Color", () => "Defines the line color of the top timeline.");
        var topTimelineTimeColor = this.DrawerSettings.DefineSetting($"{name}-topTimelineTimeColor", this.DefaultGW2Color, () => "Top Timeline Time Color", () => "Defines the time color of the top timeline.");
        var topTimelineBackgroundOpacity = this.DrawerSettings.DefineSetting($"{name}-topTimelineBackgroundOpacity", 1f, () => "Top Timeline Background Opacity", () => "Defines the background color opacity of the top timeline.");
        var topTimelineLineOpacity = this.DrawerSettings.DefineSetting($"{name}-topTimelineLineOpacity", 1f, () => "Top Timeline Line Opacity", () => "Defines the line color opacity of the top timeline.");
        var topTimelineTimeOpacity = this.DrawerSettings.DefineSetting($"{name}-topTimelineTimeOpacity", 1f, () => "Top Timeline Time Opacity", () => "Defines the time color opacity of the top timeline.");
        var topTimelineLinesOverWholeHeight = this.DrawerSettings.DefineSetting($"{name}-topTimelineLinesOverWholeHeight", false, () => "Top Timeline Lines Over Whole Height", () => "Defines if the top timeline lines should cover the whole event area height.");
        var topTimelineLinesInBackground = this.DrawerSettings.DefineSetting($"{name}-topTimelineLinesInBackground", true, () => "Top Timeline Lines in Background", () => "Defines if the top timeline lines should be in the background or foreground.");

        this.DrawerSettings.AddLoggingEvents();

        return new EventAreaConfiguration
        {
            Name = drawer.Name,
            Enabled = drawer.Enabled,
            EnabledKeybinding = drawer.EnabledKeybinding,
            BuildDirection = drawer.BuildDirection,
            BackgroundColor = drawer.BackgroundColor,
            FontSize = drawer.FontSize,
            FontFace = drawer.FontFace,
            CustomFontPath = drawer.CustomFontPath,
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
            HideAfterWaypointNavigation = hideAfterWaypointNavigation,
            WaypointSendingChannel = waypointSendingChannel,
            WaypointSendingGuild = waypointSendingGuild,
            EventChatFormat = eventChatFormat,
            DisabledEventKeys = disabledEventKeys,
            CompletionAction = completionAction,
            EnableLinkedCompletion = enableLinkedCompletion,
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
            EventTimespanDaysFormatString = eventTimespanDaysFormatString,
            EventTimespanHoursFormatString = eventTimespanHoursFormatString,
            EventTimespanMinutesFormatString = eventTimespanMinutesFormatString,
            EventAbsoluteTimeFormatString = eventAbsoluteTimeFormatString,
            ShowTopTimeline = showTopTimeline,
            TopTimelineTimeFormatString = topTimelineTimeFormatString,
            TopTimelineBackgroundColor = topTimelineBackgroundColor,
            TopTimelineLineColor = topTimelineLineColor,
            TopTimelineTimeColor = topTimelineTimeColor,
            TopTimelineBackgroundOpacity = topTimelineBackgroundOpacity,
            TopTimelineLineOpacity = topTimelineLineOpacity,
            TopTimelineTimeOpacity = topTimelineTimeOpacity,
            TopTimelineLinesOverWholeHeight = topTimelineLinesOverWholeHeight,
            TopTimelineLinesInBackground = topTimelineLinesInBackground
        };
    }

    public void CheckDrawerSettings(EventAreaConfiguration configuration, List<EventCategory> categories)
    {
        Dictionary<int, EventCategory> notOrderedEventCategories = categories.Where(ec => !configuration.EventOrder.Value.Contains(ec.Key)).ToDictionary(ec => categories.IndexOf(ec), ec => ec);
        foreach (KeyValuePair<int, EventCategory> notOrderedEventCategory in notOrderedEventCategories)
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
        this.DrawerSettings.UndefineSetting($"{name}-waypointSendingChannel");
        this.DrawerSettings.UndefineSetting($"{name}-waypointSendingGuild");
        this.DrawerSettings.UndefineSetting($"{name}-eventChatFormat");
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
        this.DrawerSettings.UndefineSetting($"{name}-eventTimespanDaysFormatString");
        this.DrawerSettings.UndefineSetting($"{name}-eventTimespanHoursFormatString");
        this.DrawerSettings.UndefineSetting($"{name}-eventTimespanMinutesFormatString");
        this.DrawerSettings.UndefineSetting($"{name}-eventAbsoluteTimeFormatString");
        this.DrawerSettings.UndefineSetting($"{name}-showTopTimeline");
        this.DrawerSettings.UndefineSetting($"{name}-topTimeLineTimeFormatString");
        this.DrawerSettings.UndefineSetting($"{name}-topTimelineBackgroundColor");
        this.DrawerSettings.UndefineSetting($"{name}-topTimelineLineColor");
        this.DrawerSettings.UndefineSetting($"{name}-topTimelineTimeColor");
        this.DrawerSettings.UndefineSetting($"{name}-topTimelineBackgroundOpacity");
        this.DrawerSettings.UndefineSetting($"{name}-topTimelineLineOpacity");
        this.DrawerSettings.UndefineSetting($"{name}-topTimelineTimeOpacity");
        this.DrawerSettings.UndefineSetting($"{name}-topTimelineLinesOverWholeHeight");
        this.DrawerSettings.UndefineSetting($"{name}-topTimelineLinesInBackground");
    }

    public override void UpdateLocalization(TranslationService translationService)
    {
        base.UpdateLocalization(translationService);

        string mapKeybindingDisplayNameDefault = this.MapKeybinding.DisplayName;
        string mapKeybindingDescriptionDefault = this.MapKeybinding.Description;
        this.MapKeybinding.GetDisplayNameFunc = () => translationService.GetTranslation("setting-mapKeybinding-name", mapKeybindingDisplayNameDefault);
        this.MapKeybinding.GetDescriptionFunc = () => translationService.GetTranslation("setting-mapKeybinding-description", mapKeybindingDescriptionDefault);

        string remindersEnabledDisplayNameDefault = this.RemindersEnabled.DisplayName;
        string remindersEnabledDescriptionDefault = this.RemindersEnabled.Description;
        this.RemindersEnabled.GetDisplayNameFunc = () => translationService.GetTranslation("setting-remindersEnabled-name", remindersEnabledDisplayNameDefault);
        this.RemindersEnabled.GetDescriptionFunc = () => translationService.GetTranslation("setting-remindersEnabled-description", remindersEnabledDescriptionDefault);

        string reminderPositionXDisplayNameDefault = this.ReminderPosition.X.DisplayName;
        string reminderPositionXDescriptionDefault = this.ReminderPosition.X.Description;
        this.ReminderPosition.X.GetDisplayNameFunc = () => translationService.GetTranslation("setting-reminderPositionX-name", reminderPositionXDisplayNameDefault);
        this.ReminderPosition.X.GetDescriptionFunc = () => translationService.GetTranslation("setting-reminderPositionX-description", reminderPositionXDescriptionDefault);

        string reminderPositionYDisplayNameDefault = this.ReminderPosition.Y.DisplayName;
        string reminderPositionYDescriptionDefault = this.ReminderPosition.Y.Description;
        this.ReminderPosition.Y.GetDisplayNameFunc = () => translationService.GetTranslation("setting-reminderPositionY-name", reminderPositionYDisplayNameDefault);
        this.ReminderPosition.Y.GetDescriptionFunc = () => translationService.GetTranslation("setting-reminderPositionY-description", reminderPositionYDescriptionDefault);

        string reminderDurationDisplayNameDefault = this.ReminderDuration.DisplayName;
        string reminderDurationDescriptionDefault = this.ReminderDuration.Description;
        this.ReminderDuration.GetDisplayNameFunc = () => translationService.GetTranslation("setting-reminderDuration-name", reminderDurationDisplayNameDefault);
        this.ReminderDuration.GetDescriptionFunc = () => translationService.GetTranslation("setting-reminderDuration-description", reminderDurationDescriptionDefault);

        string reminderOpacityDisplayNameDefault = this.ReminderBackgroundOpacity.DisplayName;
        string reminderOpacityDescriptionDefault = this.ReminderBackgroundOpacity.Description;
        this.ReminderBackgroundOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-reminderOpacity-name", reminderOpacityDisplayNameDefault);
        this.ReminderBackgroundOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-reminderOpacity-description", reminderOpacityDescriptionDefault);

        string showDynamicEventsOnMapDisplayNameDefault = this.ShowDynamicEventsOnMap.DisplayName;
        string showDynamicEventsOnMapDescriptionDefault = this.ShowDynamicEventsOnMap.Description;
        this.ShowDynamicEventsOnMap.GetDisplayNameFunc = () => translationService.GetTranslation("setting-showDynamicEventsOnMap-name", showDynamicEventsOnMapDisplayNameDefault);
        this.ShowDynamicEventsOnMap.GetDescriptionFunc = () => translationService.GetTranslation("setting-showDynamicEventsOnMap-description", showDynamicEventsOnMapDescriptionDefault);

        string showDynamicEventInWorldDisplayNameDefault = this.ShowDynamicEventInWorld.DisplayName;
        string showDynamicEventInWorldDescriptionDefault = this.ShowDynamicEventInWorld.Description;
        this.ShowDynamicEventInWorld.GetDisplayNameFunc = () => translationService.GetTranslation("setting-showDynamicEventInWorld-name", showDynamicEventInWorldDisplayNameDefault);
        this.ShowDynamicEventInWorld.GetDescriptionFunc = () => translationService.GetTranslation("setting-showDynamicEventInWorld-description", showDynamicEventInWorldDescriptionDefault);

        string showDynamicEventsInWorldOnlyWhenInsideDisplayNameDefault = this.ShowDynamicEventsInWorldOnlyWhenInside.DisplayName;
        string showDynamicEventsInWorldOnlyWhenInsideDescriptionDefault = this.ShowDynamicEventsInWorldOnlyWhenInside.Description;
        this.ShowDynamicEventsInWorldOnlyWhenInside.GetDisplayNameFunc = () => translationService.GetTranslation("setting-showDynamicEventsInWorldOnlyWhenInside-name", showDynamicEventsInWorldOnlyWhenInsideDisplayNameDefault);
        this.ShowDynamicEventsInWorldOnlyWhenInside.GetDescriptionFunc = () => translationService.GetTranslation("setting-showDynamicEventsInWorldOnlyWhenInside-description", showDynamicEventsInWorldOnlyWhenInsideDescriptionDefault);

        string ignoreZAxisOnDynamicEventsInWorldDisplayNameDefault = this.IgnoreZAxisOnDynamicEventsInWorld.DisplayName;
        string ignoreZAxisOnDynamicEventsInWorldDescriptionDefault = this.IgnoreZAxisOnDynamicEventsInWorld.Description;
        this.IgnoreZAxisOnDynamicEventsInWorld.GetDisplayNameFunc = () => translationService.GetTranslation("setting-ignoreZAxisOnDynamicEventsInWorld-name", ignoreZAxisOnDynamicEventsInWorldDisplayNameDefault);
        this.IgnoreZAxisOnDynamicEventsInWorld.GetDescriptionFunc = () => translationService.GetTranslation("setting-ignoreZAxisOnDynamicEventsInWorld-description", ignoreZAxisOnDynamicEventsInWorldDescriptionDefault);

        string dynamicEventsRenderDistanceDisplayNameDefault = this.DynamicEventsRenderDistance.DisplayName;
        string dynamicEventsRenderDistanceDescriptionDefault = this.DynamicEventsRenderDistance.Description;
        this.DynamicEventsRenderDistance.GetDisplayNameFunc = () => translationService.GetTranslation("setting-dynamicEventsRenderDistance-name", dynamicEventsRenderDistanceDisplayNameDefault);
        this.DynamicEventsRenderDistance.GetDescriptionFunc = () => translationService.GetTranslation("setting-dynamicEventsRenderDistance-description", dynamicEventsRenderDistanceDescriptionDefault);

        string menuEventSortModeDisplayNameDefault = this.MenuEventSortMode.DisplayName;
        string menuEventSortModeDescriptionDefault = this.MenuEventSortMode.Description;
        this.MenuEventSortMode.GetDisplayNameFunc = () => translationService.GetTranslation("setting-menuEventSortMode-name", menuEventSortModeDisplayNameDefault);
        this.MenuEventSortMode.GetDescriptionFunc = () => translationService.GetTranslation("setting-menuEventSortMode-description", menuEventSortModeDescriptionDefault);

        string hideRemindersOnOpenMapDisplayNameDefault = this.HideRemindersOnOpenMap.DisplayName;
        string hideRemindersOnOpenMapDescriptionDefault = this.HideRemindersOnOpenMap.Description;
        this.HideRemindersOnOpenMap.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersOnOpenMap-name", hideRemindersOnOpenMapDisplayNameDefault);
        this.HideRemindersOnOpenMap.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersOnOpenMap-description", hideRemindersOnOpenMapDescriptionDefault);

        string hideRemindersOnMissingMumbleTicksDisplayNameDefault = this.HideRemindersOnMissingMumbleTicks.DisplayName;
        string hideRemindersOnMissingMumbleTicksDescriptionDefault = this.HideRemindersOnMissingMumbleTicks.Description;
        this.HideRemindersOnMissingMumbleTicks.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersOnMissingMumbleTicks-name", hideRemindersOnMissingMumbleTicksDisplayNameDefault);
        this.HideRemindersOnMissingMumbleTicks.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersOnMissingMumbleTicks-description", hideRemindersOnMissingMumbleTicksDescriptionDefault);

        string hideRemindersInCombatDisplayNameDefault = this.HideRemindersInCombat.DisplayName;
        string hideRemindersInCombatDescriptionDefault = this.HideRemindersInCombat.Description;
        this.HideRemindersInCombat.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersInCombat-name", hideRemindersInCombatDisplayNameDefault);
        this.HideRemindersInCombat.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersInCombat-description", hideRemindersInCombatDescriptionDefault);

        string hideRemindersInPvE_OpenWorldDisplayNameDefault = this.HideRemindersInPvE_OpenWorld.DisplayName;
        string hideRemindersInPvE_OpenWorldDescriptionDefault = this.HideRemindersInPvE_OpenWorld.Description;
        this.HideRemindersInPvE_OpenWorld.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersInPvE_OpenWorld-name", hideRemindersInPvE_OpenWorldDisplayNameDefault);
        this.HideRemindersInPvE_OpenWorld.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersInPvE_OpenWorld-description", hideRemindersInPvE_OpenWorldDescriptionDefault);

        string hideRemindersInPvE_CompetetiveDisplayNameDefault = this.HideRemindersInPvE_Competetive.DisplayName;
        string hideRemindersInPvE_CompetetiveDescriptionDefault = this.HideRemindersInPvE_Competetive.Description;
        this.HideRemindersInPvE_Competetive.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersInPvE_Competetive-name", hideRemindersInPvE_CompetetiveDisplayNameDefault);
        this.HideRemindersInPvE_Competetive.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersInPvE_Competetive-description", hideRemindersInPvE_CompetetiveDescriptionDefault);

        string hideRemindersInWvWDisplayNameDefault = this.HideRemindersInWvW.DisplayName;
        string hideRemindersInWvWDescriptionDefault = this.HideRemindersInWvW.Description;
        this.HideRemindersInWvW.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersInWvW-name", hideRemindersInWvWDisplayNameDefault);
        this.HideRemindersInWvW.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersInWvW-description", hideRemindersInWvWDescriptionDefault);

        string hideRemindersInPvPDisplayNameDefault = this.HideRemindersInPvP.DisplayName;
        string hideRemindersInPvPDescriptionDefault = this.HideRemindersInPvP.Description;
        this.HideRemindersInPvP.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideRemindersInPvP-name", hideRemindersInPvPDisplayNameDefault);
        this.HideRemindersInPvP.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideRemindersInPvP-description", hideRemindersInPvPDescriptionDefault);
    }

    public void UpdateDrawerLocalization(EventAreaConfiguration drawerConfiguration, TranslationService translationService)
    {
        base.UpdateDrawerLocalization(drawerConfiguration, translationService);

        string leftClickActionDisplayNameDefault = drawerConfiguration.LeftClickAction.DisplayName;
        string leftClickActionDescriptionDefault = drawerConfiguration.LeftClickAction.Description;
        drawerConfiguration.LeftClickAction.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerLeftClickAction-name", leftClickActionDisplayNameDefault);
        drawerConfiguration.LeftClickAction.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerLeftClickAction-description", leftClickActionDescriptionDefault);

        string showTooltipsDisplayNameDefault = drawerConfiguration.ShowTooltips.DisplayName;
        string showTooltipsDescriptionDefault = drawerConfiguration.ShowTooltips.Description;
        drawerConfiguration.ShowTooltips.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerShowTooltips-name", showTooltipsDisplayNameDefault);
        drawerConfiguration.ShowTooltips.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerShowTooltips-description", showTooltipsDescriptionDefault);

        string timespanDisplayNameDefault = drawerConfiguration.TimeSpan.DisplayName;
        string timespanDescriptionDefault = drawerConfiguration.TimeSpan.Description;
        drawerConfiguration.TimeSpan.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerTimespan-name", timespanDisplayNameDefault);
        drawerConfiguration.TimeSpan.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerTimespan-description", timespanDescriptionDefault);

        string historySplitDisplayNameDefault = drawerConfiguration.HistorySplit.DisplayName;
        string historySplitDescriptionDefault = drawerConfiguration.HistorySplit.Description;
        drawerConfiguration.HistorySplit.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHistorySplit-name", historySplitDisplayNameDefault);
        drawerConfiguration.HistorySplit.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHistorySplit-description", historySplitDescriptionDefault);

        string enableHistorySplitScrollingDisplayNameDefault = drawerConfiguration.EnableHistorySplitScrolling.DisplayName;
        string enableHistorySplitScrollingDescriptionDefault = drawerConfiguration.EnableHistorySplitScrolling.Description;
        drawerConfiguration.EnableHistorySplitScrolling.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEnableHistorySplitScrolling-name", enableHistorySplitScrollingDisplayNameDefault);
        drawerConfiguration.EnableHistorySplitScrolling.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEnableHistorySplitScrolling-description", enableHistorySplitScrollingDescriptionDefault);

        string historySplitScrollingSpeedDisplayNameDefault = drawerConfiguration.HistorySplitScrollingSpeed.DisplayName;
        string historySplitScrollingSpeedDescriptionDefault = drawerConfiguration.HistorySplitScrollingSpeed.Description;
        drawerConfiguration.HistorySplitScrollingSpeed.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHistorySplitScrollingSpeed-name", historySplitScrollingSpeedDisplayNameDefault);
        drawerConfiguration.HistorySplitScrollingSpeed.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHistorySplitScrollingSpeed-description", historySplitScrollingSpeedDescriptionDefault);

        string drawBordersDisplayNameDefault = drawerConfiguration.DrawBorders.DisplayName;
        string drawBordersDescriptionDefault = drawerConfiguration.DrawBorders.Description;
        drawerConfiguration.DrawBorders.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerDrawBorders-name", drawBordersDisplayNameDefault);
        drawerConfiguration.DrawBorders.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerDrawBorders-description", drawBordersDescriptionDefault);

        string useFillersDisplayNameDefault = drawerConfiguration.UseFiller.DisplayName;
        string useFillersDescriptionDefault = drawerConfiguration.UseFiller.Description;
        drawerConfiguration.UseFiller.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerUseFillers-name", useFillersDisplayNameDefault);
        drawerConfiguration.UseFiller.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerUseFillers-description", useFillersDescriptionDefault);

        string fillerTextColorDisplayNameDefault = drawerConfiguration.FillerTextColor.DisplayName;
        string fillerTextColorDescriptionDefault = drawerConfiguration.FillerTextColor.Description;
        drawerConfiguration.FillerTextColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerFillerTextColor-name", fillerTextColorDisplayNameDefault);
        drawerConfiguration.FillerTextColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerFillerTextColor-description", fillerTextColorDescriptionDefault);

        string acceptWaypointPromptDisplayNameDefault = drawerConfiguration.AcceptWaypointPrompt.DisplayName;
        string acceptWaypointPromptDescriptionDefault = drawerConfiguration.AcceptWaypointPrompt.Description;
        drawerConfiguration.AcceptWaypointPrompt.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerAcceptWaypointPrompt-name", acceptWaypointPromptDisplayNameDefault);
        drawerConfiguration.AcceptWaypointPrompt.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerAcceptWaypointPrompt-description", acceptWaypointPromptDescriptionDefault);

        string completionActionDisplayNameDefault = drawerConfiguration.CompletionAction.DisplayName;
        string completionActionDescriptionDefault = drawerConfiguration.CompletionAction.Description;
        drawerConfiguration.CompletionAction.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerCompletionAction-name", completionActionDisplayNameDefault);
        drawerConfiguration.CompletionAction.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerCompletionAction-description", completionActionDescriptionDefault);

        string eventHeightDisplayNameDefault = drawerConfiguration.EventHeight.DisplayName;
        string eventHeightDescriptionDefault = drawerConfiguration.EventHeight.Description;
        drawerConfiguration.EventHeight.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEventHeight-name", eventHeightDisplayNameDefault);
        drawerConfiguration.EventHeight.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEventHeight-description", eventHeightDescriptionDefault);

        string eventBackgroundOpacityDisplayNameDefault = drawerConfiguration.EventBackgroundOpacity.DisplayName;
        string eventBackgroundOpacityDescriptionDefault = drawerConfiguration.EventBackgroundOpacity.Description;
        drawerConfiguration.EventBackgroundOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEventBackgroundOpacity-name", eventBackgroundOpacityDisplayNameDefault);
        drawerConfiguration.EventBackgroundOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEventBackgroundOpacity-description", eventBackgroundOpacityDescriptionDefault);

        string drawShadowsDisplayNameDefault = drawerConfiguration.DrawShadows.DisplayName;
        string drawShadowsDescriptionDefault = drawerConfiguration.DrawShadows.Description;
        drawerConfiguration.DrawShadows.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerDrawShadows-name", drawShadowsDisplayNameDefault);
        drawerConfiguration.DrawShadows.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerDrawShadows-description", drawShadowsDescriptionDefault);

        string shadowColorDisplayNameDefault = drawerConfiguration.ShadowColor.DisplayName;
        string shadowColorDescriptionDefault = drawerConfiguration.ShadowColor.Description;
        drawerConfiguration.ShadowColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerShadowColor-name", shadowColorDisplayNameDefault);
        drawerConfiguration.ShadowColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerShadowColor-description", shadowColorDescriptionDefault);

        string drawShadowsForFillerDisplayNameDefault = drawerConfiguration.DrawShadowsForFiller.DisplayName;
        string drawShadowsForFillerDescriptionDefault = drawerConfiguration.DrawShadowsForFiller.Description;
        drawerConfiguration.DrawShadowsForFiller.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerDrawShadowsForFiller-name", drawShadowsForFillerDisplayNameDefault);
        drawerConfiguration.DrawShadowsForFiller.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerDrawShadowsForFiller-description", drawShadowsForFillerDescriptionDefault);

        string fillerShadowColorDisplayNameDefault = drawerConfiguration.FillerShadowColor.DisplayName;
        string fillerShadowColorDescriptionDefault = drawerConfiguration.FillerShadowColor.Description;
        drawerConfiguration.FillerShadowColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerFillerShadowColor-name", fillerShadowColorDisplayNameDefault);
        drawerConfiguration.FillerShadowColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerFillerShadowColor-description", fillerShadowColorDescriptionDefault);

        string drawIntervalDisplayNameDefault = drawerConfiguration.DrawInterval.DisplayName;
        string drawIntervalDescriptionDefault = drawerConfiguration.DrawInterval.Description;
        drawerConfiguration.DrawInterval.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerDrawInterval-name", drawIntervalDisplayNameDefault);
        drawerConfiguration.DrawInterval.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerDrawInterval-description", drawIntervalDescriptionDefault);

        string limitToCurrentMapDisplayNameDefault = drawerConfiguration.LimitToCurrentMap.DisplayName;
        string limitToCurrentMapDescriptionDefault = drawerConfiguration.LimitToCurrentMap.Description;
        drawerConfiguration.LimitToCurrentMap.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerLimitToCurrentMap-name", limitToCurrentMapDisplayNameDefault);
        drawerConfiguration.LimitToCurrentMap.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerLimitToCurrentMap-description", limitToCurrentMapDescriptionDefault);

        string allowUnspecifiedMapDisplayNameDefault = drawerConfiguration.AllowUnspecifiedMap.DisplayName;
        string allowUnspecifiedMapDescriptionDefault = drawerConfiguration.AllowUnspecifiedMap.Description;
        drawerConfiguration.AllowUnspecifiedMap.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerAllowUnspecifiedMap-name", allowUnspecifiedMapDisplayNameDefault);
        drawerConfiguration.AllowUnspecifiedMap.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerAllowUnspecifiedMap-description", allowUnspecifiedMapDescriptionDefault);

        string timeLineOpacityDisplayNameDefault = drawerConfiguration.TimeLineOpacity.DisplayName;
        string timeLineOpacityDescriptionDefault = drawerConfiguration.TimeLineOpacity.Description;
        drawerConfiguration.TimeLineOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerTimeLineOpacity-name", timeLineOpacityDisplayNameDefault);
        drawerConfiguration.TimeLineOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerTimeLineOpacity-description", timeLineOpacityDescriptionDefault);

        string eventTextOpacityDisplayNameDefault = drawerConfiguration.EventTextOpacity.DisplayName;
        string eventTextOpacityDescriptionDefault = drawerConfiguration.EventTextOpacity.Description;
        drawerConfiguration.EventTextOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEventTextOpacity-name", eventTextOpacityDisplayNameDefault);
        drawerConfiguration.EventTextOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEventTextOpacity-description", eventTextOpacityDescriptionDefault);

        string fillerTextOpacityDisplayNameDefault = drawerConfiguration.FillerTextOpacity.DisplayName;
        string fillerTextOpacityDescriptionDefault = drawerConfiguration.FillerTextOpacity.Description;
        drawerConfiguration.FillerTextOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerFillerTextOpacity-name", fillerTextOpacityDisplayNameDefault);
        drawerConfiguration.FillerTextOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerFillerTextOpacity-description", fillerTextOpacityDescriptionDefault);

        string shadowOpacityDisplayNameDefault = drawerConfiguration.ShadowOpacity.DisplayName;
        string shadowOpacityDescriptionDefault = drawerConfiguration.ShadowOpacity.Description;
        drawerConfiguration.ShadowOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerShadowOpacity-name", shadowOpacityDisplayNameDefault);
        drawerConfiguration.ShadowOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerShadowOpacity-description", shadowOpacityDescriptionDefault);

        string fillerShadowOpacityDisplayNameDefault = drawerConfiguration.FillerShadowOpacity.DisplayName;
        string fillerShadowOpacityDescriptionDefault = drawerConfiguration.FillerShadowOpacity.Description;
        drawerConfiguration.FillerShadowOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerFillerShadowOpacity-name", fillerShadowOpacityDisplayNameDefault);
        drawerConfiguration.FillerShadowOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerFillerShadowOpacity-description", fillerShadowOpacityDescriptionDefault);

        string completedEventsBackgroundOpacityDisplayNameDefault = drawerConfiguration.CompletedEventsBackgroundOpacity.DisplayName;
        string completedEventsBackgroundOpacityDescriptionDefault = drawerConfiguration.CompletedEventsBackgroundOpacity.Description;
        drawerConfiguration.CompletedEventsBackgroundOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsBackgroundOpacity-name", completedEventsBackgroundOpacityDisplayNameDefault);
        drawerConfiguration.CompletedEventsBackgroundOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsBackgroundOpacity-description", completedEventsBackgroundOpacityDescriptionDefault);

        string completedEventsTextOpacityDisplayNameDefault = drawerConfiguration.CompletedEventsTextOpacity.DisplayName;
        string completedEventsTextOpacityDescriptionDefault = drawerConfiguration.CompletedEventsTextOpacity.Description;
        drawerConfiguration.CompletedEventsTextOpacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsTextOpacity-name", completedEventsTextOpacityDisplayNameDefault);
        drawerConfiguration.CompletedEventsTextOpacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsTextOpacity-description", completedEventsTextOpacityDescriptionDefault);

        string completedEventsInvertTextColorDisplayNameDefault = drawerConfiguration.CompletedEventsInvertTextColor.DisplayName;
        string completedEventsInvertTextColorDescriptionDefault = drawerConfiguration.CompletedEventsInvertTextColor.Description;
        drawerConfiguration.CompletedEventsInvertTextColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsInvertTextColor-name", completedEventsInvertTextColorDisplayNameDefault);
        drawerConfiguration.CompletedEventsInvertTextColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerCompletedEventsInvertTextColor-description", completedEventsInvertTextColorDescriptionDefault);

        string hideOnOpenMapDisplayNameDefault = drawerConfiguration.HideOnOpenMap.DisplayName;
        string hideOnOpenMapDescriptionDefault = drawerConfiguration.HideOnOpenMap.Description;
        drawerConfiguration.HideOnOpenMap.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideOnOpenMap-name", hideOnOpenMapDisplayNameDefault);
        drawerConfiguration.HideOnOpenMap.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideOnOpenMap-description", hideOnOpenMapDescriptionDefault);

        string hideOnMissingMumbleTicksDisplayNameDefault = drawerConfiguration.HideOnMissingMumbleTicks.DisplayName;
        string hideOnMissingMumbleTicksDescriptionDefault = drawerConfiguration.HideOnMissingMumbleTicks.Description;
        drawerConfiguration.HideOnMissingMumbleTicks.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideOnMissingMumbleTicks-name", hideOnMissingMumbleTicksDisplayNameDefault);
        drawerConfiguration.HideOnMissingMumbleTicks.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideOnMissingMumbleTicks-description", hideOnMissingMumbleTicksDescriptionDefault);

        string hideInCombatDisplayNameDefault = drawerConfiguration.HideInCombat.DisplayName;
        string hideInCombatDescriptionDefault = drawerConfiguration.HideInCombat.Description;
        drawerConfiguration.HideInCombat.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideInCombat-name", hideInCombatDisplayNameDefault);
        drawerConfiguration.HideInCombat.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideInCombat-description", hideInCombatDescriptionDefault);

        string hideInPvE_OpenWorldDisplayNameDefault = drawerConfiguration.HideInPvE_OpenWorld.DisplayName;
        string hideInPvE_OpenWorldDescriptionDefault = drawerConfiguration.HideInPvE_OpenWorld.Description;
        drawerConfiguration.HideInPvE_OpenWorld.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideInPvE_OpenWorld-name", hideInPvE_OpenWorldDisplayNameDefault);
        drawerConfiguration.HideInPvE_OpenWorld.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideInPvE_OpenWorld-description", hideInPvE_OpenWorldDescriptionDefault);

        string hideInPvE_CompetetiveDisplayNameDefault = drawerConfiguration.HideInPvE_Competetive.DisplayName;
        string hideInPvE_CompetetiveDescriptionDefault = drawerConfiguration.HideInPvE_Competetive.Description;
        drawerConfiguration.HideInPvE_Competetive.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideInPvE_Competetive-name", hideInPvE_CompetetiveDisplayNameDefault);
        drawerConfiguration.HideInPvE_Competetive.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideInPvE_Competetive-description", hideInPvE_CompetetiveDescriptionDefault);

        string hideInWvWDisplayNameDefault = drawerConfiguration.HideInWvW.DisplayName;
        string hideInWvWDescriptionDefault = drawerConfiguration.HideInWvW.Description;
        drawerConfiguration.HideInWvW.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideInWvW-name", hideInWvWDisplayNameDefault);
        drawerConfiguration.HideInWvW.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideInWvW-description", hideInWvWDescriptionDefault);

        string hideInPvPDisplayNameDefault = drawerConfiguration.HideInPvP.DisplayName;
        string hideInPvPDescriptionDefault = drawerConfiguration.HideInPvP.Description;
        drawerConfiguration.HideInPvP.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHideInPvP-name", hideInPvPDisplayNameDefault);
        drawerConfiguration.HideInPvP.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHideInPvP-description", hideInPvPDescriptionDefault);

        string showCategoryNamesDisplayNameDefault = drawerConfiguration.ShowCategoryNames.DisplayName;
        string showCategoryNamesDescriptionDefault = drawerConfiguration.ShowCategoryNames.Description;
        drawerConfiguration.ShowCategoryNames.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerShowCategoryNames-name", showCategoryNamesDisplayNameDefault);
        drawerConfiguration.ShowCategoryNames.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerShowCategoryNames-description", showCategoryNamesDescriptionDefault);

        string categoryNameColorDisplayNameDefault = drawerConfiguration.CategoryNameColor.DisplayName;
        string categoryNameColorDescriptionDefault = drawerConfiguration.CategoryNameColor.Description;
        drawerConfiguration.CategoryNameColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerCategoryNameColor-name", categoryNameColorDisplayNameDefault);
        drawerConfiguration.CategoryNameColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerCategoryNameColor-description", categoryNameColorDescriptionDefault);

        string enableColorGradientsDisplayNameDefault = drawerConfiguration.EnableColorGradients.DisplayName;
        string enableColorGradientsDescriptionDefault = drawerConfiguration.EnableColorGradients.Description;
        drawerConfiguration.EnableColorGradients.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEnableColorGradients-name", enableColorGradientsDisplayNameDefault);
        drawerConfiguration.EnableColorGradients.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEnableColorGradients-description", enableColorGradientsDescriptionDefault);
    }

    public void ValidateAndTryFixSettings()
    {
        this.ValidateAndFixDisableRemindersWhenEventFinishedArea();
    }

    private void ValidateAndFixDisableRemindersWhenEventFinishedArea()
    {
        var areaNames = this.EventAreaNames.Value.ToArray().ToList();
        var disableRemindersWhenEventFinishedArea = this.DisableRemindersWhenEventFinishedArea.Value;
        if (disableRemindersWhenEventFinishedArea != ANY_AREA_NAME && !areaNames.Contains(disableRemindersWhenEventFinishedArea))
        {
            this.Logger.Warn($"Setting \"{nameof(this.DisableRemindersWhenEventFinishedArea)}\" has the invalid area \"{disableRemindersWhenEventFinishedArea}\" set and will be reset to \"{ANY_AREA_NAME}\".");
            this.DisableRemindersWhenEventFinishedArea.Value = ANY_AREA_NAME;
        }
    }

    protected override void PerformMigration(SemVer.Version? lastModuleVersion, SemVer.Version currentModuleVersion)
    {
        if (lastModuleVersion == null || lastModuleVersion <= new SemVer.Version("3.15.0"))
        {
            foreach (var areaName in this.EventAreaNames.Value)
            {
                var entryKey = $"{areaName}-eventTimespanDaysFormatString";
                if (this.DrawerSettings[entryKey] is SettingEntry<string> daysFormatStringSetting && daysFormatStringSetting.Value == "dd\\.hh\\:mm\\:ss")
                {
                    daysFormatStringSetting.Value = "DD\\.hh\\:mm\\:ss";
                    this.Logger.Info($"Performed migration of {entryKey}");
                }
            }

            //if ("dd\\.hh\\:mm\\:ss")
        }
    }

    public override void Unload()
    {
        base.Unload();
        this.ShowDynamicEventInWorld.SettingChanged -= this.ShowDynamicEventInWorld_SettingChanged;
        this.ShowDynamicEventsInWorldOnlyWhenInside.SettingChanged -= this.ShowDynamicEventsInWorldOnlyWhenInside_SettingChanged;
        this.ShowDynamicEventsOnMapKeybinding.Value.Activated -= this.ShowDynamicEventsOnMapKeybinding_Activated;
        this.ShowDynamicEventsInWorldKeybinding.Value.Activated -= this.ShowDynamicEventInWorldKeybinding_Activated;
        this.ShowEventTimersInWorldKeybinding.Value.Activated -= this.ShowEventTimersInWorldKeybinding_Activated;
        this.ShowEventTimersOnMapKeybinding.Value.Activated -= this.ShowEventTimersOnMapKeybinding_Activated;

        this.EventAreaSettings.RemoveLoggingEvents();
    }
}