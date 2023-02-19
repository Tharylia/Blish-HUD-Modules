namespace Estreya.BlishHUD.EventTable.Controls
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.EventTable.Models;

    public class EventDetailsButton : DataDetailsButton<Models.Event>
    {
        public Models.Event Event
        {
            get => base.Data;
            set => base.Data = value;
        }
    }
}
