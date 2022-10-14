namespace Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RemappedSkillID
{
    public int OriginalID { get; set; }

    public int DestinationID { get; set; }

    public string Comment { get; set; }
}
