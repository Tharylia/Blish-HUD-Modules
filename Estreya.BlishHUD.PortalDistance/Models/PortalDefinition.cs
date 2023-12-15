namespace Estreya.BlishHUD.PortalDistance.Models;

using Gw2Sharp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PortalDefinition
{
    public int SkillID { get; }
    public Func<float> GetMaxDistance { get; }

    public PortalDefinition(int skillId, Func<float> getMaxDistance)
    {
        this.SkillID = skillId;
        this.GetMaxDistance = getMaxDistance;
    }
}
