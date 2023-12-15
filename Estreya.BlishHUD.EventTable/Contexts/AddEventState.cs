namespace Estreya.BlishHUD.EventTable.Contexts;

using System;
using static Estreya.BlishHUD.EventTable.Services.EventStateService;

public struct AddEventState
{
    public AddEventState()
    {
    }

    public string AreaName = null;
    public string EventKey = null;

    public EventStates State = EventStates.Hidden;

    public DateTime Until = DateTime.UtcNow;
}
