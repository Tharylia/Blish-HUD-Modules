namespace Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;

using Converter;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;

public class ContinentFloorRegionMapDetails
{
    public ContinentFloorRegionMapDetails() { }

    public ContinentFloorRegionMapDetails(ContinentFloorRegionMap continentFloorRegionMap)
    {
        this.Id = continentFloorRegionMap.Id;
        this.Name = continentFloorRegionMap.Name;
        this.MinLevel = continentFloorRegionMap.MinLevel;
        this.MaxLevel = continentFloorRegionMap.MaxLevel;
        this.DefaultFloor = continentFloorRegionMap.DefaultFloor;
        this.LabelCoord = continentFloorRegionMap.LabelCoord;
        this.MapRect = continentFloorRegionMap.MapRect;
        this.ContinentRect = continentFloorRegionMap.ContinentRect;
    }

    /// <summary>
    ///     The map id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The map name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The map minimum level.
    /// </summary>
    public int MinLevel { get; set; }

    /// <summary>
    ///     The map maximum level.
    /// </summary>
    public int MaxLevel { get; set; }

    /// <summary>
    ///     The default floor for this map.
    /// </summary>
    public int DefaultFloor { get; set; }

    /// <summary>
    ///     The map label coordinates.
    /// </summary>
    [JsonConverter(typeof(CoordinatesConverter))]
    public Coordinates2 LabelCoord { get; set; }

    /// <summary>
    ///     The map rectangle.
    /// </summary>
    [JsonConverter(typeof(BottomUpRectangleConverter))]
    public Rectangle MapRect { get; set; }

    /// <summary>
    ///     The map continent rectangle.
    /// </summary>
    [JsonConverter(typeof(TopDownRectangleConverter))]
    public Rectangle ContinentRect { get; set; }
}