namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Blish_HUD.Content;
using Blish_HUD;

using Estreya.BlishHUD.UniversalSearch.Controls.Tooltips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;

public class SkillSearchResultItem : SearchResultItem
{
    private readonly IconState _iconState;
    private Skill _skill;

    public Skill Skill
    {
        get => _skill;
        set
        {
            if (SetProperty(ref _skill, value))
            {
                if (_skill != null)
                {
                    Icon = _skill.Icon != null ? this._iconState.GetIcon(_skill.Icon) : (AsyncTexture2D)ContentService.Textures.Error;
                    Name = _skill.Name;
                    Description = _skill.Description;
                }
            }
        }
    }

    protected override string ChatLink => Skill?.ChatLink;

    public SkillSearchResultItem(IconState iconState) : base(iconState)
    {
        this._iconState = iconState;
    }

    protected override Tooltip BuildTooltip()
    {
        return new SkillTooltip(this.Skill, this._iconState);
    }
}
