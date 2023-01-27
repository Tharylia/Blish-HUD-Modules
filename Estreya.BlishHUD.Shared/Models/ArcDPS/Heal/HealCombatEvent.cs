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

    public int Value => this.Ev.Value;

    public HealCombatEvent(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(ev, src, dst, category, type, state)
    {
    }
}
