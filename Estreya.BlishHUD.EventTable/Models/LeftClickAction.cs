namespace Estreya.BlishHUD.EventTable.Models;

using Estreya.BlishHUD.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum LeftClickAction
{
    [Translation("leftClickAction-none","None")]
    None,

    [Translation("leftClickAction-copyWaypoint", "Copy Waypoint")]
    CopyWaypoint,

    [Translation("leftClickAction-navigateToWaypoint", "Navigate to Waypoint")]
    NavigateToWaypoint
}
