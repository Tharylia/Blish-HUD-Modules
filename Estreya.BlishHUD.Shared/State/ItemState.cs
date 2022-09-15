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

public class ItemState : APIState<Item>
{
    private const string BASE_FOLDER_STRUCTURE = "items";
    private const string FILE_NAME = "items.json";
    private const string LAST_UPDATED_FILE_NAME = "last_updated.txt";

    private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";

    private readonly string _baseFolderPath;

    private string DirectoryPath => Path.Combine(this._baseFolderPath, BASE_FOLDER_STRUCTURE);

    public ItemState(APIStateConfiguration configuration, Gw2ApiManager apiManager, string baseFolderPath) : base(apiManager, configuration)
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
                var filePath = Path.Combine(this.DirectoryPath, FILE_NAME);
                var itemJson = await FileUtil.ReadStringAsync(filePath);
                var items = JsonConvert.DeserializeObject<List<Item>>(itemJson);
                using (await this._apiObjectListLock.LockAsync())
                {
                    this.APIObjectList.AddRange(items);
                }
            }

            Logger.Debug("Loaded {0} items.", this.APIObjectList.Count);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed loading items:");
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
        if (this.Loading)
        {
            return null;
        }

        using (this._apiObjectListLock.Lock())
        {
            foreach (var item in this.APIObjectList)
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
        if (this.Loading)
        {
            return null;
        }

        using (this._apiObjectListLock.Lock())
        {
            foreach (var item in this.APIObjectList)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }

            return null;
        }
    }

    protected override async Task<List<Item>> Fetch(Gw2ApiManager apiManager)
    {
        List<Item> items = new List<Item>();

        var itemIds = await apiManager.Gw2ApiClient.V2.Items.IdsAsync(this._cancellationTokenSource.Token);

        Logger.Info($"Start loading items: {itemIds.First()} - {itemIds.Last()}");

        foreach (var itemIdChunk in itemIds.ChunkBy(200))
        {
            try
            {
                Logger.Debug($"Start loading items by id: {itemIdChunk.First()} - {itemIdChunk.Last()}");

                var itemChunk = await apiManager.Gw2ApiClient.V2.Items.ManyAsync(itemIdChunk, this._cancellationTokenSource.Token);

                items.AddRange(itemChunk);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed loading items:");
            }
        }

        return items;
    }
}
