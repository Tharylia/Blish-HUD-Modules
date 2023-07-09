namespace Estreya.BlishHUD.EventTable.Models;

using Shared.Attributes;

public enum MenuEventSortMode
{
    [Translation("menuEventSortMode-default", "Default")]
    Default,

    [Translation("menuEventSortMode-alphabetical", "Alphabetical (A-Z)")]
    Alphabetical,

    [Translation("menuEventSortMode-alphabeticalDesc", "Alphabetical (Z-A)")]
    AlphabeticalDesc
}