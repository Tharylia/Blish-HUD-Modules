namespace Estreya.BlishHUD.UniversalSearch.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum SearchMode
{
    [Description("Any")]
    Any,

    [Description("Starts with")]
    StartsWith,

    [Description("Contains")]
    Contains,

    [Description("Levenshtein")]
    Levenshtein,
}
