namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Blish_HUD.Content;

using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD.Controls;
using Estreya.BlishHUD.UniversalSearch.Utils;
using Estreya.BlishHUD.Shared.State;
using Blish_HUD;
using Estreya.BlishHUD.UniversalSearch.Controls.Tooltips;

public class TraitSearchResultItem : SearchResultItem
{
    private readonly IconState _iconState;
    private Trait _trait;

    public Trait Trait
    {
        get => _trait;
        set
        {
            if (SetProperty(ref _trait, value))
            {
                if (_trait != null)
                {
                    Icon = _trait.Icon != null ? this._iconState.GetIcon(_trait.Icon) : (AsyncTexture2D)ContentService.Textures.Error;
                    Name = _trait.Name;
                    Description = Utils.StringUtil.SanitizeTraitDescription(_trait.Description);
                }
            }
        }
    }

    public TraitSearchResultItem(IconState iconState) : base(iconState)
    {
        this._iconState = iconState;
    }

    protected override string ChatLink => GenerateChatLink(Trait);

    protected override Tooltip BuildTooltip()
        => new TraitTooltip(Trait);

    private static string GenerateChatLink(Trait trait)
    {
        const byte TRAIT_CHATLINK_TYPE = 0x07;

        var result = new List<byte> {
                TRAIT_CHATLINK_TYPE,
            };

        result.AddRange(BitConverter.GetBytes(trait.Id));
        return $"[&{Convert.ToBase64String(result.ToArray())}]";
    }
}
