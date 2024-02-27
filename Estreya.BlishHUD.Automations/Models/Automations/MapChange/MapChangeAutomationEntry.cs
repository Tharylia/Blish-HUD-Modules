namespace Estreya.BlishHUD.Automations.Models.Automations.MapChange;

using Blish_HUD.Modules.Managers;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MapChangeAutomationEntry : AutomationEntry<MapChangeActionInput>
{
    public int FromMapId { get; private set; }
    public int ToMapId { get; private set; }

    public MapChangeAutomationEntry(string name, int fromMapId = -1, int toMapId = -1) : base(/*AutomationType.MAP_CHANGE,*/ name)
    {
        this.FromMapId = fromMapId;
        this.ToMapId = toMapId;
    }
}
