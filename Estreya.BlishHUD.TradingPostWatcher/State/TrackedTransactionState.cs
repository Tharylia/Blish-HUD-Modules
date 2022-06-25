namespace Estreya.BlishHUD.TradingPostWatcher.State;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class TrackedTransactionState : APIState<TrackedTransaction>
{
    private static readonly Logger Logger = Logger.GetLogger<TrackedTransactionState>();

    private const string FOLDER_NAME = "tracked";
    private const string FILE_NAME = "transactions.txt";
    private const string COLUMN_SPLIT = "<-->";
    private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ssZ";

    private List<TrackedTransaction> _trackedTransactions = new List<TrackedTransaction>();
    private AsyncLock _transactionLock = new AsyncLock();
    private readonly string _baseFolder;

    private string FullFolderPath => Path.Combine(this._baseFolder, FOLDER_NAME);

    public List<TrackedTransaction> TrackedTransactions => this._trackedTransactions;
    public List<TrackedTransaction> BestPriceTransactions => this.APIObjectList;

    public event EventHandler<TrackedTransaction> TransactionEnteredRange;
    public event EventHandler<TrackedTransaction> TransactionLeftRange;

    public TrackedTransactionState(Gw2ApiManager apiManager, string baseFolder) : base(apiManager, null, TimeSpan.FromMinutes(2), true, 60000)
    {
        this.APIObjectAdded += this.TrackedTransactionState_APIObjectAdded;
        this.APIObjectRemoved += this.TrackedTransactionState_APIObjectRemoved;
        this._baseFolder = baseFolder;
    }

    private void TrackedTransactionState_APIObjectRemoved(object sender, TrackedTransaction e)
    {
        this.TransactionLeftRange?.Invoke(this, e);
    }

    private void TrackedTransactionState_APIObjectAdded(object sender, TrackedTransaction e)
    {
        this.TransactionEnteredRange?.Invoke(this, e);
    }

    protected override Task DoClear()
    {
        using (this._transactionLock.Lock())
        {
            _trackedTransactions.Clear();
        }

        return Task.CompletedTask;
    }

    protected override void DoUnload()
    {
        this.APIObjectAdded -= this.TrackedTransactionState_APIObjectAdded;
        this.APIObjectRemoved -= this.TrackedTransactionState_APIObjectRemoved;
    }

    protected override async Task Load()
    {
        List<string> trackedTransactionLines = new List<string>();
        if (File.Exists(Path.Combine(this.FullFolderPath, FILE_NAME)))
        {
            var lines = await FileUtil.ReadLinesAsync(Path.Combine(this.FullFolderPath, FILE_NAME));
            if (lines.Length > 0)
            {
                trackedTransactionLines.AddRange(lines);
            }
        }

        if (trackedTransactionLines.Count > 0)
        {
            foreach (string transactionLine in trackedTransactionLines)
            {
                string[] parts = transactionLine.Split(new string[] { COLUMN_SPLIT }, StringSplitOptions.None);

                if (parts.Length == 0)
                {
                    Logger.Warn("Line empty.");
                    continue;
                }

                string id = parts[0];
                try
                {
                    TransactionType type = (TransactionType)Enum.Parse(typeof(TransactionType), parts[1]);
                    int price = int.Parse(parts[2]);
                    DateTime created = DateTime.SpecifyKind(DateTime.ParseExact(parts[3], DATE_TIME_FORMAT, CultureInfo.InvariantCulture), DateTimeKind.Utc);

                    _ = await this.Add(int.Parse(id), price, type, created);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Could not load tracked transaction {0}", id);
                }
            }
        }

        await base.Load();
    }

    protected override async Task Save()
    {
        using (await this._transactionLock.LockAsync())
        {
            List<string> lines = new List<string>();

            foreach (TrackedTransaction trackedTransaction in this._trackedTransactions)
            {
                lines.Add($"{trackedTransaction.ItemId}{COLUMN_SPLIT}{trackedTransaction.Type}{COLUMN_SPLIT}{trackedTransaction.WishPrice}{COLUMN_SPLIT}{trackedTransaction.Created.ToString(DATE_TIME_FORMAT)}");
            }

            await FileUtil.WriteLinesAsync(Path.Combine(this.FullFolderPath, FILE_NAME), lines.ToArray());
        }
    }

    private async Task<bool> Add(int id, int price, TransactionType type, DateTime created)
    {
        this.Remove(id, type);

        using (this._transactionLock.Lock())
        {
            try
            {
                // No need to wait, as items don't need permissions.
                var item = await this._apiManager.Gw2ApiClient.V2.Items.GetAsync(id);

                this._trackedTransactions.Add(new TrackedTransaction()
                {
                    ItemId = id,
                    WishPrice = price,
                    Created = created.ToUniversalTime(),
                    Item = item,
                    Type = type
                });

                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not add item {0}", id);
                return false;
            }
        }
    }

    public async Task<bool> Add(int id, int price, TransactionType type)
    {
        return await this.Add(id, price, type, DateTime.UtcNow);
    }

    public void Remove(int id, TransactionType type)
    {
        List<TrackedTransaction> transactionsToRemove = this._trackedTransactions.Where(t => t.ItemId == id && t.Type == type).ToList();

        if (transactionsToRemove.Count == 0)
        {
            return;
        }

        using (this._transactionLock.Lock())
        {
            for (int i = transactionsToRemove.Count - 1; i >= 0; i--)
            {
                _ = this._trackedTransactions.Remove(transactionsToRemove[i]);
            }
        }
    }

    protected override async Task<List<TrackedTransaction>> Fetch(Gw2ApiManager apiManager)
    {
        List<TrackedTransaction> transactions = new List<TrackedTransaction>();

        using (await this._transactionLock.LockAsync())
        {
            foreach (TrackedTransaction transaction in this._trackedTransactions)
            {
                Gw2Sharp.WebApi.V2.Models.CommercePrices prices = await apiManager.Gw2ApiClient.V2.Commerce.Prices.GetAsync(transaction.ItemId);

                switch (transaction.Type)
                {
                    case TransactionType.Buy:
                        transaction.Price = prices.Buys.UnitPrice;

                        if (prices.Buys.UnitPrice <= transaction.WishPrice)
                        {
                            transactions.Add(transaction);
                        }

                        break;
                    case TransactionType.Sell:
                        transaction.Price = prices.Sells.UnitPrice;

                        if (prices.Sells.UnitPrice >= transaction.WishPrice)
                        {
                            transactions.Add(transaction);
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        return transactions;
    }
}
