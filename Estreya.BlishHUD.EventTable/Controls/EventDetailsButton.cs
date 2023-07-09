namespace Estreya.BlishHUD.EventTable.Controls;

public class EventDetailsButton : DataDetailsButton<Models.Event>
{
    public Models.Event Event
    {
        get => this.Data;
        set => this.Data = value;
    }
}