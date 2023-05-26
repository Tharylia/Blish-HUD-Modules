namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD.Modules.Managers;
using Extensions;
using Gw2Sharp.WebApi.V2;
using Models.GW2API.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ItemService : FilesystemAPIService<Item>
{
    public ItemService(APIServiceConfiguration configuration, Gw2ApiManager apiManager, string baseFolderPath) : base(apiManager, configuration, baseFolderPath)
    {
    }

    protected override string BASE_FOLDER_STRUCTURE => "items";
    protected override string FILE_NAME => "items.json";
    public List<Item> Items => this.APIObjectList;

    public Item GetItemByName(string name)
    {
        if (!this._apiObjectListLock.IsFree())
        {
            return null;
        }

        using (this._apiObjectListLock.Lock())
        {
            foreach (Item item in this.Items)
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
        if (!this._apiObjectListLock.IsFree())
        {
            return null;
        }

        using (this._apiObjectListLock.Lock())
        {
            foreach (Item item in this.Items)
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

        IApiV2ObjectList<int> itemIds = await apiManager.Gw2ApiClient.V2.Items.IdsAsync(cancellationToken);

        progress.Report($"Loading items... 0/{itemIds.Count}");
        this.Logger.Info($"Start loading items: {itemIds.First()} - {itemIds.Last()}");

        int loadedItems = 0;
        int chunkSize = 200;
        IEnumerable<IEnumerable<int>> chunks = itemIds.ChunkBy(chunkSize);

        IEnumerable<IEnumerable<IEnumerable<int>>> chunkGroups = chunks.ChunkBy(20);

        foreach (IEnumerable<IEnumerable<int>> chunkGroup in chunkGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Load a group in parallel
            List<Task<List<Item>>> tasks = new List<Task<List<Item>>>();

            foreach (IEnumerable<int> idChunk in chunkGroup)
            {
                cancellationToken.ThrowIfCancellationRequested();

                tasks.Add(this.FetchChunk(apiManager, idChunk, cancellationToken)
                              .ContinueWith(resultTask =>
                              {
                                  List<Item> resultItems = resultTask.IsFaulted ? new List<Item>() : resultTask.Result;

                                  int newCount = Interlocked.Add(ref loadedItems, resultItems.Count);
                                  progress.Report($"Loading items... {newCount}/{itemIds.Count}");
                                  return resultItems;
                              }));
            }

            List<Item>[] itemGroup = await Task.WhenAll(tasks);
            List<Item> fetchedItems = itemGroup.SelectMany(i => i).ToList();

            items.AddRange(fetchedItems);
        }

        return items;
    }

    private async Task<List<Item>> FetchChunk(Gw2ApiManager apiManager, IEnumerable<int> itemIdChunk, CancellationToken cancellationToken)
    {
        this.Logger.Debug($"Start loading items by id: {itemIdChunk.First()} - {itemIdChunk.Last()}");

        IReadOnlyList<Gw2Sharp.WebApi.V2.Models.Item> items = await apiManager.Gw2ApiClient.V2.Items.ManyAsync(itemIdChunk, cancellationToken);

        this.Logger.Debug($"Finished loading items by id: {itemIdChunk.First()} - {itemIdChunk.Last()}");

        return items.Select(Item.FromAPI).ToList();
    }
}