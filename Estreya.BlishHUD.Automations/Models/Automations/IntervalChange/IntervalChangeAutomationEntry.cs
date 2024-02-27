namespace Estreya.BlishHUD.Automations.Models.Automations.IntervalChange;

using Blish_HUD.Modules.Managers;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class IntervalChangeAutomationEntry : AutomationEntry<IntervalChangeActionInput>
{
    public IntervalChangeAutomationEntry(string name) : base(name)
    {
    }
}
