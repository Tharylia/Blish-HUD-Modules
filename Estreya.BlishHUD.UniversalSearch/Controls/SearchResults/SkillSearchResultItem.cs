namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.UniversalSearch.Controls.Tooltips;

public class SkillSearchResultItem : SearchResultItem
{
    private readonly IconService _iconState;
    private Skill _skill;

    public Skill Skill
    {
        get => this._skill;
        set
        {
            if (this.SetProperty(ref this._skill, value))
            {
                this.Icon = this._skill?.IconTexture ?? (AsyncTexture2D)ContentService.Textures.Error;
                this.Name = this._skill?.Name;
                this.Description = this._skill?.Description;
            }
        }
    }

    protected override string ChatLink => this.Skill?.ChatLink;

    public SkillSearchResultItem(IconService iconState) : base(iconState)
    {
        this._iconState = iconState;
    }

    protected override Tooltip BuildTooltip()
    {
        return new SkillTooltip(this.Skill, this._iconState);
    }
}
