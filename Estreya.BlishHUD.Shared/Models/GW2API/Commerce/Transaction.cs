namespace Estreya.BlishHUD.Shared.Models.GW2API.Commerce;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Models.GW2API.Items;
using Humanizer;
using Microsoft.Xna.Framework.Media;
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

    public override string ToString()
    {
        return $"Item-ID: {this.ItemId} - Type: {this.Type.Humanize()} - Quantity: {this.Quantity} - Unit Price: {this.Price}";
    }

    public override bool Equals(object obj)
    {
        return obj is Transaction transaction && this.ItemId == transaction.ItemId && this.Type == transaction.Type;
    }

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + this.ItemId.GetHashCode();
            hash = hash * 23 + this.Price.GetHashCode();
            hash = hash * 23 + this.Quantity.GetHashCode();
            hash = hash * 23 + this.Created.GetHashCode();
            hash = hash * 23 + this.Type.GetHashCode();
            return hash;
        }
    }
}
