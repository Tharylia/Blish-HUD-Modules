namespace Estreya.BlishHUD.Shared.Models.GW2API.Commerce;

using Estreya.BlishHUD.Shared.UI.Views.Controls;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TrackedTransaction : Transaction
{
    public int WishPrice { get; set; }

    public override string ToString()
    {
        return $"Item-ID: {this.ItemId} - Type: {this.Type.Humanize()} - Quantity: {this.Quantity} - Unit Price: {this.Price} - Wish Price: {this.WishPrice}";
    }
}
