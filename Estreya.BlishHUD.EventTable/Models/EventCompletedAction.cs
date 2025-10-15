namespace Estreya.BlishHUD.EventTable.Models;

using Shared.Attributes;

public enum EventCompletedAction
{
    [Translation("eventCompletedAction-none", "None")]
    None = 0,

    [Translation("eventCompletedAction-crossout", "Crossout")]
    Crossout = 1,

    [Translation("eventCompletedAction-hide", "Hide")]
    Hide = 2,

    [Translation("eventCompletedAction-changeOpacity", "Change Opacity")]
    ChangeOpacity = 3,

    [Translation("eventCompletedAction-crossoutAndChangeOpacity", "Crossout & Change Opacity")]
    CrossoutAndChangeOpacity = 4
}