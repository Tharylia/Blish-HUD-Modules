namespace Estreya.BlishHUD.Shared.Models.ArcDPS.Damage;

using Blish_HUD.ArcDps.Models;
using CombatEvent = ArcDPS.CombatEvent;

public class DamageCombatEvent : CombatEvent
{
    public DamageCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(combatEvent, category, type, state)
    {
    }

    public override Ag Source => this.Src;

    public override Ag Destination => this.Dst;

    public bool IsBuffDamage => this.Ev.Buff;

    public int Value => this.IsBuffDamage ? this.Ev.BuffDmg : this.Ev.Value;
}