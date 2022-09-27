namespace Estreya.BlishHUD.Shared.State;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
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
using System.Threading;
using System.Threading.Tasks;

public class PointOfInterestState : APIState<PointOfInterest>
{
    private const string BASE_FOLDER_STRUCTURE = "pois";
    private const string FILE_NAME = "pois.json";
    private const string LAST_UPDATED_FILE_NAME = "last_updated.txt";

    private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";

    private readonly string _baseFolderPath;

    private string DirectoryPath => Path.Combine(this._baseFolderPath, BASE_FOLDER_STRUCTURE);

    public PointOfInterestState(APIStateConfiguration configuration, Gw2ApiManager apiManager, string baseFolderPath) : base(apiManager, configuration)
    {
        this._baseFolderPath = baseFolderPath;
    }

    protected override async Task Load()
    {
        try
        {
            bool shouldLoadFiles = await this.ShouldLoadFiles();

            if (!shouldLoadFiles)
            {
                await base.Load();
                await this.Save();
            }
            else
            {
                try
                {
                    this.Loading = true;

                    var filePath = Path.Combine(this.DirectoryPath, FILE_NAME);
                    var poiJson = await FileUtil.ReadStringAsync(filePath);
                    var pois = JsonConvert.DeserializeObject<List<PointOfInterest>>(poiJson);
                    using (await this._apiObjectListLock.LockAsync())
                    {
                        this.APIObjectList.AddRange(pois);
                    }
                }
                finally
                {
                    this.Loading = false;
                    this.SignalCompletion();
                }
            }

            Logger.Debug("Loaded {0} point of interests.", this.APIObjectList.Count);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed loading point of interests:");
        }
    }

    private async Task<bool> ShouldLoadFiles()
    {
        var baseDirectoryExists = Directory.Exists(this.DirectoryPath);

        if (!baseDirectoryExists) return false;

        var savedFileExists = System.IO.File.Exists(Path.Combine(this.DirectoryPath, FILE_NAME));

        if (!savedFileExists) return false;

        string lastUpdatedFilePath = Path.Combine(this.DirectoryPath, LAST_UPDATED_FILE_NAME);
        if (System.IO.File.Exists(lastUpdatedFilePath))
        {
            string dateString = await FileUtil.ReadStringAsync(lastUpdatedFilePath);
            if (!DateTime.TryParseExact(dateString, DATE_TIME_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastUpdated))
            {
                this.Logger.Debug("Failed parsing last updated.");
                return false;
            }
            else
            {
                return DateTime.UtcNow - new DateTime(lastUpdated.Ticks, DateTimeKind.Utc) <= TimeSpan.FromDays(5);
            }
        }

        return false;
    }

    protected override async Task Save()
    {
        if (Directory.Exists(this.DirectoryPath))
        {
            Directory.Delete(this.DirectoryPath, true);
        }

        _ = Directory.CreateDirectory(this.DirectoryPath);

        using (await this._apiObjectListLock.LockAsync())
        {
            var poiJson = JsonConvert.SerializeObject(this.APIObjectList, Formatting.Indented);
            await FileUtil.WriteStringAsync(Path.Combine(this.DirectoryPath, FILE_NAME), poiJson);
        }

        await this.CreateLastUpdatedFile();
    }

    private async Task CreateLastUpdatedFile()
    {
        await FileUtil.WriteStringAsync(Path.Combine(this.DirectoryPath, LAST_UPDATED_FILE_NAME), DateTime.UtcNow.ToString(DATE_TIME_FORMAT));
    }

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

        Gw2Sharp.WebApi.V2.IApiV2ObjectList<Continent> continents = await apiManager.Gw2ApiClient.V2.Continents.AllAsync();

        foreach (ContinentDetails continent in continents.Select(x => new ContinentDetails(x)))
        {
            Gw2Sharp.WebApi.V2.IApiV2ObjectList<ContinentFloor> floors = await apiManager.Gw2ApiClient.V2.Continents[continent.Id].Floors.AllAsync();

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
