namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.IO;
using Estreya.BlishHUD.Shared.Json.Converter;
using Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;
using Estreya.BlishHUD.Shared.Utils;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class PointOfInterestService : FilesystemAPIService<PointOfInterest>
{
    protected override string BASE_FOLDER_STRUCTURE => "pois";
    protected override string FILE_NAME => "pois.json";
    public List<PointOfInterest> PointOfInterests => this.APIObjectList;

    public PointOfInterestService(APIServiceConfiguration configuration, Gw2ApiManager apiManager, string baseFolderPath) : base(apiManager, configuration, baseFolderPath)    {    }    

    public PointOfInterest GetPointOfInterest(string chatCode)
    {
        using (this._apiObjectListLock.Lock())
        {
            foreach (var poi in this.APIObjectList)
            {
                if (poi.ChatLink == chatCode)
                {
                    return poi;
                }
            }

            return null;
        }
    }

    protected override async Task<List<PointOfInterest>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress)
    {
        List<PointOfInterest> pointOfInterests = new List<PointOfInterest>();

        // Continent 1 = Tyria
        // Continent 2 = Mists

        progress.Report("Loading continents...");
        Gw2Sharp.WebApi.V2.IApiV2ObjectList<Continent> continents = await apiManager.Gw2ApiClient.V2.Continents.AllAsync();

        foreach (ContinentDetails continent in continents.Select(x => new ContinentDetails(x)))
        {
            progress.Report($"Loading floors of continent \"{continent.Name}\" ...");
            Gw2Sharp.WebApi.V2.IApiV2ObjectList<ContinentFloor> floors = await apiManager.Gw2ApiClient.V2.Continents[continent.Id].Floors.AllAsync();

            progress.Report($"Parsing floors of continent \"{continent.Name}\" ...");
            foreach (ContinentFloor floor in floors)
            {
                ContinentFloorDetails floorDetails = new ContinentFloorDetails(floor);

                foreach (ContinentFloorRegion region in floor.Regions.Values)
                {
                    ContinentFloorRegionDetails regionDetails = new ContinentFloorRegionDetails(region);

                    foreach (ContinentFloorRegionMap map in region.Maps.Values)
                    {
                        ContinentFloorRegionMapDetails mapDetails = new ContinentFloorRegionMapDetails(map);

                        foreach (ContinentFloorRegionMapPoi pointOfInterest in map.PointsOfInterest.Values.Where(poi => poi.Name != null))
                        {
                            PointOfInterest landmark = new PointOfInterest(pointOfInterest)
                            {
                                Continent = continent,
                                Floor = floorDetails,
                                Region = regionDetails,
                                Map = mapDetails
                            };

                            pointOfInterests.Add(landmark);
                        }
                    }
                }
            }
        }

        return pointOfInterests.DistinctBy(poi => new { poi.Name }).ToList();
    }
}
