namespace Estreya.BlishHUD.Shared.Models.ArcDPS.StateChange;

using Blish_HUD.ArcDps;

public abstract class StateChangeCombatEvent : CombatEvent
{
    public StateChangeCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(combatEvent, category, type, state)
    {
    }

    public ArcDpsEnums.StateChange StateChange => this.Ev.IsStateChange;
}