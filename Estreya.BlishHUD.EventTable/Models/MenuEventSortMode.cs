namespace Estreya.BlishHUD.EventTable.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum MenuEventSortMode
{
    [Description("Default")]
    Default,
    [Description("Alphabetical (A-Z)")]
    Alphabetical,
    [Description("Alphabetical (Z-A)")]
    AlphabeticalDesc
}
