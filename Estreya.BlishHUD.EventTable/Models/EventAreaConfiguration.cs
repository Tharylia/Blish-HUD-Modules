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
    public SettingEntry<List<string>> ActiveEventKeys { get; init; }
    public SettingEntry<EventCompletedAction> CompletionAcion { get; init; }

    public SettingEntry<bool> ShowTooltips { get; init; }
    public SettingEntry<LeftClickAction> LeftClickAction { get; init; }
    public SettingEntry<bool> AcceptWaypointPrompt { get; init; }
    public SettingEntry<bool> ShowContextMenu { get; init; }
    public SettingEntry<int> TimeSpan { get; init; } 
    public SettingEntry<int> HistorySplit { get; init; } 
    public SettingEntry<bool> DrawBorders { get; init; } 
    public SettingEntry<bool> UseFiller { get; init; } 
}
