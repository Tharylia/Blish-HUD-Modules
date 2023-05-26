namespace Estreya.BlishHUD.Shared.Models.ArcDPS.Buff;

using Blish_HUD.ArcDps.Models;
using CombatEvent = ArcDPS.CombatEvent;

public class BuffApplyCombatEvent : CombatEvent
{
    public BuffApplyCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(combatEvent, category, type, state)
    {
    }

    public override Ag Source => this.Src;

    public override Ag Destination => this.Dst;

    public int AppliedDuration => this.Ev.Value;
}