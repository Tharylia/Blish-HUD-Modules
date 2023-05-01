namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;

using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Estreya.BlishHUD.UniversalSearch.Models;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AchievementSearchHandler : SearchHandler<Achievement>
{
    private readonly IconService _iconService;

    public AchievementSearchHandler(IEnumerable<Achievement> searchItems, SearchHandlerConfiguration configuration, IconService iconService) : base(searchItems, configuration)
    {
        this._iconService = iconService;
    }

    public override string Prefix => "a";

    protected override SearchResultItem CreateSearchResultItem(Achievement item) => new AchievementSearchResultItem(this._iconService) { Achievement = item };

    protected override string GetSearchableProperty(Achievement item) => item.Name;

    protected override bool IsBroken(Achievement item)
    {
        return string.IsNullOrWhiteSpace(item.Name);
    }
}
