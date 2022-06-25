namespace Estreya.BlishHUD.Shared.Models.GW2API.Commerce;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.UI.Views.Controls;
using Gw2Sharp.WebApi.V2.Models;
using Humanizer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Transaction
{
    private static readonly Logger Logger = Logger.GetLogger<Transaction>();

    public int ItemId { get; set; }

    public int Price { get; set; }

    public int Quantity { get; set; }

    public DateTime Created { get; set; }

    public TransactionType Type { get; set; }

    [JsonIgnore]
    public Item Item { get; set; }

    public async Task LoadItem(Gw2ApiManager apiManager)
    {
        try
        {
            this.Item = await apiManager.Gw2ApiClient.V2.Items.GetAsync(this.ItemId);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Could not load item {0}", this.ItemId);
        }
    }

    public override string ToString()
    {
        return $"Item-ID: {this.ItemId} - Type: {this.Type.Humanize()} - Quantity: {this.Quantity} - Unit Price: {this.Price}";
    }

    public override int GetHashCode()
    {
        return this.ItemId.GetHashCode() & this.Price.GetHashCode() & this.Quantity.GetHashCode() & this.Type.GetHashCode() & this.Created.GetHashCode();
    }
}
