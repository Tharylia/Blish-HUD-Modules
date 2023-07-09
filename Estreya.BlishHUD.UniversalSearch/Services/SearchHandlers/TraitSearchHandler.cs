namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;

using Controls.SearchResults;
using Gw2Sharp.WebApi.V2.Models;
using Models;
using Shared.Services;
using System.Collections.Generic;

public class TraitSearchHandler : SearchHandler<Trait>
{
    private readonly IconService _iconState;

    public TraitSearchHandler(IEnumerable<Trait> traits, SearchHandlerConfiguration configuration, IconService iconState) : base(traits, configuration)
    {
        this._iconState = iconState;
    }

    public override string Prefix => "t";

    protected override SearchResultItem CreateSearchResultItem(Trait item)
    {
        return new TraitSearchResultItem(this._iconState) { Trait = item };
    }

    protected override string GetSearchableProperty(Trait item)
    {
        return item.Name;
    }

    protected override bool IsBroken(Trait item)
    {
        return string.IsNullOrWhiteSpace(item.Name);
    }
}