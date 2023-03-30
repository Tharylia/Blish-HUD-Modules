namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TraitSearchHandler : SearchHandler<Trait>
{
    private HashSet<Trait> _traits = new HashSet<Trait>();
    private readonly IconState _iconState;

    public override string Name => "Traits";// Strings.Common.SearchHandler_Traits;

    public override string Prefix => "t";

    protected override HashSet<Trait> SearchItems => _traits;

    public TraitSearchHandler(List<Trait> traits, IconState iconState)
    {
        _traits = traits.ToHashSet();
        this._iconState = iconState;
    }

    protected override SearchResultItem CreateSearchResultItem(Trait item)
        => new TraitSearchResultItem(this._iconState) { Trait = item };

    protected override string GetSearchableProperty(Trait item)
        => item.Name;

    public override void UpdateSearchItems(List<Trait> items)
    {
        _traits = items.ToHashSet();
    }
}
