namespace Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;

using Estreya.BlishHUD.Shared.Models.GW2API.Converter;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;

public class PointOfInterest
{

    /// <summary>
    /// The point of interest id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The point of interest name.
    /// If it has no name, this value is <see langword="null"/>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The point of interest coordinates.
    /// </summary>
    [JsonConverter(typeof(CoordinatesConverter))]
    public Coordinates2 Coordinates { get; set; }

    /// <summary>
    /// The point of interest type.
    /// </summary>
    public PoiType Type { get; set; }

    /// <summary>
    /// The point of interest chat link.
    /// </summary>
    public string ChatLink { get; set; } = string.Empty;

    /// <summary>
    /// The point of interest icon.
    /// If the point of interest has no icon, this value is <see langword="null"/>.
    /// </summary>
    public RenderUrl? Icon { get; set; }

    public ContinentDetails Continent { get; set; }

    /// <summary>
    /// The floor this point of interest is on.
    /// </summary>
    public ContinentFloorDetails Floor { get; set; }
    public ContinentFloorRegionDetails Region { get; set; }
    public ContinentFloorRegionMapDetails Map { get; set; }

    public PointOfInterest() { }

    public PointOfInterest(ContinentFloorRegionMapPoi poi)
    {
        this.Id = poi.Id;
        this.Name = poi.Name;
        this.Coordinates = poi.Coord;
        this.Type = poi.Type;
        this.ChatLink = poi.ChatLink;
        this.Icon = poi.Icon;
        this.Type = poi.Type.Value;
    }

    public static implicit operator ContinentFloorRegionMapPoi(PointOfInterest poi)
    {
        ContinentFloorRegionMapPoi mapPoi = new ContinentFloorRegionMapPoi
        {
            Id = poi.Id,
            Name = poi.Name,
            Coord = poi.Coordinates,
            Type = poi.Type,
            ChatLink = poi.ChatLink,
            Icon = poi.Icon,
            Floor = poi.Floor?.Id ?? 0
        };

        return mapPoi;
    }

    public override string ToString()
    {
        return $"Continent: {this.Continent?.Name ?? "Unknown"} - Map: {this.Map?.Name ?? "Unknown"} - Region: {this.Region?.Name ?? "Unknown"} - Floor: {this.Floor?.Id.ToString() ?? "Unknown"} - Name: {this.Name}";
    }
}
