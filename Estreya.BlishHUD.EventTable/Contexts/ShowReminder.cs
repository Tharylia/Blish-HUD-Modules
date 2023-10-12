namespace Estreya.BlishHUD.EventTable.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct ShowReminder
{
    public ShowReminder()
    {
    }

    public string Title { get; set; } = null;
    public string Message { get; set; } = null;
    public string Icon { get; set; } = null;
}
