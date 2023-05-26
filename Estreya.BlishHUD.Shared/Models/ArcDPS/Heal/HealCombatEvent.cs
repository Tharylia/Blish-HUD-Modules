namespace Estreya.BlishHUD.Shared.Models.ArcDPS.Heal;

using Blish_HUD.ArcDps.Models;
using CombatEvent = ArcDPS.CombatEvent;

public class HealCombatEvent : CombatEvent
{
    public HealCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(combatEvent, category, type, state)
    {
    }

    public override Ag Source => this.Src;

    public override Ag Destination => this.Dst;

    public int Value => this.Ev.BuffDmg;
}