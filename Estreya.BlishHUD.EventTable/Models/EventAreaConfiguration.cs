namespace Estreya.BlishHUD.EventTable.Models;

using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Shared.Models.Drawers;
using System.Collections.Generic;

public class EventAreaConfiguration : DrawerConfiguration
{
    public SettingEntry<List<string>> DisabledEventKeys { get; set; }
    public SettingEntry<EventCompletedAction> CompletionAction { get; set; }

    public SettingEntry<bool> ShowTooltips { get; set; }
    public SettingEntry<LeftClickAction> LeftClickAction { get; set; }
    public SettingEntry<bool> AcceptWaypointPrompt { get; set; }
    public SettingEntry<int> TimeSpan { get; set; }
    public SettingEntry<int> HistorySplit { get; set; }
    public SettingEntry<bool> EnableHistorySplitScrolling { get; set; }
    public SettingEntry<int> HistorySplitScrollingSpeed { get; set; }
    public SettingEntry<bool> DrawBorders { get; set; }
    public SettingEntry<bool> UseFiller { get; set; }
    public SettingEntry<Color> FillerTextColor { get; set; }
    public SettingEntry<float> FillerTextOpacity { get; set; }
    public SettingEntry<bool> DrawShadowsForFiller { get; set; }
    public SettingEntry<Color> FillerShadowColor { get; set; }
    public SettingEntry<float> FillerShadowOpacity { get; set; }
    public SettingEntry<int> EventHeight { get; set; }

    /// <summary>
    ///     Defines the event orders. Contains a list of category names.
    /// </summary>
    public SettingEntry<List<string>> EventOrder { get; set; }

    public SettingEntry<float> EventBackgroundOpacity { get; set; }

    public SettingEntry<float> EventTextOpacity { get; set; }
    public SettingEntry<bool> DrawShadows { get; set; }
    public SettingEntry<Color> ShadowColor { get; set; }
    public SettingEntry<float> ShadowOpacity { get; set; }
    public SettingEntry<DrawInterval> DrawInterval { get; set; }

    public SettingEntry<bool> LimitToCurrentMap { get; set; }

    public SettingEntry<bool> AllowUnspecifiedMap { get; set; }
    public SettingEntry<float> TimeLineOpacity { get; set; }
    public SettingEntry<float> CompletedEventsBackgroundOpacity { get; set; }
    public SettingEntry<float> CompletedEventsTextOpacity { get; set; }
    public SettingEntry<bool> CompletedEventsInvertTextColor { get; set; }

    // UI Visibility - START
    public SettingEntry<bool> HideOnMissingMumbleTicks { get; set; }
    public SettingEntry<bool> HideInCombat { get; set; }
    public SettingEntry<bool> HideOnOpenMap { get; set; }
    public SettingEntry<bool> HideInPvE_OpenWorld { get; set; }
    public SettingEntry<bool> HideInPvE_Competetive { get; set; }
    public SettingEntry<bool> HideInWvW { get; set; }

    public SettingEntry<bool> HideInPvP { get; set; }
    // UI Visibility - END

    public SettingEntry<bool> ShowCategoryNames { get; set; }
    public SettingEntry<Color> CategoryNameColor { get; set; }

    public SettingEntry<bool> EnableColorGradients { get; set; }

    public SettingEntry<string> EventAbsoluteTimeFormatString { get; set; }
    public SettingEntry<string> EventTimespanDaysFormatString { get; set; }
    public SettingEntry<string> EventTimespanHoursFormatString { get; set; }
    public SettingEntry<string> EventTimespanMinutesFormatString { get; set; }
    public SettingEntry<bool> ShowTopTimeline { get; set; }
    public SettingEntry<string> TopTimelineTimeFormatString { get; set; }

    public SettingEntry<Color> TopTimelineBackgroundColor { get; set; }

    public SettingEntry<Color> TopTimelineLineColor { get; set; }

    public SettingEntry<Color> TopTimelineTimeColor { get; set; }

    public SettingEntry<float> TopTimelineBackgroundOpacity { get; set; }
    public SettingEntry<float> TopTimelineLineOpacity { get; set; }
    public SettingEntry<float> TopTimelineTimeOpacity { get; set; }

    public SettingEntry<bool> TopTimelineLinesOverWholeHeight { get; set; }
    public SettingEntry<bool> TopTimelineLinesInBackground { get; set; }

    public void CopyTo(EventAreaConfiguration other)
    {
        base.CopyTo(other);

        other.DisabledEventKeys.Value = this.DisabledEventKeys.Value;
        other.CompletionAction.Value = this.CompletionAction.Value;
        other.ShowTooltips.Value = this.ShowTooltips.Value;
        other.LeftClickAction.Value = this.LeftClickAction.Value;
        other.AcceptWaypointPrompt.Value = this.AcceptWaypointPrompt.Value;
        other.TimeSpan.Value = this.TimeSpan.Value;
        other.HistorySplit.Value = this.HistorySplit.Value;
        other.EnableHistorySplitScrolling.Value = this.EnableHistorySplitScrolling.Value;
        other.HistorySplitScrollingSpeed.Value = this.HistorySplitScrollingSpeed.Value;
        other.DrawBorders.Value = this.DrawBorders.Value;
        other.UseFiller.Value = this.UseFiller.Value;
        other.FillerTextColor.Value = this.FillerTextColor.Value;
        other.FillerTextOpacity.Value = this.FillerTextOpacity.Value;
        other.DrawShadowsForFiller.Value = this.DrawShadowsForFiller.Value;
        other.FillerShadowColor.Value = this.FillerShadowColor.Value;
        other.FillerShadowOpacity.Value = this.FillerShadowOpacity.Value;
        other.EventHeight.Value = this.EventHeight.Value;
        other.EventOrder.Value = this.EventOrder.Value;
        other.EventBackgroundOpacity.Value = this.EventBackgroundOpacity.Value;
        other.EventTextOpacity.Value = this.EventTextOpacity.Value;
        other.DrawShadows.Value = this.DrawShadows.Value;
        other.ShadowColor.Value = this.ShadowColor.Value;
        other.DrawInterval.Value = this.DrawInterval.Value;
        other.LimitToCurrentMap.Value = this.LimitToCurrentMap.Value;
        other.AllowUnspecifiedMap.Value = this.AllowUnspecifiedMap.Value;
        other.TimeLineOpacity.Value = this.TimeLineOpacity.Value;
        other.CompletedEventsBackgroundOpacity.Value = this.CompletedEventsBackgroundOpacity.Value;
        other.CompletedEventsTextOpacity.Value = this.CompletedEventsTextOpacity.Value;
        other.CompletedEventsInvertTextColor.Value = this.CompletedEventsInvertTextColor.Value;
        other.HideOnMissingMumbleTicks.Value = this.HideOnMissingMumbleTicks.Value;
        other.HideInCombat.Value = this.HideInCombat.Value;
        other.HideOnOpenMap.Value = this.HideOnOpenMap.Value;
        other.HideInPvE_OpenWorld.Value = this.HideInPvE_OpenWorld.Value;
        other.HideInPvE_Competetive.Value = this.HideInPvE_Competetive.Value;
        other.HideInWvW.Value = this.HideInWvW.Value;
        other.HideInPvP.Value = this.HideInPvP.Value;
        other.ShowCategoryNames.Value = this.ShowCategoryNames.Value;
        other.CategoryNameColor.Value = this.CategoryNameColor.Value;
        other.EnableColorGradients.Value = this.EnableColorGradients.Value;
        other.EventAbsoluteTimeFormatString.Value = this.EventAbsoluteTimeFormatString.Value;
        other.EventTimespanDaysFormatString.Value = this.EventTimespanDaysFormatString.Value;
        other.EventTimespanHoursFormatString.Value = this.EventTimespanHoursFormatString.Value;
        other.EventTimespanMinutesFormatString.Value = this.EventTimespanMinutesFormatString.Value;
        other.ShowTopTimeline.Value = this.ShowTopTimeline.Value;
        other.TopTimelineTimeFormatString.Value = this.TopTimelineTimeFormatString.Value;
        other.TopTimelineBackgroundColor.Value = this.TopTimelineBackgroundColor.Value;
        other.TopTimelineLineColor.Value = this.TopTimelineLineColor.Value;
        other.TopTimelineTimeColor.Value = this.TopTimelineTimeColor.Value;
        other.TopTimelineBackgroundOpacity.Value = this.TopTimelineBackgroundOpacity.Value;
        other.TopTimelineLineOpacity.Value = this.TopTimelineLineOpacity.Value;
        other.TopTimelineTimeOpacity.Value = this.TopTimelineTimeOpacity.Value;
        other.TopTimelineLinesOverWholeHeight.Value = this.TopTimelineLinesOverWholeHeight.Value;
        other.TopTimelineLinesInBackground.Value = this.TopTimelineLinesInBackground.Value;
    }
}