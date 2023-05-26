namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;

using Blish_HUD;
using Blish_HUD.Controls;
using Shared.Models.GW2API.Skills;
using Shared.Services;
using Tooltips;

public class SkillSearchResultItem : SearchResultItem
{
    private readonly IconService _iconState;
    private Skill _skill;

    public SkillSearchResultItem(IconService iconState) : base(iconState)
    {
        this._iconState = iconState;
    }

    public Skill Skill
    {
        get => this._skill;
        set
        {
            if (this.SetProperty(ref this._skill, value))
            {
                this.Icon = this._skill?.IconTexture ?? ContentService.Textures.Error;
                this.Name = this._skill?.Name;
                this.Description = this._skill?.Description;
            }
        }
    }

    protected override string ChatLink => this.Skill?.ChatLink;

    protected override Tooltip BuildTooltip()
    {
        return new SkillTooltip(this.Skill, this._iconState);
    }
}