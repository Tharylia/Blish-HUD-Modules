namespace Estreya.BlishHUD.Shared.Models.ArcDPS.Heal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class HealCombatEvent : CombatEvent
{
    public override Blish_HUD.ArcDps.Models.Ag Source => this.Src;

    public override Blish_HUD.ArcDps.Models.Ag Destination => this.Dst;

    public int Value => this.Ev.BuffDmg;

    public HealCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(combatEvent, category, type, state)
    {
    }
}
