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
    public SettingEntry<List<string>> ActiveEventKeys { get; set; }
    public SettingEntry<EventCompletedAction> CompletionAcion { get; set; }

    public SettingEntry<bool> ShowTooltips { get; set; }
    public SettingEntry<LeftClickAction> LeftClickAction { get; set; }
    public SettingEntry<bool> AcceptWaypointPrompt { get; set; }
    public SettingEntry<bool> ShowContextMenu { get; set; }
    public SettingEntry<int> TimeSpan { get; set; } 
    public SettingEntry<int> HistorySplit { get; set; } 
    public SettingEntry<bool> DrawBorders { get; set; } 
    public SettingEntry<bool> UseFiller { get; set; }
    public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> FillerTextColor { get; set; }
}
