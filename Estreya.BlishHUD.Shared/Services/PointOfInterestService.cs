namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD.Modules.Managers;
using Extensions;
using Flurl.Http;
using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Models;
using Models.GW2API.PointOfInterest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class PointOfInterestService : FilesystemAPIService<PointOfInterest>
{
    public PointOfInterestService(APIServiceConfiguration configuration, Gw2ApiManager apiManager, string baseFolderPath, IFlurlClient flurlClient, string fileRootUrl) : base(apiManager, configuration, baseFolderPath, flurlClient, fileRootUrl) { }
    protected override string BASE_FOLDER_STRUCTURE => "pois";
    protected override string FILE_NAME => "pois.json";
    public List<PointOfInterest> PointOfInterests => this.APIObjectList;

    public PointOfInterest GetPointOfInterest(string chatCode)
    {
        using (this._apiObjectListLock.Lock())
        {
            foreach (PointOfInterest poi in this.APIObjectList)
            {
                if (poi.ChatLink == chatCode)
                {
                    return poi;
                }
            }

            return null;
        }
    }

    protected override async Task<List<PointOfInterest>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
    {
        List<PointOfInterest> pointOfInterests = new List<PointOfInterest>();

        // Continent 1 = Tyria
        // Continent 2 = Mists

        progress.Report("Loading continents...");
        IApiV2ObjectList<Continent> continents = await apiManager.Gw2ApiClient.V2.Continents.AllAsync(cancellationToken);

        foreach (ContinentDetails continent in continents.Select(x => new ContinentDetails(x)))
        {
            progress.Report($"Loading floors of continent \"{continent.Name}\" ...");
            IApiV2ObjectList<ContinentFloor> floors = await apiManager.Gw2ApiClient.V2.Continents[continent.Id].Floors.AllAsync(cancellationToken);

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