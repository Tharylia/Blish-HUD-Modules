namespace Estreya.BlishHUD.UniversalSearch.Controls.Tooltips;

using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Shared.Models.GW2API.PointOfInterest;

public class LandmarkTooltip : Tooltip
{
    public LandmarkTooltip(PointOfInterest pointOfInterest, PointOfInterest closestWaypoint)
    {
        Label detailsName = new Label
        {
            Text = pointOfInterest.Name,
            Font = Content.DefaultFont16,
            Location = new Point(10, 10),
            Height = 11,
            TextColor = ContentService.Colors.Chardonnay,
            ShowShadow = true,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            VerticalAlignment = VerticalAlignment.Middle,
            Parent = this
        };

        Label detailsHintCopyChatCode = new Label
        {
            Text = "Left Click: Copy chat code to clipboard", // Strings.Common.Landmark_Details_CopyChatCode,
            Font = Content.DefaultFont16,
            Location = new Point(10, detailsName.Bottom + 5),
            TextColor = Color.White,
            ShowShadow = true,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Parent = this
        };

        Label detailsClosestWaypointTitle = new Label
        {
            Text = "Closest Waypoint", // Strings.Common.Landmark_Details_ClosestWaypoint,
            Font = Content.DefaultFont16,
            Location = new Point(10, detailsHintCopyChatCode.Bottom + 12),
            Height = 11,
            TextColor = ContentService.Colors.Chardonnay,
            ShadowColor = Color.Black,
            ShowShadow = true,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Parent = this
        };

        Label detailsClosestWaypoint = new Label
        {
            Text = closestWaypoint.Map.Name + ": " + closestWaypoint.Name,
            Font = Content.DefaultFont14,
            Location = new Point(10, detailsClosestWaypointTitle.Bottom + 5),
            TextColor = Color.White,
            ShadowColor = Color.Black,
            ShowShadow = true,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Parent = this
        };

        new Label
        {
            Text = "Shift + Click: Copy closest waypoint to clipboard.", // Strings.Common.Landmark_Details_CopyClosestWaypoint,
            Font = Content.DefaultFont14,
            Location = new Point(10, detailsClosestWaypoint.Bottom + 5),
            TextColor = Color.White,
            ShadowColor = Color.Black,
            ShowShadow = true,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Visible = true,
            Parent = this
        };
    }
}