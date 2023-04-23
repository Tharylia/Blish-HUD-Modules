namespace Estreya.BlishHUD.Shared.Models.ArcDPS.Damage;
public class DamageCombatEvent : CombatEvent
{
    public override Blish_HUD.ArcDps.Models.Ag Source => this.Src;

    public override Blish_HUD.ArcDps.Models.Ag Destination => this.Dst;

    public bool IsBuffDamage => this.Ev.Buff;

    public int Value => this.IsBuffDamage ? this.Ev.BuffDmg : this.Ev.Value;

    public DamageCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(combatEvent, category, type, state)
    {
    }
}
