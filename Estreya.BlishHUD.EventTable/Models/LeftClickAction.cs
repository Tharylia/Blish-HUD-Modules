namespace Estreya.BlishHUD.EventTable.Models;

using Shared.Attributes;

public enum LeftClickAction
{
    [Translation("leftClickAction-none", "None")]
    None,

    [Translation("leftClickAction-copyWaypoint", "Copy Waypoint")]
    CopyWaypoint,

    [Translation("leftClickAction-navigateToWaypoint", "Navigate to Waypoint")]
    NavigateToWaypoint
}