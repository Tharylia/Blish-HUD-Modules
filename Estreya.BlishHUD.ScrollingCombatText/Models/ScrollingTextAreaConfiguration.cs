namespace Estreya.BlishHUD.ScrollingCombatText.Models;

using Blish_HUD.Settings;
using Shared.Models.ArcDPS;
using Shared.Models.Drawers;
using System.Collections.Generic;

public class ScrollingTextAreaConfiguration : DrawerConfiguration
{
    public SettingEntry<List<CombatEventType>> Types { get; set; }

    public SettingEntry<List<CombatEventCategory>> Categories { get; set; }

    public SettingEntry<int> EventHeight { get; set; }

    public SettingEntry<float> ScrollSpeed { get; set; }

    public SettingEntry<ScrollingTextAreaCurve> Curve { get; set; }

    public SettingEntry<List<CombatEventFormatRule>> FormatRules { get; set; }
}