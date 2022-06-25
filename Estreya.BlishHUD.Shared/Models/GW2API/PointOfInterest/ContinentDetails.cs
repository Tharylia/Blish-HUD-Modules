namespace Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;

using Blish_HUD.Controls.Extern;
using Estreya.BlishHUD.Shared.Models.GW2API.Converter;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ContinentDetails
{
    /// <summary>
    /// The continent id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The continent name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The continent dimensions.
    /// </summary>
    [JsonConverter(typeof(CoordinatesConverter))]
    public Coordinates2 ContinentDims { get; set; }

    /// <summary>
    /// The minimum zoom level.
    /// </summary>
    public int MinZoom { get; set; }

    /// <summary>
    /// The maximum zoom level.
    /// </summary>
    public int MaxZoom { get; set; }

    public ContinentDetails() { }

    public ContinentDetails(Continent continent)
    {
        this.Id = continent.Id;
        this.Name = continent.Name;
        this.ContinentDims = continent.ContinentDims;
        this.MinZoom = continent.MinZoom;
        this.MaxZoom = continent.MaxZoom;
    }
}
