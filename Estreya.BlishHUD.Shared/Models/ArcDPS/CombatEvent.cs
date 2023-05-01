namespace Estreya.BlishHUD.Shared.Models.ArcDPS;

using Blish_HUD.ArcDps.Models;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Estreya.BlishHUD.Shared.Services;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

public abstract class CombatEvent
{
    public Blish_HUD.ArcDps.Models.CombatEvent RawCombatEvent { get; private set; }

    protected Ev Ev => this.RawCombatEvent?.Ev;
    protected Ag Src => this.RawCombatEvent?.Src;

    protected Ag Dst => this.RawCombatEvent?.Dst;

    public abstract Ag Source { get; }

    public abstract Ag Destination { get; }

    public uint SkillId => this.Ev?.SkillId ?? 0;

    public CombatEventCategory Category { get; }
    public CombatEventType Type { get; }
    public CombatEventState State { get; }
    public Skill Skill { get; set; }

    public CombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state)
    {
        this.RawCombatEvent = combatEvent;
        this.Category = category;
        this.Type = type;
        this.State = state;
    }

    public void Dispose()
    {
        this.RawCombatEvent = null;
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

        if (ev.IsStateChange == Blish_HUD.ArcDps.ArcDpsEnums.StateChange.BuffInitial) return CombatEventState.BUFFAPPLY;

        if (ev.IsStateChange == Blish_HUD.ArcDps.ArcDpsEnums.StateChange.None && ev.IsActivation == Blish_HUD.ArcDps.ArcDpsEnums.Activation.None && ev.IsBuffRemove == Blish_HUD.ArcDps.ArcDpsEnums.BuffRemove.None)
        {
            // Can be buff apply, buff damage or direct damage

            return ev.Buff && ev.BuffDmg == 0 && ev.Value != 0 ? CombatEventState.BUFFAPPLY : CombatEventState.NORMAL;
        }

        throw new ArgumentOutOfRangeException(nameof(ev), "Event state invalid.");
    }
}
