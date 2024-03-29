﻿namespace Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;

using Converter;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;

public class ContinentDetails
{
    public ContinentDetails() { }

    public ContinentDetails(Continent continent)
    {
        this.Id = continent.Id;
        this.Name = continent.Name;
        this.ContinentDims = continent.ContinentDims;
        this.MinZoom = continent.MinZoom;
        this.MaxZoom = continent.MaxZoom;
    }

    /// <summary>
    ///     The continent id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The continent name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The continent dimensions.
    /// </summary>
    [JsonConverter(typeof(CoordinatesConverter))]
    public Coordinates2 ContinentDims { get; set; }

    /// <summary>
    ///     The minimum zoom level.
    /// </summary>
    public int MinZoom { get; set; }

    /// <summary>
    ///     The maximum zoom level.
    /// </summary>
    public int MaxZoom { get; set; }
}