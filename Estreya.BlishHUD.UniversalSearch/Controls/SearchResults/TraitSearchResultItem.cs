namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;

using Blish_HUD;
using Blish_HUD.Controls;
using Gw2Sharp.WebApi.V2.Models;
using Shared.Services;
using System;
using System.Collections.Generic;
using Tooltips;
using StringUtil = Utils.StringUtil;

public class TraitSearchResultItem : SearchResultItem
{
    private readonly IconService _iconState;
    private Trait _trait;

    public TraitSearchResultItem(IconService iconState) : base(iconState)
    {
        this._iconState = iconState;
    }

    public Trait Trait
    {
        get => this._trait;
        set
        {
            if (this.SetProperty(ref this._trait, value))
            {
                if (this._trait != null)
                {
                    this.Icon = this._trait.Icon.Url?.AbsoluteUri != null ? this._iconState.GetIcon(this._trait.Icon.Url.AbsoluteUri) : ContentService.Textures.Error;
                    this.Name = this._trait.Name;
                    this.Description = StringUtil.SanitizeTraitDescription(this._trait.Description);
                }
            }
        }
    }

    protected override string ChatLink => GenerateChatLink(this.Trait);

    protected override Tooltip BuildTooltip()
    {
        return new TraitTooltip(this.Trait);
    }

    private static string GenerateChatLink(Trait trait)
    {
        const byte TRAIT_CHATLINK_TYPE = 0x07;

        List<byte> result = new List<byte> { TRAIT_CHATLINK_TYPE };

        result.AddRange(BitConverter.GetBytes(trait.Id));
        return $"[&{Convert.ToBase64String(result.ToArray())}]";
    }
}