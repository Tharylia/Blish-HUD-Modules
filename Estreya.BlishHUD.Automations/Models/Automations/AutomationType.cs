namespace Estreya.BlishHUD.Automations.Models.Automations;

using Estreya.BlishHUD.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum AutomationType
{
    [Translation("automationType-mapChange", "Map Change")]
    MAP_CHANGE,

    [Translation("automationType-interval", "Interval")]
    INTERVAL
}
