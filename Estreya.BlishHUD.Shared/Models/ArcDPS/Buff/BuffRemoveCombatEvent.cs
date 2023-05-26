namespace Estreya.BlishHUD.Shared.Models.ArcDPS.Buff;

using Blish_HUD.ArcDps.Models;
using CombatEvent = ArcDPS.CombatEvent;

public class BuffRemoveCombatEvent : CombatEvent
{
    public BuffRemoveCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(combatEvent, category, type, state)
    {
    }

    public override Ag Source => this.Dst;

    public override Ag Destination => this.Source;

    public int RemainingTimeRemovedAsDuration => this.Ev.Value;

    public int RemainingTimeRemovedAsIntensity => this.Ev.BuffDmg;

    public int StacksRemoved => this.Ev.Result;
}