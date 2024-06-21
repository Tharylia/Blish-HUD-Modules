namespace Estreya.BlishHUD.BuffReminder;

using Blish_HUD;
using Blish_HUD.Input;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Extensions;
using Gw2Sharp.WebApi.V2.Models;
using Humanizer.Localisation;
using Microsoft.Xna.Framework.Input;
using Shared.Models.Drawers;
using Shared.Services;
using Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

public class ModuleSettings : BaseModuleSettings
{
    private const string AREA_SETTINGS = "area-settings";

    public const string ANY_AREA_NAME = "Any";

    public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(ModifierKeys.Alt, Keys.B)) { }

    private SettingCollection AreaSettings { get; set; }
    public SettingEntry<List<string>> AreaNames { get; private set; }

    protected override void InitializeAdditionalSettings(SettingCollection settings)
    {
        this.AreaSettings = settings.AddSubCollection(AREA_SETTINGS);

        this.AreaNames = this.AreaSettings.DefineSetting(nameof(this.AreaNames), new List<string>(), () => "Area Names", () => "Defines the area names.");

        this.AreaSettings.AddLoggingEvents();
    }

    protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
    {
        
    }

    //public void CheckDrawerSizeAndPosition(EventAreaConfiguration configuration)
    //{
    //    base.CheckDrawerSizeAndPosition(configuration);
    //}

    //public void CheckGlobalSizeAndPosition()
    //{
    //    int maxResX = (int)(GameService.Graphics.Resolution.X / GameService.Graphics.UIScaleMultiplier);
    //    int maxResY = (int)(GameService.Graphics.Resolution.Y / GameService.Graphics.UIScaleMultiplier);

    //    if (!this.IsMaxResolutionValid(maxResX, maxResY))
    //    {
    //        this.Logger.Warn($"Max Global size and position resolution is invalid. X: {maxResX} - Y: {maxResY}");
    //        return;
    //    }

    //    int minLocationX = 0;
    //    int maxLocationX = maxResX - this.ReminderSize.X.Value;
    //    int minLocationY = 0;
    //    int maxLocationY = maxResY - this.ReminderSize.Y.Value;
    //    int minWidth = 0;
    //    int maxWidth = maxResX - this.ReminderPosition.X.Value;
    //    int minHeight = 0;
    //    int maxHeight = maxResY - this.ReminderPosition.Y.Value;

    //    this.ReminderPosition?.X.SetRange(minLocationX, maxLocationX);
    //    this.ReminderPosition?.Y.SetRange(minLocationY, maxLocationY);
    //    this.ReminderSize?.X.SetRange(minWidth, maxWidth);
    //    this.ReminderSize?.Y.SetRange(minHeight, maxHeight);
    //    this.ReminderSize?.Icon.SetRange(0, this.ReminderSize.Y.Value);
    //}

    //public EventAreaConfiguration AddDrawer(string name, List<EventCategory> eventCategories)
    //{
    //    DrawerConfiguration drawer = base.AddDrawer(name);

    //    SettingEntry<LeftClickAction> leftClickAction = this.DrawerSettings.DefineSetting($"{name}-leftClickAction", LeftClickAction.CopyWaypoint, () => "Left Click Action", () => "Defines the action which is executed when left clicking.");
    //    SettingEntry<bool> showTooltips = this.DrawerSettings.DefineSetting($"{name}-showTooltips", true, () => "Show Tooltips", () => "Whether a tooltip should be displayed when hovering.");

    //    SettingEntry<int> timespan = this.DrawerSettings.DefineSetting($"{name}-timespan", 120, () => "Timespan", () => "Defines the timespan the event drawer covers.");
    //    timespan.SetRange(60, 240);

    //    SettingEntry<int> historySplit = this.DrawerSettings.DefineSetting($"{name}-historySplit", 50, () => "History Split", () => "Defines how much history the timespan should contain.");
    //    historySplit.SetRange(0, 75);
    //    SettingEntry<bool> enableHistorySplitScrolling = this.DrawerSettings.DefineSetting($"{name}-enableHistorySplitScrolling", false, () => "Enable History Split Scrolling", () => "Defines if scrolling inside the event area temporary moves the history split until the mouse leaves the area.");

    //    SettingEntry<int> historySplitScrollingSpeed = this.DrawerSettings.DefineSetting($"{name}-historySplitScrollingSpeed", 1, () => "History Split Scrolling Speed", () => "Defines the speed when scrolling inside the event area.");
    //    historySplitScrollingSpeed.SetRange(1, 10);

    //    SettingEntry<bool> drawBorders = this.DrawerSettings.DefineSetting($"{name}-drawBorders", false, () => "Draw Borders", () => "Whether the events should be rendered with borders.");
    //    SettingEntry<bool> useFillers = this.DrawerSettings.DefineSetting($"{name}-useFillers", true, () => "Use Filler Events", () => "Whether the empty spaces should be filled by filler events.");
    //    SettingEntry<Color> fillerTextColor = this.DrawerSettings.DefineSetting($"{name}-fillerTextColor", this.DefaultGW2Color, () => "Filler Text Color", () => "Defines the text color used by filler events.");

    //    SettingEntry<bool> acceptWaypointPrompt = this.DrawerSettings.DefineSetting($"{name}-acceptWaypointPrompt", true, () => "Accept Waypoint Prompt", () => "Whether the waypoint prompt should be accepted automatically when performing an automated teleport.");

    //    SettingEntry<Shared.Models.GameIntegration.Chat.ChatChannel> waypointSendingChannel = this.DrawerSettings.DefineSetting($"{name}-waypointSendingChannel", Shared.Models.GameIntegration.Chat.ChatChannel.Private, () => "Send Waypoint to Channel", () => "Defines the channel in which the waypoint is pasted automatically.");
    //    SettingEntry<Shared.Models.GameIntegration.Guild.GuildNumber> waypointSendingGuild = this.DrawerSettings.DefineSetting($"{name}-waypointSendingGuild", Shared.Models.GameIntegration.Guild.GuildNumber.Guild_1, () => "Send Waypoint to Guild", () => "Defines the guild in which the waypoint is pasted automatically if channel guild is selected.");
    //    SettingEntry<EventChatFormat> eventChatFormat = this.DrawerSettings.DefineSetting($"{name}-eventChatFormat", EventChatFormat.OnlyWaypoint, () => "Event Chat Format", () => "Defines the chat format when event waypoints are copied or pasted.");

    //    SettingEntry<EventCompletedAction> completionAction = this.DrawerSettings.DefineSetting($"{name}-completionAction", EventCompletedAction.Crossout, () => "Completion Action", () => "Defines the action to perform if an event has been completed.");
    //    SettingEntry<bool> enableLinkedCompletion = this.DrawerSettings.DefineSetting($"{name}-enableLinkedCompletion", true, () => "Enable Linked Completion", () => "Enables the completion of events that are linked to the completed event. (e.g. Auric Basin)");

    //    SettingEntry<List<string>> disabledEventKeys = this.DrawerSettings.DefineSetting($"{name}-disabledEventKeys", new List<string>(), () => "Active Event Keys", () => "Defines the active event keys.");

    //    SettingEntry<int> eventHeight = this.DrawerSettings.DefineSetting($"{name}-eventHeight", 30, () => "Event Height", () => "Defines the height of the individual event rows.");
    //    eventHeight.SetRange(5, 30);

    //    SettingEntry<List<string>> eventOrder = this.DrawerSettings.DefineSetting($"{name}-eventOrder", new List<string>(eventCategories.Select(x => x.Key)), () => "Event Order", () => "Defines the order of events.");

    //    SettingEntry<float> eventBackgroundOpacity = this.DrawerSettings.DefineSetting($"{name}-eventBackgroundOpacity", 1f, () => "Event Background Opacity", () => "Defines the opacity of the individual event backgrounds.");
    //    eventBackgroundOpacity.SetRange(0.1f, 1f);

    //    SettingEntry<bool> drawShadows = this.DrawerSettings.DefineSetting($"{name}-drawShadows", false, () => "Draw Shadows", () => "Whether the text should have shadows");

    //    SettingEntry<Color> shadowColor = this.DrawerSettings.DefineSetting($"{name}-shadowColor", this.DefaultGW2Color, () => "Shadow Color", () => "Defines the color of the shadows");

    //    SettingEntry<bool> drawShadowsForFiller = this.DrawerSettings.DefineSetting($"{name}-drawShadowsForFiller", false, () => "Draw Shadows for Filler", () => "Whether the filler text should have shadows");

    //    SettingEntry<Color> fillerShadowColor = this.DrawerSettings.DefineSetting($"{name}-fillerShadowColor", this.DefaultGW2Color, () => "Filler Shadow Color", () => "Defines the color of the shadows for fillers");

    //    SettingEntry<DrawInterval> drawInterval = this.DrawerSettings.DefineSetting($"{name}-drawInterval", DrawInterval.FAST, () => "Draw Interval", () => "Defines the refresh rate of the drawer.");

    //    SettingEntry<bool> limitToCurrentMap = this.DrawerSettings.DefineSetting($"{name}-limitToCurrentMap", false, () => "Limit to current Map", () => "Whether the drawer should only show events from the current map.");

    //    SettingEntry<bool> allowUnspecifiedMap = this.DrawerSettings.DefineSetting($"{name}-allowUnspecifiedMap", true, () => "Allow from unspecified Maps", () => "Whether the table should show events which do not have a map id specified.");

    //    SettingEntry<float> timeLineOpacity = this.DrawerSettings.DefineSetting($"{name}-timeLineOpacity", 1f, () => "Timeline Opacity", () => "Defines the opacity of the time line bar.");
    //    timeLineOpacity.SetRange(0.1f, 1f);

    //    SettingEntry<float> eventTextOpacity = this.DrawerSettings.DefineSetting($"{name}-eventTextOpacity", 1f, () => "Event Text Opacity", () => "Defines the opacity of the event text.");
    //    eventTextOpacity.SetRange(0.1f, 1f);

    //    SettingEntry<float> fillerTextOpacity = this.DrawerSettings.DefineSetting($"{name}-fillerTextOpacity", 1f, () => "Filler Text Opacity", () => "Defines the opacity of filler event text.");
    //    fillerTextOpacity.SetRange(0.1f, 1f);

    //    SettingEntry<float> shadowOpacity = this.DrawerSettings.DefineSetting($"{name}-shadowOpacity", 1f, () => "Shadow Opacity", () => "Defines the opacity for shadows.");
    //    shadowOpacity.SetRange(0.1f, 1f);

    //    SettingEntry<float> fillerShadowOpacity = this.DrawerSettings.DefineSetting($"{name}-fillerShadowOpacity", 1f, () => "Filler Shadow Opacity", () => "Defines the opacity for filler shadows.");
    //    fillerShadowOpacity.SetRange(0.1f, 1f);

    //    SettingEntry<float> completedEventsBackgroundOpacity = this.DrawerSettings.DefineSetting($"{name}-completedEventsBackgroundOpacity", 0.5f, () => "Completed Events Background Opacity", () => "Defines the background opacity of completed events. Only works in combination with CompletionAction = Change Opacity");
    //    completedEventsBackgroundOpacity.SetRange(0.1f, 0.9f);

    //    SettingEntry<float> completedEventsTextOpacity = this.DrawerSettings.DefineSetting($"{name}-completedEventsTextOpacity", 1f, () => "Completed Events Text Opacity", () => "Defines the text opacity of completed events. Only works in combination with CompletionAction = Change Opacity");
    //    completedEventsBackgroundOpacity.SetRange(0f, 1f);

    //    SettingEntry<bool> completedEventsInvertTextColor = this.DrawerSettings.DefineSetting($"{name}-completedEventsInvertTextColor", true, () => "Completed Events Invert Textcolor", () => "Specified if completed events should have their text color inverted. Only works in combination with CompletionAction = Change Opacity");

    //    SettingEntry<bool> hideOnOpenMap = this.DrawerSettings.DefineSetting($"{name}-hideOnOpenMap", true, () => "Hide on open Map", () => "Whether the area should hide when the map is open.");

    //    SettingEntry<bool> hideOnMissingMumbleTicks = this.DrawerSettings.DefineSetting($"{name}-hideOnMissingMumbleTicks", true, () => "Hide on Cutscenes", () => "Whether the area should hide when cutscenes are played.");

    //    SettingEntry<bool> hideInCombat = this.DrawerSettings.DefineSetting($"{name}-hideInCombat", false, () => "Hide in Combat", () => "Whether the area should hide when in combat.");

    //    SettingEntry<bool> hideInPvE_OpenWorld = this.DrawerSettings.DefineSetting($"{name}-hideInPvE_OpenWorld", false, () => "Hide in PvE (Open World)", () => "Whether the area should hide when in PvE (Open World).");

    //    SettingEntry<bool> hideInPvE_Competetive = this.DrawerSettings.DefineSetting($"{name}-hideInPvE_Competetive", false, () => "Hide in PvE (Competetive)", () => "Whether the area should hide when in PvE (Competetive).");

    //    SettingEntry<bool> hideInWvW = this.DrawerSettings.DefineSetting($"{name}-hideInWvW", false, () => "Hide in WvW", () => "Whether the area should hide when in world vs. world.");

    //    SettingEntry<bool> hideInPvP = this.DrawerSettings.DefineSetting($"{name}-hideInPvP", false, () => "Hide in PvP", () => "Whether the area should hide when in player vs. player.");

    //    SettingEntry<bool> showCategoryNames = this.DrawerSettings.DefineSetting($"{name}-showCategoryNames", false, () => "Show Category Names", () => "Defines if the category names should be shown before the event bars.");

    //    SettingEntry<Color> categoryNameColor = this.DrawerSettings.DefineSetting($"{name}-categoryNameColor", this.DefaultGW2Color, () => "Category Name Color", () => "Defines the color of the category names.");

    //    SettingEntry<bool> enableColorGradients = this.DrawerSettings.DefineSetting($"{name}-enableColorGradients", false, () => "Enable Color Gradients", () => "Defines if supported events should have a smoother color gradient from and to the next event.");

    //    SettingEntry<string> eventTimespanDaysFormatString = this.DrawerSettings.DefineSetting($"{name}-eventTimespanDaysFormatString", "dd\\.hh\\:mm\\:ss", () => "Days Format String", () => "Defines the format strings for timespans over 1 day.");
    //    SettingEntry<string> eventTimespanHoursFormatString = this.DrawerSettings.DefineSetting($"{name}-eventTimespanHoursFormatString", "hh\\:mm\\:ss", () => "Hours Format String", () => "Defines the format strings for timespans over 1 hours.");
    //    SettingEntry<string> eventTimespanMinutesFormatString = this.DrawerSettings.DefineSetting($"{name}-eventTimespanMinutesFormatString", "mm\\:ss", () => "Minutes Format String", () => "Defines the fallback format strings for timespans.");

    //    SettingEntry<string> eventAbsoluteTimeFormatString = this.DrawerSettings.DefineSetting($"{name}-eventAbsoluteTimeFormatString", "HH\\:mm", () => "Absolute Time Format String", () => "Defines the format strings for absolute time.");

    //    var showTopTimeline = this.DrawerSettings.DefineSetting($"{name}-showTopTimeline", false, () => "Show Top Timeline", () => "Defines whether the top timeline is visible.");
    //    var topTimelineTimeFormatString = this.DrawerSettings.DefineSetting($"{name}-topTimelineTimeFormatString", "HH\\:mm", () => "Top Timeline Time Format String", () => "Defines the format strings for absolute time.");
    //    var topTimelineBackgroundColor = this.DrawerSettings.DefineSetting($"{name}-topTimelineBackgroundColor", this.DefaultGW2Color, () => "Top Timeline Background Color", () => "Defines the background color of the top timeline.");
    //    var topTimelineLineColor = this.DrawerSettings.DefineSetting($"{name}-topTimelineLineColor", this.DefaultGW2Color, () => "Top Timeline Line Color", () => "Defines the line color of the top timeline.");
    //    var topTimelineTimeColor = this.DrawerSettings.DefineSetting($"{name}-topTimelineTimeColor", this.DefaultGW2Color, () => "Top Timeline Time Color", () => "Defines the time color of the top timeline.");
    //    var topTimelineBackgroundOpacity = this.DrawerSettings.DefineSetting($"{name}-topTimelineBackgroundOpacity", 1f, () => "Top Timeline Background Opacity", () => "Defines the background color opacity of the top timeline.");
    //    var topTimelineLineOpacity = this.DrawerSettings.DefineSetting($"{name}-topTimelineLineOpacity", 1f, () => "Top Timeline Line Opacity", () => "Defines the line color opacity of the top timeline.");
    //    var topTimelineTimeOpacity = this.DrawerSettings.DefineSetting($"{name}-topTimelineTimeOpacity", 1f, () => "Top Timeline Time Opacity", () => "Defines the time color opacity of the top timeline.");
    //    var topTimelineLinesOverWholeHeight = this.DrawerSettings.DefineSetting($"{name}-topTimelineLinesOverWholeHeight", false, () => "Top Timeline Lines Over Whole Height", () => "Defines if the top timeline lines should cover the whole event area height.");
    //    var topTimelineLinesInBackground = this.DrawerSettings.DefineSetting($"{name}-topTimelineLinesInBackground", true, () => "Top Timeline Lines in Background", () => "Defines if the top timeline lines should be in the background or foreground.");

    //    this.DrawerSettings.AddLoggingEvents();

    //    return new EventAreaConfiguration
    //    {
    //        Name = drawer.Name,
    //        Enabled = drawer.Enabled,
    //        EnabledKeybinding = drawer.EnabledKeybinding,
    //        BuildDirection = drawer.BuildDirection,
    //        BackgroundColor = drawer.BackgroundColor,
    //        FontSize = drawer.FontSize,
    //        FontFace = drawer.FontFace,
    //        CustomFontPath = drawer.CustomFontPath,
    //        TextColor = drawer.TextColor,
    //        Location = drawer.Location,
    //        Opacity = drawer.Opacity,
    //        Size = drawer.Size,
    //        LeftClickAction = leftClickAction,
    //        ShowTooltips = showTooltips,
    //        DrawBorders = drawBorders,
    //        HistorySplit = historySplit,
    //        EnableHistorySplitScrolling = enableHistorySplitScrolling,
    //        HistorySplitScrollingSpeed = historySplitScrollingSpeed,
    //        TimeSpan = timespan,
    //        UseFiller = useFillers,
    //        FillerTextColor = fillerTextColor,
    //        AcceptWaypointPrompt = acceptWaypointPrompt,
    //        WaypointSendingChannel = waypointSendingChannel,
    //        WaypointSendingGuild = waypointSendingGuild,
    //        EventChatFormat = eventChatFormat,
    //        DisabledEventKeys = disabledEventKeys,
    //        CompletionAction = completionAction,
    //        EnableLinkedCompletion = enableLinkedCompletion,
    //        EventHeight = eventHeight,
    //        EventOrder = eventOrder,
    //        EventBackgroundOpacity = eventBackgroundOpacity,
    //        DrawShadows = drawShadows,
    //        ShadowColor = shadowColor,
    //        DrawShadowsForFiller = drawShadowsForFiller,
    //        FillerShadowColor = fillerShadowColor,
    //        DrawInterval = drawInterval,
    //        LimitToCurrentMap = limitToCurrentMap,
    //        AllowUnspecifiedMap = allowUnspecifiedMap,
    //        TimeLineOpacity = timeLineOpacity,
    //        EventTextOpacity = eventTextOpacity,
    //        FillerTextOpacity = fillerTextOpacity,
    //        ShadowOpacity = shadowOpacity,
    //        FillerShadowOpacity = fillerShadowOpacity,
    //        CompletedEventsBackgroundOpacity = completedEventsBackgroundOpacity,
    //        CompletedEventsTextOpacity = completedEventsTextOpacity,
    //        CompletedEventsInvertTextColor = completedEventsInvertTextColor,
    //        HideInCombat = hideInCombat,
    //        HideOnMissingMumbleTicks = hideOnMissingMumbleTicks,
    //        HideOnOpenMap = hideOnOpenMap,
    //        HideInPvE_Competetive = hideInPvE_Competetive,
    //        HideInPvE_OpenWorld = hideInPvE_OpenWorld,
    //        HideInPvP = hideInPvP,
    //        HideInWvW = hideInWvW,
    //        ShowCategoryNames = showCategoryNames,
    //        CategoryNameColor = categoryNameColor,
    //        EnableColorGradients = enableColorGradients,
    //        EventTimespanDaysFormatString = eventTimespanDaysFormatString,
    //        EventTimespanHoursFormatString = eventTimespanHoursFormatString,
    //        EventTimespanMinutesFormatString = eventTimespanMinutesFormatString,
    //        EventAbsoluteTimeFormatString = eventAbsoluteTimeFormatString,
    //        ShowTopTimeline = showTopTimeline,
    //        TopTimelineTimeFormatString = topTimelineTimeFormatString,
    //        TopTimelineBackgroundColor = topTimelineBackgroundColor,
    //        TopTimelineLineColor = topTimelineLineColor,
    //        TopTimelineTimeColor = topTimelineTimeColor,
    //        TopTimelineBackgroundOpacity = topTimelineBackgroundOpacity,
    //        TopTimelineLineOpacity = topTimelineLineOpacity,
    //        TopTimelineTimeOpacity = topTimelineTimeOpacity,
    //        TopTimelineLinesOverWholeHeight = topTimelineLinesOverWholeHeight,
    //        TopTimelineLinesInBackground = topTimelineLinesInBackground
    //    };
    //}

    //public void CheckDrawerSettings(EventAreaConfiguration configuration, List<EventCategory> categories)
    //{
    //    Dictionary<int, EventCategory> notOrderedEventCategories = categories.Where(ec => !configuration.EventOrder.Value.Contains(ec.Key)).ToDictionary(ec => categories.IndexOf(ec), ec => ec);
    //    foreach (KeyValuePair<int, EventCategory> notOrderedEventCategory in notOrderedEventCategories)
    //    {
    //        configuration.EventOrder.Value.Insert(notOrderedEventCategory.Key, notOrderedEventCategory.Value.Key);
    //    }

    //    if (notOrderedEventCategories.Count > 0)
    //    {
    //        configuration.EventOrder.Value = new List<string>(configuration.EventOrder.Value);
    //    }
    //}

    //public new void RemoveDrawer(string name)
    //{
    //    base.RemoveDrawer(name);

    //    this.DrawerSettings.UndefineSetting($"{name}-leftClickAction");
    //    this.DrawerSettings.UndefineSetting($"{name}-showTooltips");
    //    this.DrawerSettings.UndefineSetting($"{name}-timespan");
    //    this.DrawerSettings.UndefineSetting($"{name}-historySplit");
    //    this.DrawerSettings.UndefineSetting($"{name}-enableHistorySplitScrolling");
    //    this.DrawerSettings.UndefineSetting($"{name}-historySplitScrollingSpeed");
    //    this.DrawerSettings.UndefineSetting($"{name}-drawBorders");
    //    this.DrawerSettings.UndefineSetting($"{name}-useFillers");
    //    this.DrawerSettings.UndefineSetting($"{name}-fillerTextColor");
    //    this.DrawerSettings.UndefineSetting($"{name}-acceptWaypointPrompt");
    //    this.DrawerSettings.UndefineSetting($"{name}-waypointSendingChannel");
    //    this.DrawerSettings.UndefineSetting($"{name}-waypointSendingGuild");
    //    this.DrawerSettings.UndefineSetting($"{name}-eventChatFormat");
    //    this.DrawerSettings.UndefineSetting($"{name}-completionAction");
    //    this.DrawerSettings.UndefineSetting($"{name}-disabledEventKeys");
    //    this.DrawerSettings.UndefineSetting($"{name}-eventHeight");
    //    this.DrawerSettings.UndefineSetting($"{name}-eventOrder");
    //    this.DrawerSettings.UndefineSetting($"{name}-eventBackgroundOpacity");
    //    this.DrawerSettings.UndefineSetting($"{name}-drawShadows");
    //    this.DrawerSettings.UndefineSetting($"{name}-shadowColor");
    //    this.DrawerSettings.UndefineSetting($"{name}-drawShadowsForFiller");
    //    this.DrawerSettings.UndefineSetting($"{name}-fillerShadowColor");
    //    this.DrawerSettings.UndefineSetting($"{name}-drawInterval");
    //    this.DrawerSettings.UndefineSetting($"{name}-limitToCurrentMap");
    //    this.DrawerSettings.UndefineSetting($"{name}-allowUnspecifiedMap");
    //    this.DrawerSettings.UndefineSetting($"{name}-timeLineOpacity");
    //    this.DrawerSettings.UndefineSetting($"{name}-eventTextOpacity");
    //    this.DrawerSettings.UndefineSetting($"{name}-fillerTextOpacity");
    //    this.DrawerSettings.UndefineSetting($"{name}-shadowOpacity");
    //    this.DrawerSettings.UndefineSetting($"{name}-fillerShadowOpacity");
    //    this.DrawerSettings.UndefineSetting($"{name}-completedEventsBackgroundOpacity");
    //    this.DrawerSettings.UndefineSetting($"{name}-completedEventsTextOpacity");
    //    this.DrawerSettings.UndefineSetting($"{name}-completedEventsInvertTextColor");
    //    this.DrawerSettings.UndefineSetting($"{name}-hideOnOpenMap");
    //    this.DrawerSettings.UndefineSetting($"{name}-hideOnMissingMumbleTicks");
    //    this.DrawerSettings.UndefineSetting($"{name}-hideInCombat");
    //    this.DrawerSettings.UndefineSetting($"{name}-hideInPvE_OpenWorld");
    //    this.DrawerSettings.UndefineSetting($"{name}-hideInPvE_Competetive");
    //    this.DrawerSettings.UndefineSetting($"{name}-hideInWvW");
    //    this.DrawerSettings.UndefineSetting($"{name}-hideInPvP");
    //    this.DrawerSettings.UndefineSetting($"{name}-showCategoryNames");
    //    this.DrawerSettings.UndefineSetting($"{name}-categoryNameColor");
    //    this.DrawerSettings.UndefineSetting($"{name}-enableColorGradients");
    //    this.DrawerSettings.UndefineSetting($"{name}-eventTimespanDaysFormatString");
    //    this.DrawerSettings.UndefineSetting($"{name}-eventTimespanHoursFormatString");
    //    this.DrawerSettings.UndefineSetting($"{name}-eventTimespanMinutesFormatString");
    //    this.DrawerSettings.UndefineSetting($"{name}-eventAbsoluteTimeFormatString");
    //    this.DrawerSettings.UndefineSetting($"{name}-showTopTimeline");
    //    this.DrawerSettings.UndefineSetting($"{name}-topTimeLineTimeFormatString");
    //    this.DrawerSettings.UndefineSetting($"{name}-topTimelineBackgroundColor");
    //    this.DrawerSettings.UndefineSetting($"{name}-topTimelineLineColor");
    //    this.DrawerSettings.UndefineSetting($"{name}-topTimelineTimeColor");
    //    this.DrawerSettings.UndefineSetting($"{name}-topTimelineBackgroundOpacity");
    //    this.DrawerSettings.UndefineSetting($"{name}-topTimelineLineOpacity");
    //    this.DrawerSettings.UndefineSetting($"{name}-topTimelineTimeOpacity");
    //    this.DrawerSettings.UndefineSetting($"{name}-topTimelineLinesOverWholeHeight");
    //    this.DrawerSettings.UndefineSetting($"{name}-topTimelineLinesInBackground");
    //}

    

    public override void Unload()
    {
        base.Unload();
    }
}