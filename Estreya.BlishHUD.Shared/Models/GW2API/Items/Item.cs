namespace Estreya.BlishHUD.Shared.Models.GW2API.Items;

using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Gw2Sharp;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;

public class Item
{
    public const string LAST_SCHEMA_CHANGE = "2022-09-20";

    public int Id { get; private set; }

    public string Name { get; private set; }
    public string? Description { get; set; }

    public ItemType Type { get; set; }

    public ItemRarity Rarity { get; set; }

    public ItemFlag[] Flags { get; set; }

    public string ChatLink { get; set; }

    public string Icon { get; set; }

    public Item(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public static Item FromAPI(Gw2Sharp.WebApi.V2.Models.Item apiItem)
    {
        Item item = new Item(apiItem.Id, apiItem.Name)
        {
            Description = apiItem.Description,
            Type = apiItem.Type?.IsUnknown ?? true ? ItemType.Unknown : apiItem.Type,
            Rarity = apiItem.Rarity?.IsUnknown ?? true ? ItemRarity.Unknown : apiItem.Rarity,
            ChatLink = apiItem.ChatLink,
            Icon = apiItem.Icon.Url?.AbsoluteUri,
            Flags = apiItem.Flags?.Where(flag => !flag.IsUnknown).Select(flag => flag.Value).ToArray()
        };

        return item;

    }

    public override string ToString()
    {
        return $"{this.Id} - {this.Name ?? "Unknown"}";
    }
}
