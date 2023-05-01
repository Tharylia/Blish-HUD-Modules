namespace Estreya.BlishHUD.Shared.Services;

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

public class ItemService : FilesystemAPIService<Item>
{
    protected override string BASE_FOLDER_STRUCTURE => "items";
    protected override string FILE_NAME => "items.json";
    public List<Item> Items => this.APIObjectList;

    public ItemService(APIServiceConfiguration configuration, Gw2ApiManager apiManager, string baseFolderPath) : base(apiManager, configuration, baseFolderPath)
    {
    }

    public Item GetItemByName(string name)
    {
        if (!this._apiObjectListLock.IsFree()) return null;

        using (this._apiObjectListLock.Lock())
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
        if (!this._apiObjectListLock.IsFree()) return null;

        using (this._apiObjectListLock.Lock())
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
}
