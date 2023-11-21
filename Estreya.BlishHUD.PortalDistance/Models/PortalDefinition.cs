namespace Estreya.BlishHUD.PortalDistance.Models;

using Gw2Sharp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PortalDefinition
{
    public int SkillID { get; set; }
    public float MaxDistance { get; set; }

    public PortalDefinition(int skillId, float maxDistance)
    {
        this.SkillID = skillId;
        this.MaxDistance = maxDistance;
    }
}
