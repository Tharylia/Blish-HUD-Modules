using Estreya.BlishHUD.EventTable.Models;

namespace Estreya.BlishHUD.EventTable.Contexts;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct AddEvent
{
    public AddEvent()
    {
    }

    public string CategoryKey { get; set; } = null;
    public string Key { get; set; } = null;
    public string Name { get; set; } = null;
    public string Icon { get; set; } = null;
    public TimeSpan Offset { get; set; } = TimeSpan.Zero;
    public TimeSpan Repeat { get; set; } = TimeSpan.Zero;
    public DateTime? StartingDate { get; set; } = null;
    public EventLocations Locations { get; set; } = new EventLocations();

    public int[] MapIds { get; set; } = null;

    public EventWaypoints Waypoints { get; set; } = new EventWaypoints();

    public string Wiki { get; set; } = null;

    public int Duration { get; set; } = 0;

    public string BackgroundColorCode { get; set; } = null;

    public string[] BackgroundColorGradientCodes { get; set; } = null;

    public APICodeType? APICodeType { get; set; } = null;

    public string APICode { get; set; } = null;

    public bool Filler { get; set; } = false;

    public List<DateTime> Occurences { get; set; } = null;

    public TimeSpan[] ReminderTimes { get; set; } =
    {
        TimeSpan.FromMinutes(10)
    };
}
