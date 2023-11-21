namespace Estreya.BlishHUD.TradingPostWatcher.Services;

using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class FavouriteItemService : ManagedService
{
    private const string FILE_NAME = "favoritedItems.txt";
    private readonly string _baseFolder;
    private readonly TransactionsService _transactionsService;

    private string FullFilePath => Path.Combine(this._baseFolder, FILE_NAME);

    private List<int> _favoritedItemIds;

    public FavouriteItemService(ServiceConfiguration configuration, string baseFolder, TransactionsService transactionsService) : base(configuration)
    {
        this._baseFolder = baseFolder;
        this._transactionsService = transactionsService;
    }

    protected override Task Initialize()
    {
        this._favoritedItemIds = new List<int>();
        return Task.CompletedTask;
    }

    protected override void InternalUnload()
    {
        this._favoritedItemIds?.Clear();
        this._favoritedItemIds = null;
    }

    protected override void InternalUpdate(GameTime gameTime) { }

    protected override async Task Save()
    {
        try
        {
            await FileUtil.WriteStringAsync(this.FullFilePath, JsonConvert.SerializeObject(this._favoritedItemIds));
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Could not save favourite items.");
        }
    }

    protected override async Task Load()
    {
        if (!File.Exists(this.FullFilePath)) return;

        try
        {
            var content = await FileUtil.ReadStringAsync(this.FullFilePath);

            this._favoritedItemIds = JsonConvert.DeserializeObject<List<int>>(content);

            await this.AddItemSubscribtions();
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Could not load favourite items.");
        }
    }

    private async Task AddItemSubscribtions()
    {
        await this._transactionsService.AddItemSubscribtions(this._favoritedItemIds.ToArray());
    }

    public IEnumerable<int> GetAll()
    {
        return this._favoritedItemIds.ToArray();
    }

    public async Task AddItem(int id)
    {
        if (this._favoritedItemIds == null) throw new ArgumentNullException(nameof(this._favoritedItemIds));
        if (this._favoritedItemIds.Contains(id)) throw new ArgumentException(nameof(id), "Duplicate entry.");

        this._favoritedItemIds.Add(id);

        await this.AddItemSubscribtions();
    }

    public void RemoveItem(int id)
    {
        this._favoritedItemIds?.Remove(id);
    }
}
