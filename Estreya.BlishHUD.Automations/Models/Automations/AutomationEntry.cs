namespace Estreya.BlishHUD.Automations.Models.Automations;

using Blish_HUD.Modules.Managers;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class AutomationEntry
{
    //public AutomationType Type { get; private set; }

    public string Name { get; private set; }

    /// <summary>
    ///     Defines the count how often the entry should be executed before being removed. -1 is unlimited.
    /// </summary>
    public int ExecutionCount = -1;

    public AutomationEntry(/*AutomationType type, */string name)
    {
        //this.Type = type;
        this.Name = name;
    }
}
