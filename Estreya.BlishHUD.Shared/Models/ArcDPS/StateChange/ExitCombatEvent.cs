namespace Estreya.BlishHUD.Shared.Models.ArcDPS.StateChange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ExitCombatEvent : CombatEvent
{
    /// <summary>
    /// The agent that left combat
    /// </summary>
    public override Blish_HUD.ArcDps.Models.Ag Source => this.Src;

    /// <summary>
    /// Not applicable.
    /// </summary>
    public override Blish_HUD.ArcDps.Models.Ag Destination => null;

    public ExitCombatEvent(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(ev, src, dst, category, type, state)
    {
    }
}
