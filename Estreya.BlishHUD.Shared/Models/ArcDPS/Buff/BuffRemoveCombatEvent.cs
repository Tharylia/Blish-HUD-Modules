namespace Estreya.BlishHUD.Shared.Models.ArcDPS.Buff;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BuffRemoveCombatEvent : CombatEvent
{
    public override Blish_HUD.ArcDps.Models.Ag Source => this.Dst;

    public override Blish_HUD.ArcDps.Models.Ag Destination => this.Source;

    public int RemainingTimeRemovedAsDuration => this.Ev.Value;

    public int RemainingTimeRemovedAsIntensity => this.Ev.BuffDmg;

    public int StacksRemoved => this.Ev.Result;

    public BuffRemoveCombatEvent(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(ev, src, dst, category, type, state)
    {
    }
}
