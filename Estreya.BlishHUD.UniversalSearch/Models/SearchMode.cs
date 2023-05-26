namespace Estreya.BlishHUD.UniversalSearch.Models;

using System.ComponentModel;

public enum SearchMode
{
    [Description("Any")] Any,

    [Description("Starts with")] StartsWith,

    [Description("Contains")] Contains,

    [Description("Levenshtein")] Levenshtein
}