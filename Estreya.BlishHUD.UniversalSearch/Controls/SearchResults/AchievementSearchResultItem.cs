namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.UniversalSearch.Controls.Tooltips;
using Gw2Sharp.WebApi.V2.Models;
using System;

public class AchievementSearchResultItem : SearchResultItem
{

    private Achievement _achievement;

    public Achievement Achievement
    {
        get => this._achievement;
        set
        {
            if (this.SetProperty(ref this._achievement, value))
            {
                if (this._achievement != null)
                {
                    this.Icon = this._achievement.Icon.Url?.AbsoluteUri != null ? this.IconService.GetIcon(this._achievement.Icon.Url.AbsoluteUri) : (AsyncTexture2D)ContentService.Textures.Error;
                    this.Name = this._achievement.Name;
                    this.Description = this._achievement.Description;
                }
            }
        }
    }

    public AchievementSearchResultItem(IconService iconState) : base(iconState)
    {
    }

    protected override string ChatLink => null;

    protected override Tooltip BuildTooltip()
    {
        return new AchievementTooltip(this.Achievement);
    }
}
