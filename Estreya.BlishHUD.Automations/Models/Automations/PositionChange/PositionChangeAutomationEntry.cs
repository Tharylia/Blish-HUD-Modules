namespace Estreya.BlishHUD.Automations.Models.Automations.PositionChange;

using Blish_HUD.Modules.Managers;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class PositionChangeAutomationEntry : AutomationEntry<PositionChangeActionInput>
{
    public PositionChangeAutomationEntry(string name) : base(name)
    {
    }
}
