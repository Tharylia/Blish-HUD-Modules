namespace Estreya.BlishHUD.Shared.Models.ArcDPS;

using Blish_HUD.ArcDps.Models;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Estreya.BlishHUD.Shared.State;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

public abstract class CombatEvent
{
    protected Ev Ev { get; private set; }
    protected Ag Src { get; private set; }

    protected Ag Dst { get; private set; }

    public abstract Ag Source { get; }

    public abstract Ag Destination { get; }

    public uint SkillId => this.Ev.SkillId;

    public CombatEventCategory Category { get; }
    public CombatEventType Type { get; }
    public CombatEventState State { get; }
    public Skill Skill { get; set; }

    public CombatEvent(Ev ev, Ag src, Ag dst, CombatEventCategory category, CombatEventType type, CombatEventState state)
    {
        this.Ev = ev;
        this.Src = src;
        this.Dst = dst;
        this.Category = category;
        this.Type = type;
        this.State = state;
    }

    public void Dispose()
    {
        this.Ev = null;
        this.Src = null;
        this.Dst = null;
        this.Skill = null; // Don't dispose as its held by skill state
    }

    public static CombatEventState GetState(Ev ev)
    {
        if (ev == null)
            throw new ArgumentNullException(nameof(ev), "Ev can't be null.");

        if (ev.IsStateChange != Blish_HUD.ArcDps.ArcDpsEnums.StateChange.None)
        {
            return CombatEventState.STATECHANGE;
        }

        if (ev.IsStateChange == Blish_HUD.ArcDps.ArcDpsEnums.StateChange.None && ev.IsActivation != Blish_HUD.ArcDps.ArcDpsEnums.Activation.None)
        {
            return CombatEventState.ACTIVATION;
        }

        if (ev.IsStateChange == Blish_HUD.ArcDps.ArcDpsEnums.StateChange.None && ev.IsActivation == Blish_HUD.ArcDps.ArcDpsEnums.Activation.None && ev.IsBuffRemove != Blish_HUD.ArcDps.ArcDpsEnums.BuffRemove.None && ev.Buff)
        {
            return CombatEventState.BUFFREMOVE;
        }

        if (ev.IsStateChange == Blish_HUD.ArcDps.ArcDpsEnums.StateChange.None && ev.IsActivation == Blish_HUD.ArcDps.ArcDpsEnums.Activation.None && ev.IsBuffRemove == Blish_HUD.ArcDps.ArcDpsEnums.BuffRemove.None)
        {
            // Can be buff apply, buff damage or direct damage

            return ev.Buff && ev.BuffDmg == 0 && ev.Value != 0 ? CombatEventState.BUFFAPPLY : CombatEventState.NORMAL;
        }

        throw new ArgumentOutOfRangeException(nameof(ev), "Event state invalid.");
    }
}
