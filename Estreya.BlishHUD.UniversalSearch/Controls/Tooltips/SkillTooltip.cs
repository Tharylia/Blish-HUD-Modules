﻿namespace Estreya.BlishHUD.UniversalSearch.Controls.Tooltips;

using Blish_HUD;
using Blish_HUD.Controls;
using Gw2Sharp;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Shared.Services;
using System;
using System.Linq;
using Utils;
using Color = Microsoft.Xna.Framework.Color;
using ItemAttributes = Statics.ItemAttributes;
using Skill = Shared.Models.GW2API.Skills.Skill;
using StringUtil = Utils.StringUtil;

public class SkillTooltip : Tooltip
{
    private const int MAX_WIDTH = 400;
    private readonly IconService _iconState;

    private readonly Skill _skill;
    private readonly Label _title;

    public SkillTooltip(Skill skill, IconService iconState)
    {
        this._skill = skill;
        this._iconState = iconState;

        this._title = new Label
        {
            Text = this._skill.Name,
            Font = Content.DefaultFont18,
            TextColor = ContentService.Colors.Chardonnay,
            AutoSizeHeight = true,
            AutoSizeWidth = true,
            Parent = this
        };

        Label categoryText = null;
        string description = this._skill.Description;

        if (this._skill.Categories != null)
        {
            categoryText = new Label
            {
                Text = string.Join(", ", skill.Categories),
                Font = Content.DefaultFont16,
                AutoSizeHeight = true,
                AutoSizeWidth = true,
                Location = new Point(0, this._title.Bottom + 5),
                TextColor = ContentService.Colors.ColonialWhite,
                Parent = this
            };

            description = description.Substring(description.IndexOf(".") + 1).Trim();
        }

        Label skillDescription = new Label
        {
            Text = description,
            Font = Content.DefaultFont16,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Location = new Point(0, (categoryText == null ? this._title.Bottom : categoryText.Bottom) + 5),
            Parent = this
        };

        LabelUtil.HandleMaxWidth(skillDescription, MAX_WIDTH);

        Control lastFact = skillDescription;
        FlowPanel topRightControlPanel = new FlowPanel
        {
            Parent = this,
            FlowDirection = ControlFlowDirection.SingleLeftToRight,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize
        };

        // FlowDirection = ControlFlowDirection.SingleRightToLeft does not change width correctly. We need to add them backwards due to SingleLeftToRight.
        if (skill.Flags?.ToArray().FirstOrDefault(x => x == SkillFlag.NoUnderwater) != null)
        {
            this.CreateNonUnderwaterDisplay(topRightControlPanel);
        }

        if (skill.Professions.Contains("Thief") && skill.Initiative > 0)
        {
            this.CreateInitiativeDisplay(topRightControlPanel);
        }

        if ((skill.Professions.Contains("Revenant") || skill.Professions.Contains("Warrior")) && skill.Cost > 0)
        {
            this.CreateEnergyDisplay(topRightControlPanel);
        }

        //Control lastTopRightCornerControl = null;
        if (this._skill.Facts != null)
        {
            SkillFactRecharge rechargeFact = null;
            foreach (SkillFact fact in this._skill.Facts)
            {
                switch (fact)
                {
                    case SkillFactRecharge skillFactRecharge:
                        rechargeFact = skillFactRecharge;
                        break;
                    default:
                        lastFact = this.CreateFact(fact, lastFact);
                        break;
                }
            }

            if (rechargeFact != null)
            {
                this.CreateRechargeFact(rechargeFact, topRightControlPanel);
            }
        }

        topRightControlPanel.Right = this.Right;
        int minLeft = this._title.Right + 5;
        if (topRightControlPanel.Left < minLeft)
        {
            topRightControlPanel.Left = minLeft;
        }
    }

    private Control CreateFact(SkillFact fact, Control lastFact)
    {
        string icon = fact.Icon?.Url?.AbsoluteUri;

        if (fact is SkillFactPrefixedBuff prefixedBuff)
        {
            icon = prefixedBuff.Prefix.Icon.Url?.AbsoluteUri;
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
            Text = StringUtil.SanitizeTraitDescription(this.GetTextForFact(fact)),
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

    private string GetDamageFactText(SkillFactDamage skillFactDamage)
    {
        // This will not be the same as the wiki!

        int power = 1000;
        int armor = 2597;

        float weaponPower = ItemAttributes.Ascended.Weapon.Greatsword;

        if (this._skill.WeaponType is SkillWeaponType.Axe or SkillWeaponType.Dagger or SkillWeaponType.Mace or SkillWeaponType.Pistol or SkillWeaponType.Scepter or SkillWeaponType.Sword or SkillWeaponType.Focus or SkillWeaponType.Shield or SkillWeaponType.Torch or SkillWeaponType.Warhorn)
        {
            weaponPower = ItemAttributes.Ascended.Weapon.Sword;
        }

        return $"{skillFactDamage.Text}({skillFactDamage.HitCount}x): {skillFactDamage.HitCount * Math.Round(weaponPower * skillFactDamage.DmgMultiplier * power / armor, 0)}";
    }

    private string GetTextForFact(SkillFact fact)
    {
        switch (fact)
        {
            case SkillFactAttributeAdjust attributeAdjust:
                return $"{attributeAdjust.Text}: {attributeAdjust.Value}";
            case SkillFactBuff buff:
                string applyCountText = buff.ApplyCount is not null and not 1 ? buff.ApplyCount + "x " : string.Empty;
                string durationText = buff.Duration != 0 ? $" ({buff.Duration}s) " : string.Empty;
                return $"{applyCountText}{buff.Status}{durationText}: {buff.Description}";
            case SkillFactComboField comboField:
                return $"{comboField.Text}: {comboField.FieldType.ToEnumString()}";
            case SkillFactComboFinisher comboFinisher:
                return $"{comboFinisher.Text}: {comboFinisher.FinisherType} ({comboFinisher.Percent}% Chance)";
            case SkillFactDamage damage:
                return this.GetDamageFactText(damage);
            case SkillFactDistance distance:
                return $"{distance.Text}: {distance.Distance}";
            case SkillFactDuration duration:
                return $"{duration.Text}: {duration.Duration}s";
            case SkillFactHeal heal:
                return $"{heal.HitCount}x {heal.Text}";
            case SkillFactHealingAdjust healingAdjust:
                return $"{healingAdjust.HitCount}x {healingAdjust.Text}";
            case SkillFactNumber skillFactNumber:
                return $"{skillFactNumber.Text}: {skillFactNumber.Value}";
            case SkillFactPercent skillFactPercent:
                return $"{skillFactPercent.Text}: {skillFactPercent.Percent}%";
            case SkillFactPrefixedBuff skillFactPrefixedBuff:
                return $"{skillFactPrefixedBuff.ApplyCount}x {skillFactPrefixedBuff.Status} ({skillFactPrefixedBuff.Duration}s): {skillFactPrefixedBuff.Description}";
            case SkillFactRadius skillFactRadius:
                return $"{skillFactRadius.Text}: {skillFactRadius.Distance}";
            case SkillFactRange skillFactRange:
                return $"{skillFactRange.Text}: {skillFactRange.Value}";
            case SkillFactStunBreak stunBreak:
                return "Breaks Stun";
            case SkillFactTime skillFactTime:
                return $"{skillFactTime.Text}: {skillFactTime.Duration}s";
            case SkillFactUnblockable skillFactUnblockable:
            case SkillFactNoData skillFactNoData:
            default:
                return fact.Text;
        }
    }

    private void CreateRechargeFact(SkillFactRecharge skillFactRecharge, FlowPanel parent)
    {
        Image cooldownImage = new Image
        {
            Texture = skillFactRecharge.Icon != null ? Content.GetRenderServiceTexture(skillFactRecharge.Icon) : ContentService.Textures.Error,
            Visible = true,
            Size = new Point(20, 20),
            Parent = parent
        };

        //cooldownImage.Location = new Point(this.Width - cooldownImage.Width, 1);

        Label cooldownText = new Label
        {
            Text = skillFactRecharge.Value.ToString(),
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Parent = parent,
            Font = GameService.Content.DefaultFont16
        };
        //cooldownText.Location = new Point(cooldownImage.Left - cooldownText.Width - 2, 0);

        //return cooldownText;
    }

    private void CreateInitiativeDisplay(FlowPanel parent)
    {
        Image initiativeImage = new Image
        {
            Texture = this._iconState.GetIcon("156649.png"),
            Visible = true,
            Size = new Point(20, 20),
            Parent = parent
        };

        //if (lastControl == null)
        //{
        //    initiativeImage.Location = new Point(Math.Max(this._title.Right + 5, this.Width - initiativeImage.Width), 1);
        //}
        //else
        //{
        //    initiativeImage.Location = new Point(lastControl.Left - initiativeImage.Width - 5, 0);
        //}

        Label initiativeText = new Label
        {
            Text = this._skill.Initiative.ToString(),
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Parent = parent,
            Font = GameService.Content.DefaultFont16
        };
        //initiativeText.Location = new Point(initiativeImage.Left - initiativeText.Width - 2, 0);
        //return initiativeText;
    }

    private void CreateEnergyDisplay(FlowPanel parent)
    {
        Image energyImage = new Image
        {
            Texture = this._iconState.GetIcon("156647.png"),
            Visible = true,
            Size = new Point(20, 20),
            Parent = parent
        };

        //if (lastControl == null)
        //{
        //    energyImage.Location = new Point(Math.Max(this._title.Right + 5, this.Width - energyImage.Width), 1);
        //}
        //else
        //{
        //    energyImage.Location = new Point(lastControl.Left - energyImage.Width - 5, 0);
        //}

        Label energyText = new Label
        {
            Text = this._skill.Cost.ToString(),
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Parent = parent,
            Font = GameService.Content.DefaultFont16
        };
        //energyText.Location = new Point(energyImage.Left - energyText.Width - 2, 0);
        //return energyText;
    }

    private void CreateNonUnderwaterDisplay(FlowPanel parent)
    {
        Image underwaterImage = new Image
        {
            Texture = this._iconState.GetIcon("358417.png"),
            Visible = true,
            Size = new Point(20, 20),
            Parent = parent
        };

        //if (lastControl == null)
        //{
        //    underwaterImage.Location = new Point(Math.Max(this._title.Right + 5, this.Width - underwaterImage.Width), 1);
        //}
        //else
        //{
        //    underwaterImage.Location = new Point(lastControl.Left - underwaterImage.Width - 5, 0);
        //}

        //return underwaterImage;
    }
}