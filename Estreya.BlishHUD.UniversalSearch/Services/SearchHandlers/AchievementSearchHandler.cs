namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;

using Controls.SearchResults;
using Gw2Sharp.WebApi.V2.Models;
using Models;
using Shared.Services;
using System.Collections.Generic;

public class AchievementSearchHandler : SearchHandler<Achievement>
{
    private readonly IconService _iconService;

    public AchievementSearchHandler(IEnumerable<Achievement> searchItems, SearchHandlerConfiguration configuration, IconService iconService) : base(searchItems, configuration)
    {
        this._iconService = iconService;
    }

    public override string Prefix => "a";

    protected override SearchResultItem CreateSearchResultItem(Achievement item)
    {
        return new AchievementSearchResultItem(this._iconService) { Achievement = item };
    }

    protected override string GetSearchableProperty(Achievement item)
    {
        return item.Name;
    }

    protected override bool IsBroken(Achievement item)
    {
        return string.IsNullOrWhiteSpace(item.Name);
    }
}