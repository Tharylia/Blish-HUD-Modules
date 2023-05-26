namespace Estreya.BlishHUD.UniversalSearch.Controls.Tooltips;

using Blish_HUD;
using Blish_HUD.Controls;
using Gw2Sharp.WebApi;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Utils;
using Color = Microsoft.Xna.Framework.Color;
using StringUtil = Utils.StringUtil;

public class TraitTooltip : Tooltip
{
    private const int MAX_WIDTH = 400;

    private readonly Trait _trait;

    public TraitTooltip(Trait trait)
    {
        this._trait = trait;

        Label traitTitle = new Label
        {
            Text = this._trait.Name,
            Font = Content.DefaultFont18,
            TextColor = ContentService.Colors.Chardonnay,
            AutoSizeHeight = true,
            AutoSizeWidth = true,
            Parent = this
        };

        Label traitDescription = new Label
        {
            Text = StringUtil.SanitizeTraitDescription(this._trait.Description),
            Font = Content.DefaultFont16,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Location = new Point(0, traitTitle.Bottom + 5),
            Parent = this
        };

        LabelUtil.HandleMaxWidth(traitDescription, MAX_WIDTH);

        Control lastFact = traitDescription;
        if (this._trait.Facts != null)
        {
            TraitFactRecharge rechargeFact = null;
            foreach (TraitFact fact in this._trait.Facts)
            {
                switch (fact)
                {
                    case TraitFactRecharge traitFactRecharge:
                        rechargeFact = traitFactRecharge;
                        break;
                    default:
                        lastFact = this.CreateFact(fact, lastFact);
                        break;
                }
            }

            if (rechargeFact != null)
            {
                this.CreateRechargeFact(rechargeFact);
            }
        }
    }

    private Control CreateFact(TraitFact fact, Control lastFact)
    {
        // Skip Damage fact bc calculation of the actual damage value is rather complicated
        if (fact is TraitFactDamage)
        {
            return lastFact;
        }

        RenderUrl? icon = fact.Icon;

        if (fact is TraitFactPrefixedBuff prefixedBuff)
        {
            icon = prefixedBuff.Prefix.Icon;
        }

        Image factImage = new Image
        {
            Texture = icon != null ? Content.GetRenderServiceTexture(icon) : ContentService.Textures.Error,
            Size = new Point(32, 32),
            Location = new Point(0, lastFact.Bottom + 5),
            Parent = this
        };

        Label factDescription = new Label
        {
            Text = this.GetTextForFact(fact),
            Font = Content.DefaultFont16,
            TextColor = new Color(161, 161, 161),
            Height = factImage.Height,
            VerticalAlignment = VerticalAlignment.Middle,
            AutoSizeWidth = true,
            Location = new Point(factImage.Width + 5, lastFact.Bottom + 5),
            Parent = this
        };

        LabelUtil.HandleMaxWidth(
            factDescription,
            MAX_WIDTH,
            factImage.Width,
            () =>
            {
                factDescription.AutoSizeHeight = true;
                factDescription.RecalculateLayout();
                factImage.Location = new Point(0, factDescription.Location.Y + ((factDescription.Height / 2) - (factImage.Height / 2)));
            });

        return factDescription;
    }

    private string GetTextForFact(TraitFact fact)
    {
        switch (fact)
        {
            case TraitFactAttributeAdjust attributeAdjust:
                return $"{attributeAdjust.Text}: {attributeAdjust.Value}";
            case TraitFactBuff buff:
                string applyCountText = buff.ApplyCount != null && buff.ApplyCount != 1 ? buff.ApplyCount + "x " : string.Empty;
                string durationText = buff.Duration != 0 ? $" ({buff.Duration}s) " : string.Empty;
                return $"{applyCountText}{buff.Status}{durationText}: {buff.Description}";
            case TraitFactBuffConversion buffConversion:
                return string.Format("Gain {0} Based on a Percentage of {1}: {2}%", buffConversion.Target, buffConversion.Source, buffConversion.Percent);
            case TraitFactComboField comboField:
                return $"{comboField.Text}: {comboField.FieldType.ToEnumString()}";
            case TraitFactComboFinisher comboFinisher:
                return $"{comboFinisher.Text}: {comboFinisher.Type} ({comboFinisher.Percent} Chance)";
            case TraitFactDamage damage: // Skip
                return $"{damage.Text}({damage.HitCount}x): {damage.Text}";
            case TraitFactDistance distance:
                return $"{distance.Text}: {distance.Distance}";
            case TraitFactNoData noData:
                return "Combat Only";
            case TraitFactNumber number:
                return $"{number.Text}: {number.Value}";
            case TraitFactPercent percent:
                return $"{percent.Text}: {percent.Percent}%";
            case TraitFactPrefixedBuff prefixedBuff:
                return $"{prefixedBuff.ApplyCount}x {prefixedBuff.Status} ({prefixedBuff.Duration}s): {prefixedBuff.Description}";
            case TraitFactRadius radius:
                return $"{radius.Text}: {radius.Distance}";
            case TraitFactRange range:
                return $"{range.Text}: {range.Value}";
            case TraitFactTime time:
                return $"{time.Text}: {time.Duration}s";
            case TraitFactUnblockable unblockable:
            default:
                return fact.Text;
        }
    }

    private void CreateRechargeFact(TraitFactRecharge skillFactRecharge)
    {
        Image cooldownImage = new Image
        {
            Texture = skillFactRecharge.Icon != null ? Content.GetRenderServiceTexture(skillFactRecharge.Icon) : ContentService.Textures.Error,
            Visible = true,
            Size = new Point(16, 16),
            Parent = this
        };

        cooldownImage.Location = new Point(this.Width - cooldownImage.Width, 1);

        Label cooldownText = new Label
        {
            Text = skillFactRecharge.Value.ToString(),
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Parent = this
        };
        cooldownText.Location = new Point(cooldownImage.Left - cooldownText.Width - 2, 0);
    }
}