namespace Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;

using Estreya.BlishHUD.Shared.Models.GW2API.Converter;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ContinentFloorRegionDetails
{
    /// <summary>
    /// The region id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The region name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The region label coordinates.
    /// </summary>
    [JsonConverter(typeof(CoordinatesConverter))]
    public Coordinates2 LabelCoord { get; set; }

    /// <summary>
    /// The region continent rectangle.
    /// </summary>
    [JsonConverter(typeof(TopDownRectangleConverter))]
    public Rectangle ContinentRect { get; set; }

    public ContinentFloorRegionDetails() { }

    public ContinentFloorRegionDetails(ContinentFloorRegion continentFloorRegion)
    {
        this.Id = continentFloorRegion.Id;
        this.Name = continentFloorRegion.Name;
        this.LabelCoord = continentFloorRegion.LabelCoord;
        this.ContinentRect = continentFloorRegion.ContinentRect;
    }
}
