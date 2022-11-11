namespace Estreya.BlishHUD.Shared.Models.ArcDPS.Buff;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BuffApplyCombatEvent : CombatEvent
{
    public override Blish_HUD.ArcDps.Models.Ag Source => this.Src;

    public override Blish_HUD.ArcDps.Models.Ag Destination => this.Dst;

    public int AppliedDuration => this.Ev.Value;

    public BuffApplyCombatEvent(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(ev, src, dst, category, type, state)
    {
    }
}
