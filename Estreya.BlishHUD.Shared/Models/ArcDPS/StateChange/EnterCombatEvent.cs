namespace Estreya.BlishHUD.Shared.Models.ArcDPS.StateChange;

using Blish_HUD.ArcDps.Models;
using CombatEvent = ArcDPS.CombatEvent;

public class EnterCombatEvent : CombatEvent
{
    public EnterCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type, CombatEventState state) : base(combatEvent, category, type, state)
    {
    }

    /// <summary>
    ///     The agent that entered combat
    /// </summary>
    public override Ag Source => this.Src;

    /// <summary>
    ///     Not applicable.
    /// </summary>
    public override Ag Destination => null;

    /// <summary>
    ///     The subgroup of <see cref="Source" />.
    /// </summary>
    public ulong Subgroup => this.Dst.Id;
}