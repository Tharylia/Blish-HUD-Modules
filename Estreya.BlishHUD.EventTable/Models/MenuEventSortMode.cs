namespace Estreya.BlishHUD.EventTable.Models;

using Estreya.BlishHUD.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum MenuEventSortMode
{
    [Translation("menuEventSortMode-default","Default")]
    Default,
    [Translation("menuEventSortMode-alphabetical", "Alphabetical (A-Z)")]
    Alphabetical,
    [Translation("menuEventSortMode-alphabeticalDesc", "Alphabetical (Z-A)")]
    AlphabeticalDesc
}
