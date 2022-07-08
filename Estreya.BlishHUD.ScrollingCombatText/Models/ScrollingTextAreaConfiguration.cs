namespace Estreya.BlishHUD.ScrollingCombatText.Models;

using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Models.ArcDPS;
using Estreya.BlishHUD.Shared.Models.Drawers;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ScrollingTextAreaConfiguration : DrawerConfiguration
{
    public SettingEntry<List<CombatEventType>> Types { get; init; }

    public SettingEntry<List<CombatEventCategory>> Categories { get; init; }

    public SettingEntry<int> EventHeight { get; init; }

    public SettingEntry<float> ScrollSpeed { get; init; }

    public SettingEntry<ScrollingTextAreaCurve> Curve { get; init; }
}
