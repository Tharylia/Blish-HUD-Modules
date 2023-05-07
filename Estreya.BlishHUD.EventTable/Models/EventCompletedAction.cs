namespace Estreya.BlishHUD.EventTable.Models
{
    using Estreya.BlishHUD.Shared.Attributes;
    using System.ComponentModel;

    public enum EventCompletedAction
    {
        [Translation("eventCompletedAction-crossout", "Crossout")]
        Crossout,

        [Translation("eventCompletedAction-hide", "Hide")]
        Hide,

        [Translation("eventCompletedAction-changeOpacity", "Change Opacity")]
        ChangeOpacity,

        [Translation("eventCompletedAction-crossoutAndChangeOpacity", "Crossout & Change Opacity")]
        CrossoutAndChangeOpacity
    }
}
