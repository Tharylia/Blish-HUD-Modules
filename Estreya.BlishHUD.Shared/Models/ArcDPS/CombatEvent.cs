namespace Estreya.BlishHUD.Shared.Models.ArcDPS;

using Blish_HUD.ArcDps.Models;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Estreya.BlishHUD.Shared.State;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

public class CombatEvent
{
    public Ev Ev { get; private set; }
    public Ag Src { get; private set; }

    public Ag Dst { get; private set; }
    public CombatEventCategory Category { get; }
    public CombatEventType Type { get; }
    public CombatEventGroup Group { get; }
    public Skill Skill { get; set; }

    public CombatEvent(Ev ev, Ag src, Ag dst, CombatEventCategory category, CombatEventType type, CombatEventGroup group)
    {
        this.Ev = ev;
        this.Src = src;
        this.Dst = dst;
        this.Category = category;
        this.Type = type;
        this.Group = group;
    }

    public void Dispose()
    {
        this.Ev = null;
        this.Src = null;
        this.Dst = null;
        this.Skill = null; // Don't dispose as its held by skill state
    }
}
