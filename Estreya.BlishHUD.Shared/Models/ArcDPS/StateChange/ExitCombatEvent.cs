namespace Estreya.BlishHUD.Shared.Models.ArcDPS.StateChange;

using Blish_HUD.ArcDps.Models;
using CombatEvent = ArcDPS.CombatEvent;

public class ExitCombatEvent : CombatEvent
{
    public ExitCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(combatEvent, category, type, state)
    {
    }

    /// <summary>
    ///     The agent that left combat
    /// </summary>
    public override Ag Source => this.Src;

    /// <summary>
    ///     Not applicable.
    /// </summary>
    public override Ag Destination => null;
}