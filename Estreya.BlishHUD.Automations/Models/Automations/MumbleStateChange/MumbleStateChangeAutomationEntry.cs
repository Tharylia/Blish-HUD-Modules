namespace Estreya.BlishHUD.Automations.Models.Automations.MumbleStateChange;

using Blish_HUD.Modules.Managers;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class MumbleStateChangeAutomationEntry : AutomationEntry<MumbleStateChangeActionInput>
{
    public MumbleStateChangeAutomationEntry(string name) : base(name)
    {
    }
}
