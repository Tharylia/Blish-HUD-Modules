namespace Estreya.BlishHUD.Shared.Models.ArcDPS.Damage;
public class DamageCombatEvent : CombatEvent
{
    public override Blish_HUD.ArcDps.Models.Ag Source => this.Src;

    public override Blish_HUD.ArcDps.Models.Ag Destination => this.Dst;

    public bool IsBuffDamage => this.Ev.Buff;

    public int Value => this.IsBuffDamage ? this.Ev.BuffDmg : this.Ev.Value;

    public DamageCombatEvent(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(ev, src, dst, category, type, state)
    {
    }
}
