namespace Estreya.BlishHUD.Shared.Models.ArcDPS.StateChange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class StateChangeCombatEvent : CombatEvent
{
    public Blish_HUD.ArcDps.ArcDpsEnums.StateChange StateChange => this.Ev.IsStateChange;

    public StateChangeCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(combatEvent, category, type, state)
    {
    }
}
