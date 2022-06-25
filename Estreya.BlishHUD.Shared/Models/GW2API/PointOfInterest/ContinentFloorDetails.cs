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

 public class ContinentFloorDetails
{
    /// <summary>
    /// The floor id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The floor texture dimensions.
    /// </summary>
    [JsonConverter(typeof(CoordinatesConverter))]
    public Coordinates2 TextureDims { get; set; }

    /// <summary>
    /// The floor map rectangle that represent valid floor coordinates.
    /// </summary>
    [JsonConverter(typeof(TopDownRectangleConverter))]
    public Rectangle ClampedView { get; set; }

    public ContinentFloorDetails() { }

    public ContinentFloorDetails(ContinentFloor continentFloor)
    {
        this.Id = continentFloor.Id;
        this.TextureDims = continentFloor.TextureDims;
        this.ClampedView = continentFloor.ClampedView;
    }
}
