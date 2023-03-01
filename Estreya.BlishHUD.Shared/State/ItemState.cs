namespace Estreya.BlishHUD.Shared.State;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Models.GW2API.Items;
using Estreya.BlishHUD.Shared.Models.GW2API.PointOfInterest;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

public class ItemState : APIState<Item>
{
    private const string BASE_FOLDER_STRUCTURE = "items";
    private const string FILE_NAME = "items.json";
    private const string LAST_UPDATED_FILE_NAME = "last_updated.txt";

    private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";

    private readonly string _baseFolderPath;

    private string DirectoryPath => Path.Combine(this._baseFolderPath, BASE_FOLDER_STRUCTURE);

    private AsyncLock _itemLock = new AsyncLock();
    private List<Item> _items;
    public List<Item> Items => _items;

    public ItemState(APIStateConfiguration configuration, Gw2ApiManager apiManager, string baseFolderPath) : base(apiManager, configuration)
    {
        this._baseFolderPath = baseFolderPath;
        this.Updated += this.ItemState_Updated;
    }

    private void ItemState_Updated(object sender, EventArgs e)
    {
        using (_itemLock.Lock())
        {
            this._items = this.APIObjectList.ToArray().ToList();
        }
    }

    protected override Task DoInitialize()
    {
        using (_itemLock.Lock())
        {
            _items = new List<Item>();
        }

        return Task.CompletedTask;
    }

    protected override Task DoClear()
    {
        using (_itemLock.Lock())
        {
            _items.Clear();
        }

        return Task.CompletedTask;
    }

    protected override async Task Load()
    {
        try
        {
            bool canLoadFiles = this.CanLoadFiles();
            bool shouldLoadFiles = await this.ShouldLoadFiles();

            if (canLoadFiles)
            {
                try
                {
                    this.Loading = true;

                    var filePath = Path.Combine(this.DirectoryPath, FILE_NAME);
                    var itemJson = await FileUtil.ReadStringAsync(filePath);
                    var items = JsonConvert.DeserializeObject<List<Item>>(itemJson);
                    using (_itemLock.Lock())
                    {
                        this._items = items;
                    }

                }
                finally
                {
                    this.Loading = false;
                    this.SignalCompletion();
                }
            }

            // Refresh files after we loaded the prior saved
            if (!shouldLoadFiles)
            {
                await base.LoadFromAPI(!canLoadFiles); // Only reset completion if we could not load anything at start
                if (!this._cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await this.Save();
                }
            }

            Logger.Debug("Loaded {0} items.", this.APIObjectList.Count);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed loading items:");
        }
    }

    private bool CanLoadFiles()
    {
        var baseDirectoryExists = Directory.Exists(this.DirectoryPath);

        if (!baseDirectoryExists) return false;

        var savedFileExists = System.IO.File.Exists(Path.Combine(this.DirectoryPath, FILE_NAME));

        if (!savedFileExists) return false;

        return true;
    }

    private async Task<bool> ShouldLoadFiles()
    {
        if (!this.CanLoadFiles()) return false;

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
                var lastUpdatedUTC = new DateTime(lastUpdated.Ticks, DateTimeKind.Utc);
                return lastUpdatedUTC >= DateTime.ParseExact(Item.LAST_SCHEMA_CHANGE, "yyyy-MM-dd", CultureInfo.InvariantCulture) && DateTime.UtcNow - lastUpdatedUTC <= TimeSpan.FromDays(5);
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
            var itemJson = JsonConvert.SerializeObject(this.APIObjectList, Formatting.Indented);
            await FileUtil.WriteStringAsync(Path.Combine(this.DirectoryPath, FILE_NAME), itemJson);
        }

        await this.CreateLastUpdatedFile();
    }

    private async Task CreateLastUpdatedFile()
    {
        await FileUtil.WriteStringAsync(Path.Combine(this.DirectoryPath, LAST_UPDATED_FILE_NAME), DateTime.UtcNow.ToString(DATE_TIME_FORMAT));
    }

    public Item GetItemByName(string name)
    {
        if (!this._itemLock.IsFree()) return null;

        using (this._itemLock.Lock())
        {
            foreach (var item in this.Items)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }

            return null;
        }
    }
    public Item GetItemById(int id)
    {
        if (!this._itemLock.IsFree()) return null;

        using (this._itemLock.Lock())
        {
            foreach (var item in this.Items)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }

            return null;
        }
    }

    protected override async Task<List<Item>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress)
    {
        List<Item> items = new List<Item>();

        progress.Report("Load item ids...");

        var itemIds = await apiManager.Gw2ApiClient.V2.Items.IdsAsync(this._cancellationTokenSource.Token);

        Logger.Info($"Start loading items: {itemIds.First()} - {itemIds.Last()}");

        int chunkSize = 200;
        foreach (var itemIdChunk in itemIds.ChunkBy(chunkSize))
        {
            if (this._cancellationTokenSource.Token.IsCancellationRequested)
            {
                break;
            }

            try
            {
                try
                {
                    var itemChunk = await this.FetchChunk(apiManager, progress, itemIdChunk);

                    items.AddRange(itemChunk);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Failed loading items with chunk size {chunkSize}:");

                    chunkSize = 10;

                    Logger.Debug($"Try load failed chunk in smaller chunk size: {chunkSize}");

                    foreach (var smallerItemIdChunk in itemIdChunk.ChunkBy(chunkSize))
                    {
                        try
                        {
                            var itemChunk = await this.FetchChunk(apiManager, progress, smallerItemIdChunk);

                            items.AddRange(itemChunk);
                        }
                        catch (Exception smallerEx)
                        {
                            Logger.Warn(smallerEx, $"Failed loading items with chunk size {chunkSize}:");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed loading items:");
            }
        }

        return items;
    }

    private async Task<List<Item>> FetchChunk(Gw2ApiManager apiManager, IProgress<string> progress, IEnumerable<int> itemIdChunk)
    {
        string message = $"Start loading items by id: {itemIdChunk.First()} - {itemIdChunk.Last()}";

        progress.Report(message);
        Logger.Debug(message);

        var items = await apiManager.Gw2ApiClient.V2.Items.ManyAsync(itemIdChunk, this._cancellationTokenSource.Token);

        return items.Select(apiItem => Item.FromAPI(apiItem)).ToList();
    }

    protected override void DoUnload()
    {
        this.Updated -= this.ItemState_Updated;
    }
}
