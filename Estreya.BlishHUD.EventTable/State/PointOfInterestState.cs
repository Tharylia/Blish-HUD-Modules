namespace Estreya.BlishHUD.EventTable.State;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Extensions;
using Estreya.BlishHUD.EventTable.Helpers;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.EventTable.Models.GW2API;
using Estreya.BlishHUD.EventTable.Utils;
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
    private static readonly Logger Logger = Logger.GetLogger<PointOfInterestState>();
    private const string BASE_FOLDER_STRUCTURE = "pois";
    private const string LAST_UPDATED_FILE_NAME = "last_updated.txt";

    private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";

    private readonly string _baseFolderPath;

    private string FullPath => Path.Combine(this._baseFolderPath, BASE_FOLDER_STRUCTURE);

    public bool Loading { get; private set; }

    // Don't await loading as this can take a long time.

    public PointOfInterestState(Gw2ApiManager apiManager, string baseFolderPath) : base(apiManager, updateInterval: Timeout.InfiniteTimeSpan, awaitLoad: false) // Don't save in interval, must be manually triggered
    {
        this._baseFolderPath = baseFolderPath;

        this.FetchAction = async (apiManager) =>
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
        };
    }

    protected override async Task Load()
    {
        lock (this)
        {
            this.Loading = true;
        }

        try
        {
            bool loadFromApi = false;

            if (Directory.Exists(this.FullPath))
            {
                bool continueLoadingFiles = true;

                string lastUpdatedFilePath = Path.Combine(this.FullPath, LAST_UPDATED_FILE_NAME);
                if (!System.IO.File.Exists(lastUpdatedFilePath))
                {
                    await this.CreateLastUpdatedFile();
                }

                string dateString = await FileUtil.ReadStringAsync(lastUpdatedFilePath);
                if (!DateTime.TryParseExact(dateString, DATE_TIME_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastUpdated))
                {
                    Logger.Debug("Failed parsing last updated.");
                }
                else
                {
                    if (DateTime.UtcNow - new DateTime(lastUpdated.Ticks, DateTimeKind.Utc) > TimeSpan.FromDays(5))
                    {
                        continueLoadingFiles = false;
                        loadFromApi = true;
                    }
                }

                if (continueLoadingFiles)
                {
                    var dirs = Directory.GetDirectories(this.FullPath).ToList();

                    string[] files = dirs.SelectMany(dir => Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)).ToArray();

                    if (files.Length > 0)
                    {
                        await this.LoadFromFiles(files);
                    }
                    else
                    {
                        loadFromApi = true;
                    }
                }
            }
            else
            {
                loadFromApi = true;
            }

            if (loadFromApi)
            {
                await base.Load();
                await this.Save();
            }

            Logger.Debug("Loaded {0} point of interests.", this.APIObjectList.Count);
        }catch (Exception ex)
        {
            Logger.Warn(ex, "Failed loading point of interests:");
        }
        finally
        {
            lock (this)
            {
                this.Loading = false;
            }
        }
    }

    private async Task LoadFromFiles(string[] files)
    {
        List<Task<string>> loadTasks = files.ToList().Select(file =>
        {
            if (!System.IO.File.Exists(file))
            {
                Logger.Warn("Could not find file \"{0}\"", file);
                return Task.FromResult((string)null);
            }

            return FileUtil.ReadStringAsync(file);
        }).ToList();

        _ = await Task.WhenAll(loadTasks);

        using (await this._listLock.LockAsync())
        {
            foreach (Task<string> loadTask in loadTasks)
            {
                string result = loadTask.Result;

                if (string.IsNullOrWhiteSpace(result))
                {
                    continue;
                }

                try
                {
                    PointOfInterest poi = JsonConvert.DeserializeObject<PointOfInterest>(result);

                    this.APIObjectList.Add(poi);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Could not parse poi: {0}", result.Replace("\n", "").Replace("\r", ""));
                }
            }
        }
    }

    protected override async Task Save()
    {
        if (Directory.Exists(this.FullPath))
        {
            Directory.Delete(this.FullPath, true);
        }

        _ = Directory.CreateDirectory(this.FullPath);

        using (await this._listLock.LockAsync())
        {
            IEnumerable<Task> fileWriteTasks = this.APIObjectList.Select(poi =>
            {
                string landmarkPath = Path.Combine(this.FullPath, FileUtil.SanitizeFileName(poi.Continent.Name), FileUtil.SanitizeFileName(poi.Floor.Id.ToString()), FileUtil.SanitizeFileName(poi.Region.Name), FileUtil.SanitizeFileName(poi.Map.Name), FileUtil.SanitizeFileName(poi.Name) + ".txt");

                _ = Directory.CreateDirectory(Path.GetDirectoryName(landmarkPath));

                string landmarkData = JsonConvert.SerializeObject(poi, Formatting.Indented);

                return FileUtil.WriteStringAsync(landmarkPath, landmarkData);
            });

            await Task.WhenAll(fileWriteTasks);
        }

        await this.CreateLastUpdatedFile();
    }

    private async Task CreateLastUpdatedFile()
    {
        await FileUtil.WriteStringAsync(Path.Combine(this.FullPath, LAST_UPDATED_FILE_NAME), DateTime.UtcNow.ToString(DATE_TIME_FORMAT));
    }

    public PointOfInterest GetPointOfInterest(string chatCode)
    {
        using (this._listLock.Lock())
        {
            IEnumerable<PointOfInterest> foundPointOfInterests = this.APIObjectList.Where(wp => wp.ChatLink == chatCode);

            return foundPointOfInterests.Any() ? foundPointOfInterests.First() : null;
        }
    }

    public override Task DoClear() => Task.CompletedTask;

    protected override void DoUnload() { /* NOOP */ }
}
