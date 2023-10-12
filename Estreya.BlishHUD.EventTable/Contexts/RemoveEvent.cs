namespace Estreya.BlishHUD.EventTable.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct RemoveEvent
{
    public RemoveEvent()
    {
    }

    public string CategoryKey { get; set; } = null;
    public string EventKey { get; set; } = null;
}
