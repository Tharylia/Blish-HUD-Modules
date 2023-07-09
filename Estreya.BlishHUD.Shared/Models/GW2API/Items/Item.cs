namespace Estreya.BlishHUD.Shared.Models.GW2API.Items;

using Gw2Sharp.WebApi.V2.Models;
using System.Linq;

public class Item
{
    public const string LAST_SCHEMA_CHANGE = "2022-09-20";

    public Item(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public int Id { get; }

    public string Name { get; }
    public string? Description { get; set; }

    public ItemType Type { get; set; }

    public ItemRarity Rarity { get; set; }

    public ItemFlag[] Flags { get; set; }

    public string ChatLink { get; set; }

    public string Icon { get; set; }

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