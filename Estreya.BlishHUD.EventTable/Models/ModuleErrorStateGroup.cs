namespace Estreya.BlishHUD.EventTable.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ModuleErrorStateGroup : Shared.Modules.ModuleErrorStateGroup
{
    protected ModuleErrorStateGroup(string group) : base(group)
    {
    }

    public static ModuleErrorStateGroup LOADING_EVENTS = new ModuleErrorStateGroup("loading-events");
}
