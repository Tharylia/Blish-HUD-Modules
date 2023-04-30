﻿namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Blish_HUD.Content;
using Blish_HUD;

using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;

public class LandmarkSearchResultItem : SearchResultItem
{
    private const string POI_FILE = "https://render.guildwars2.com/file/25B230711176AB5728E86F5FC5F0BFAE48B32F6E/97461.png";
    private const string WAYPOINT_FILE = "https://render.guildwars2.com/file/32633AF8ADEA696A1EF56D3AE32D617B10D3AC57/157353.png";
    private const string VISTA_FILE = "https://render.guildwars2.com/file/A2C16AF497BA3A0903A0499FFBAF531477566F10/358415.png";
    private readonly IEnumerable<PointOfInterest> _waypoints;
    private readonly IconService _iconState;

    protected override string ChatLink => Landmark?.ChatLink;

    private PointOfInterest _landmark;
    public PointOfInterest Landmark
    {
        get => _landmark;
        set
        {
            if (SetProperty(ref _landmark, value))
            {
                if (_landmark != null)
                {
                    Icon = GetTextureForLandmarkAsync(_landmark);
                    Name = _landmark.Name;
                    Description = _landmark.ChatLink;
                }
            }
        }
    }

    public LandmarkSearchResultItem(IEnumerable<PointOfInterest> waypoints, IconService iconState) : base(iconState)
    {
        _waypoints = waypoints;
        this._iconState = iconState;
    }

    protected override Tooltip BuildTooltip()
    {
        var tooltip = new Tooltip() { CurrentControl = this };

        var detailsName = new Label()
        {
            Text = Name,
            Font = Content.DefaultFont16,
            Location = new Point(10, 10),
            Height = 11,
            TextColor = ContentService.Colors.Chardonnay,
            ShowShadow = true,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            VerticalAlignment = VerticalAlignment.Middle,
            Parent = tooltip,
        };

        var detailsHintCopyChatCode = new Label()
        {
            Text = "Left Click: Copy chat code to clipboard",// Strings.Common.Landmark_Details_CopyChatCode,
            Font = Content.DefaultFont16,
            Location = new Point(10, detailsName.Bottom + 5),
            TextColor = Color.White,
            ShowShadow = true,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Parent = tooltip,
        };

        var detailsClosestWaypointTitle = new Label()
        {
            Text = "Closest Waypoint",// Strings.Common.Landmark_Details_ClosestWaypoint,
            Font = Content.DefaultFont16,
            Location = new Point(10, detailsHintCopyChatCode.Bottom + 12),
            Height = 11,
            TextColor = ContentService.Colors.Chardonnay,
            ShadowColor = Color.Black,
            ShowShadow = true,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Parent = tooltip,
        };

        var closestWaypont = ClosestWaypoint();

        var detailsClosestWaypoint = new Label()
        {
            Text = closestWaypont.Map.Name + ": " + closestWaypont.Name,
            Font = Content.DefaultFont14,
            Location = new Point(10, detailsClosestWaypointTitle.Bottom + 5),
            TextColor = Color.White,
            ShadowColor = Color.Black,
            ShowShadow = true,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Parent = tooltip,
        };

        new Label()
        {
            Text = "Shift + Click: Copy closest waypoint to clipboard.",// Strings.Common.Landmark_Details_CopyClosestWaypoint,
            Font = Content.DefaultFont14,
            Location = new Point(10, detailsClosestWaypoint.Bottom + 5),
            TextColor = Color.White,
            ShadowColor = Color.Black,
            ShowShadow = true,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Visible = true,
            Parent = tooltip,
        };

        return tooltip;
    }

    protected override async Task ClickAction()
    {

        if (GameService.Input.Keyboard.ActiveModifiers == Microsoft.Xna.Framework.Input.ModifierKeys.Shift)
        {
            var clipboardResult = await ClipboardUtil.WindowsClipboardService.SetTextAsync(ClosestWaypoint().ChatLink);

            this.SignalClickActionExecuted(clipboardResult);
        }
        else
        {
            await base.ClickAction();
        }
    }

    private PointOfInterest ClosestWaypoint()
    {
        var distances = _waypoints.Select(waypoint => (Math.Sqrt(Math.Pow(Landmark.Coordinates.X - waypoint.Coordinates.X, 2) + Math.Pow(Landmark.Coordinates.Y - waypoint.Coordinates.Y, 2)), waypoint));
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
