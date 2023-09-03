namespace Estreya.BlishHUD.Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum FontFace
{
    [Description("Menomonia - Guild Wars 2 UI Font")]
    Menomonia,

    [Description("GWTwoFont - Guild Wars 2 Title font")]
    GWTwoFont,

    [Description("Anonymous - Monospace")]
    Anonymous,

    [Description("Elizabeth - Substitute to Eason Pro Inline Caps")]
    Elizabeth,

    //[Description("StoweOpenFace - Substitute to Eason Pro Inline Caps")]
    //StoweOpenFace,

    //[Description("StoweTitling - Substitute to Eason Pro")]
    //StoweTitling,

    [Description("PTSerif - Substitute to Eason Pro")]
    PTSerif,

    [Description("Lato - Substitute to Cronos")]
    Lato,

    [Description("Cagliostro - Substitute to Cronos")]
    Cagliostro,

    [Description("New Krytan Typeface")]
    NewKrytan,

    [Description("Ascalonian Typeface")]
    Ascalonian,

    [Description("Canthan Ideogram")]
    CanthanIdeogram,

    [Description("Asuran Script")]
    Asuran,

    Roboto,
    OpenSans,

    Custom
}
