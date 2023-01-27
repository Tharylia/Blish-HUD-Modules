namespace Estreya.BlishHUD.Shared.Models.ArcDPS.StateChange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class StateChangeCombatEvent : CombatEvent
{
    public Blish_HUD.ArcDps.ArcDpsEnums.StateChange StateChange => this.Ev.IsStateChange;

    public StateChangeCombatEvent(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(ev, src, dst, category, type, state)
    {
    }
}
