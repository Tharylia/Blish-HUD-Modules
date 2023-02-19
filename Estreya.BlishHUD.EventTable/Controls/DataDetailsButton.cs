namespace Estreya.BlishHUD.EventTable.Controls
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.EventTable.Models;

    public class DataDetailsButton<T> : DetailsButton
    {
        public T Data { get; set; }
    }
}
