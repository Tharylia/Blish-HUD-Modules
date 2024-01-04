namespace Estreya.BlishHUD.ArcDPSLogManager.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum EncounterMode
{
    Unknown,
    /// <summary>
    /// The standard version of an encounter.
    /// </summary>
    Normal,
    /// <summary>
    /// A harder version of an encounter.
    /// </summary>
    Challenge,
    /// <summary>
    /// 1 stack of Emboldened (easy mode; extra stats).
    /// </summary>
    Emboldened1,
    /// <summary>
    /// 2 stacks of Emboldened (easy mode; extra stats).
    /// </summary>
    Emboldened2,
    /// <summary>
    /// 3 stacks of Emboldened (easy mode; extra stats).
    /// </summary>
    Emboldened3,
    /// <summary>
    /// 4 stacks of Emboldened (easy mode; extra stats).
    /// </summary>
    Emboldened4,
    /// <summary>
    /// 5 stacks of Emboldened (easy mode; extra stats).
    /// </summary>
    Emboldened5,
}
