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

    protected override async Task<List<Item>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
    {
        List<Item> items = new List<Item>();

        progress.Report("Load item ids...");

        var itemIds = await apiManager.Gw2ApiClient.V2.Items.IdsAsync(cancellationToken);

        progress.Report($"Loading items... 0/{itemIds.Count}");
        Logger.Info($"Start loading items: {itemIds.First()} - {itemIds.Last()}");

        int loadedItems = 0;
        int chunkSize = 200;
        var chunks = itemIds.ChunkBy(chunkSize);

        var chunkGroups = chunks.ChunkBy(20);

        foreach (var chunkGroup in chunkGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Load a group in parallel
            var tasks = new List<Task<List<Item>>>();

            foreach (var idChunk in chunkGroup)
            {
                cancellationToken.ThrowIfCancellationRequested();

                tasks.Add(this.FetchChunk(apiManager, idChunk, cancellationToken)
                    .ContinueWith(resultTask =>
                    {
                        var resultItems = resultTask.IsFaulted ? new List<Item>() : resultTask.Result;

                        var newCount = Interlocked.Add(ref loadedItems, resultItems.Count);
                        progress.Report($"Loading items... {newCount}/{itemIds.Count}");
                        return resultItems;
                    }));
            }

            var itemGroup = await Task.WhenAll(tasks);
            var fetchedItems = itemGroup.SelectMany(i => i).ToList();

            items.AddRange(fetchedItems);
        }

        return items;
    }

    private async Task<List<Item>> FetchChunk(Gw2ApiManager apiManager, IEnumerable<int> itemIdChunk, CancellationToken cancellationToken)
    {
        Logger.Debug($"Start loading items by id: {itemIdChunk.First()} - {itemIdChunk.Last()}");

        var items = await apiManager.Gw2ApiClient.V2.Items.ManyAsync(itemIdChunk, cancellationToken);

        Logger.Debug($"Finished loading items by id: {itemIdChunk.First()} - {itemIdChunk.Last()}");

        return items.Select(Item.FromAPI).ToList();
    }
}
