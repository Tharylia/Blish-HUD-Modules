namespace Estreya.BlishHUD.Shared.Models.ArcDPS;

using Blish_HUD.ArcDps;
using Blish_HUD.ArcDps.Models;
using GW2API.Skills;
using System;

public abstract class CombatEvent
{
    public CombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state)
    {
        this.RawCombatEvent = combatEvent;
        this.Category = category;
        this.Type = type;
        this.State = state;
    }

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

    public void Dispose()
    {
        this.RawCombatEvent = null;
        this.Skill = null; // Don't dispose as its held by skill state
    }

    public static CombatEventState GetState(Ev ev)
    {
        if (ev == null)
        {
            throw new ArgumentNullException(nameof(ev), "Ev can't be null.");
        }

        if (ev.IsStateChange != ArcDpsEnums.StateChange.None)
        {
            return CombatEventState.STATECHANGE;
        }

        if (ev.IsStateChange == ArcDpsEnums.StateChange.None && ev.IsActivation != ArcDpsEnums.Activation.None)
        {
            return CombatEventState.ACTIVATION;
        }

        if (ev.IsStateChange == ArcDpsEnums.StateChange.None && ev.IsActivation == ArcDpsEnums.Activation.None && ev.IsBuffRemove != ArcDpsEnums.BuffRemove.None && ev.Buff)
        {
            return CombatEventState.BUFFREMOVE;
        }

        if (ev.IsStateChange == ArcDpsEnums.StateChange.BuffInitial)
        {
            return CombatEventState.BUFFAPPLY;
        }

        if (ev.IsStateChange == ArcDpsEnums.StateChange.None && ev.IsActivation == ArcDpsEnums.Activation.None && ev.IsBuffRemove == ArcDpsEnums.BuffRemove.None)
        {
            // Can be buff apply, buff damage or direct damage

            return ev.Buff && ev.BuffDmg == 0 && ev.Value != 0 ? CombatEventState.BUFFAPPLY : CombatEventState.NORMAL;
        }

        throw new ArgumentOutOfRangeException(nameof(ev), "Event state invalid.");
    }
}