namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Estreya.BlishHUD.UniversalSearch.Models;
using Gw2Sharp.WebApi.V2.Models;
using System.Collections.Generic;
using System.Linq;

public class LandmarkSearchHandler : SearchHandler<PointOfInterest>
{
    private readonly IconService _iconState;

    public override string Prefix => "l";

    public LandmarkSearchHandler(IEnumerable<PointOfInterest> pointOfInterests, SearchHandlerConfiguration configuration, IconService iconState) : base(pointOfInterests, configuration)
    {
        this._iconState = iconState;
    }

    protected override SearchResultItem CreateSearchResultItem(PointOfInterest item)
    {
        var possibleWaypoints = this.SearchItems.Where(x => x.Map == item.Map && x.Type == PoiType.Waypoint);
        // For the case where a landmark exists only in an instance where no waypoint is, just take the closest waypoint from all waypoints
        if (!possibleWaypoints.Any())
        {
            possibleWaypoints = this.SearchItems.Where(x => x.Type == PoiType.Waypoint);
        }

        return new LandmarkSearchResultItem(possibleWaypoints, this._iconState) { Landmark = item };
    }

    protected override string GetSearchableProperty(PointOfInterest item)
    {
        return item.Name;
    }

    protected override bool IsBroken(PointOfInterest item)
    {
        return string.IsNullOrWhiteSpace(item.Name);
    }
}
