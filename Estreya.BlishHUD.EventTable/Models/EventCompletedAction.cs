namespace Estreya.BlishHUD.EventTable.Models
{
    using System.ComponentModel;

    public enum EventCompletedAction
    {
        Crossout,
        Hide,
        [Description("Change Opacity")]
        ChangeOpacity,
        [Description("Crossout & Change Opacity")]
        CrossoutAndChangeOpacity
    }
}
