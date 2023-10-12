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

    public string CategoryKey { get; set; }
    public string Key { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public TimeSpan Offset { get; set; } = TimeSpan.Zero;
    public TimeSpan Repeat { get; set; } = TimeSpan.Zero;
    public DateTime? StartingDate { get; set; }
    public string Location { get; set; }

    public int[] MapIds { get; set; }

    public string Waypoint { get; set; }

    public string Wiki { get; set; }

    public int Duration { get; set; }

    public string BackgroundColorCode { get; set; }

    public string[] BackgroundColorGradientCodes { get; set; }

    public APICodeType? APICodeType { get; set; }

    public string APICode { get; set; }

    public bool Filler { get; set; }

    public List<DateTime> Occurences { get; set; }

    public TimeSpan[] ReminderTimes { get; set; } =
    {
        TimeSpan.FromMinutes(10)
    };
}
