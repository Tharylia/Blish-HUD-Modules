namespace Estreya.BlishHUD.Shared.Models.ArcDPS.Shield;

using Blish_HUD.ArcDps.Models;
using CombatEvent = ArcDPS.CombatEvent;

public class BarrierCombatEvent : CombatEvent
{
    public BarrierCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(combatEvent, category, type, state)
    {
    }

    public override Ag Source => this.Src;

    public override Ag Destination => this.Dst;

    public uint Value => this.Ev.OverStackValue;
}