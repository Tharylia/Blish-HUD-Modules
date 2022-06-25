namespace Estreya.BlishHUD.ScrollingCombatText.Models;

using Estreya.BlishHUD.Shared.Models.ArcDPS;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ScrollingTextAreaConfiguration
{
    public string Name { get; init; }

    public List<CombatEventType> Types { get; init; }

    public List<CombatEventCategory> Categories { get; init; }

    public Point Location { get; init; }

    public Point Size { get; init; }

    public int EventHeight { get; init; } = -1;

    public float ScrollSpeed { get; init; } = 1f;

    public ScrollingTextAreaCurve Curve { get; init; } = ScrollingTextAreaCurve.Straight;
}
