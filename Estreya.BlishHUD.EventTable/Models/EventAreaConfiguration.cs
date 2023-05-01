namespace Estreya.BlishHUD.EventTable.Models;

using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Models.Drawers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventAreaConfiguration : DrawerConfiguration
{    
    public SettingEntry<List<string>> DisabledEventKeys { get; set; }
    public SettingEntry<EventCompletedAction> CompletionAction { get; set; }

    public SettingEntry<bool> ShowTooltips { get; set; }
    public SettingEntry<LeftClickAction> LeftClickAction { get; set; }
    public SettingEntry<bool> AcceptWaypointPrompt { get; set; }
    public SettingEntry<int> TimeSpan { get; set; } 
    public SettingEntry<int> HistorySplit { get; set; } 
    public SettingEntry<bool> DrawBorders { get; set; } 
    public SettingEntry<bool> UseFiller { get; set; }
    public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> FillerTextColor { get; set; }
    public SettingEntry<float> FillerTextOpacity { get; set; }
    public SettingEntry<bool> DrawShadowsForFiller { get; set; }
    public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> FillerShadowColor { get; set; }
    public SettingEntry<float> FillerShadowOpacity { get; set; }
    public SettingEntry<int> EventHeight { get; set; } 

    /// <summary>
    /// Defines the event orders. Contains a list of category names.
    /// </summary>
    public SettingEntry<List<string>> EventOrder { get; set; }

    public SettingEntry<float> EventBackgroundOpacity { get; set; }

    public SettingEntry<float> EventTextOpacity { get; set; }
    public SettingEntry<bool> DrawShadows { get; set; }
    public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> ShadowColor { get; set; }
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
    public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> CategoryNameColor { get; set; }

    public SettingEntry<bool> EnableColorGradients { get; set; }
}
