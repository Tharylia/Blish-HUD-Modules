namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Estreya.BlishHUD.UniversalSearch.Models;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TraitSearchHandler : SearchHandler<Trait>
{
    private readonly IconService _iconState;

    public override string Prefix => "t";

    public TraitSearchHandler(IEnumerable<Trait> traits, SearchHandlerConfiguration configuration, IconService iconState) : base(traits, configuration)
    {
        this._iconState = iconState;
    }

    protected override SearchResultItem CreateSearchResultItem(Trait item)
        => new TraitSearchResultItem(this._iconState) { Trait = item };

    protected override string GetSearchableProperty(Trait item)
        => item.Name;

    protected override bool IsBroken(Trait item)
    {
        return string.IsNullOrWhiteSpace(item.Name);
    }
}
