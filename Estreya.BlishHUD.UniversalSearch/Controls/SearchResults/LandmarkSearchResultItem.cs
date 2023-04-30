namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.UniversalSearch.Controls.Tooltips;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class LandmarkSearchResultItem : SearchResultItem
{
    private const string POI_FILE = "https://render.guildwars2.com/file/25B230711176AB5728E86F5FC5F0BFAE48B32F6E/97461.png";
    private const string WAYPOINT_FILE = "https://render.guildwars2.com/file/32633AF8ADEA696A1EF56D3AE32D617B10D3AC57/157353.png";
    private const string VISTA_FILE = "https://render.guildwars2.com/file/A2C16AF497BA3A0903A0499FFBAF531477566F10/358415.png";
    private readonly IEnumerable<PointOfInterest> _waypoints;
    private readonly IconService _iconState;

    protected override string ChatLink => this.Landmark?.ChatLink;

    private PointOfInterest _landmark;
    public PointOfInterest Landmark
    {
        get => this._landmark;
        set
        {
            if (this.SetProperty(ref this._landmark, value))
            {
                if (this._landmark != null)
                {
                    this.Icon = this.GetTextureForLandmarkAsync(this._landmark);
                    this.Name = this._landmark.Name;
                    this.Description = this._landmark.ChatLink;
                }
            }
        }
    }

    public LandmarkSearchResultItem(IEnumerable<PointOfInterest> waypoints, IconService iconState) : base(iconState)
    {
        this._waypoints = waypoints;
        this._iconState = iconState;
    }

    protected override Tooltip BuildTooltip()
    {
        return new LandmarkTooltip(this.Landmark, this.ClosestWaypoint());
    }

    protected override async Task ClickAction()
    {
        if (GameService.Input.Keyboard.ActiveModifiers == Microsoft.Xna.Framework.Input.ModifierKeys.Shift)
        {
            var clipboardResult = await ClipboardUtil.WindowsClipboardService.SetTextAsync(this.ClosestWaypoint().ChatLink);

            this.SignalClickActionExecuted(clipboardResult);
        }
        else
        {
            await base.ClickAction();
        }
    }

    private PointOfInterest ClosestWaypoint()
    {
        var distances = this._waypoints.Select(waypoint => (Math.Sqrt(Math.Pow(this.Landmark.Coordinates.X - waypoint.Coordinates.X, 2) + Math.Pow(this.Landmark.Coordinates.Y - waypoint.Coordinates.Y, 2)), waypoint));
        return distances.OrderBy(x => x.Item1).First().waypoint;
    }


    private AsyncTexture2D GetTextureForLandmarkAsync(ContinentFloorRegionMapPoi landmark)
    {
        string imgUrl = string.Empty;

        switch (landmark.Type.Value)
        {
            case PoiType.Landmark:
                imgUrl = POI_FILE;
                break;
            case PoiType.Waypoint:
                imgUrl = WAYPOINT_FILE;
                break;
            case PoiType.Vista:
                imgUrl = VISTA_FILE;
                break;
            case PoiType.Unknown:
            case PoiType.Unlock:
                if (!string.IsNullOrEmpty(landmark.Icon?.Url?.AbsoluteUri))
                {
                    imgUrl = landmark.Icon;
                }
                else
                {
                    return ContentService.Textures.Error;
                }

                break;
        }

        return this._iconState.GetIcon(imgUrl);
    }
}
