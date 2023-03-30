namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;

using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class LandmarkSearchHandler : SearchHandler<PointOfInterest>
{
    private HashSet<PointOfInterest> _landmarks = new HashSet<PointOfInterest>();
    private readonly IconState _iconState;

    public override string Name => "Landmarks";// Strings.Common.SearchHandler_Landmarks;

    public override string Prefix => "l";

    public LandmarkSearchHandler(List<PointOfInterest> pointOfInterests, IconState iconState)
    {
        this._landmarks = pointOfInterests.ToHashSet();
        this._iconState = iconState;
    }

    protected override HashSet<PointOfInterest> SearchItems => _landmarks;

    protected override SearchResultItem CreateSearchResultItem(PointOfInterest item)
    {
        var possibleWaypoints = _landmarks.Where(x => x.Map == item.Map && x.Type == PoiType.Waypoint);
        // For the case where a landmark exists only in an instance where no waypoint is, just take the closest waypoint from all waypoints
        if (!possibleWaypoints.Any())
        {
            possibleWaypoints = _landmarks.Where(x => x.Type == PoiType.Waypoint);
        }

        return new LandmarkSearchResultItem(possibleWaypoints, this._iconState) { Landmark = item };
    }

    protected override string GetSearchableProperty(PointOfInterest item)
        => item.Name;

    public override void UpdateSearchItems(List<PointOfInterest> items)
    {
        _landmarks = items.ToHashSet();
    }
}
