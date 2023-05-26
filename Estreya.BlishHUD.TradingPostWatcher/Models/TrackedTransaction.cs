namespace Estreya.BlishHUD.TradingPostWatcher.Models;

using Humanizer;
using Newtonsoft.Json;
using Shared.Models.GW2API.Items;
using System;

public class TrackedTransaction
{
    public int ItemId { get; set; }

    public int WishPrice { get; set; }

    public int ActualPrice { get; set; }

    public DateTime Created { get; set; }

    public TrackedTransactionType Type { get; set; }

    [JsonIgnore] public Item Item { get; set; }

    public override string ToString()
    {
        return $"Item-ID: {this.ItemId} - Type: {this.Type.Humanize()} - Price: {this.WishPrice}";
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 23) + this.ItemId.GetHashCode();
            hash = (hash * 23) + this.WishPrice.GetHashCode();
            hash = (hash * 23) + this.ActualPrice.GetHashCode();
            hash = (hash * 23) + this.Created.GetHashCode();
            hash = (hash * 23) + this.Type.GetHashCode();
            return hash;
        }
    }
}