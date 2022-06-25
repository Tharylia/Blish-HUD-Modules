namespace Estreya.BlishHUD.Shared.Models.ArcDPS;

using Blish_HUD.ArcDps.Models;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Estreya.BlishHUD.Shared.State;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

public class CombatEvent
{
    private readonly SkillState _skillState;

    public Ev Ev { get; }
    public Ag Src { get; }

    public Ag Dst { get; }
    public CombatEventCategory Category { get; }
    public CombatEventType Type { get; }
    public Skill Skill { get; private set; }
    public Texture2D SkillTexture { get; private set; }

    public CombatEvent(Ev ev, Ag src, Ag dst, CombatEventCategory category, CombatEventType type, SkillState skillState)
    {
        this.Ev = ev;
        this.Src = src;
        this.Dst = dst;
        this.Category = category;
        this.Type = type;
        this._skillState = skillState;

        this.Skill = this._skillState.GetById((int)this.Ev.SkillId);
    }
}
