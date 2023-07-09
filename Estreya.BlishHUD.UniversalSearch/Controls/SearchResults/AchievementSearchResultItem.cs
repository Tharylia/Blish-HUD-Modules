namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;

using Blish_HUD;
using Blish_HUD.Controls;
using Gw2Sharp.WebApi.V2.Models;
using Shared.Services;
using Tooltips;

public class AchievementSearchResultItem : SearchResultItem
{
    private Achievement _achievement;

    public AchievementSearchResultItem(IconService iconState) : base(iconState)
    {
    }

    public Achievement Achievement
    {
        get => this._achievement;
        set
        {
            if (this.SetProperty(ref this._achievement, value))
            {
                if (this._achievement != null)
                {
                    this.Icon = this._achievement.Icon.Url?.AbsoluteUri != null ? this.IconService.GetIcon(this._achievement.Icon.Url.AbsoluteUri) : ContentService.Textures.Error;
                    this.Name = this._achievement.Name;
                    this.Description = this._achievement.Description;
                }
            }
        }
    }

    protected override string ChatLink => null;

    protected override Tooltip BuildTooltip()
    {
        return new AchievementTooltip(this.Achievement);
    }
}