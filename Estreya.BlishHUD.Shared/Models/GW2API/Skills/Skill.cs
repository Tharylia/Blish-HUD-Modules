namespace Estreya.BlishHUD.Shared.Models.GW2API.Skills;

using Blish_HUD.Content;
using Estreya.BlishHUD.Shared.Services;
using Gw2Sharp;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class Skill : IDisposable
{
    [JsonConverter(typeof(StringEnumConverter))]
    public SkillCategory Category { get; set; }

    /// <summary>
    /// The skill id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The skill name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The skill description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The skill icon URL.
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// The skill specialization.
    /// Can be resolved against <see cref="IGw2WebApiV2Client.Specializations"/>.
    /// If the skill is not associated with a specific specialization, this value is <see langword="null"/>.
    /// </summary>
    public int Specialization { get; set; }

    /// <summary>
    /// The skill chat link.
    /// </summary>
    public string ChatLink { get; set; } = string.Empty;

    /// <summary>
    /// The skill type.
    /// If the skill does not have a type, this value is <see langword="null"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public SkillType Type { get; set; }

    /// <summary>
    /// The weapon type.
    /// If the skill does not have a weapon type, this value is <see langword="null"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public SkillWeaponType WeaponType { get; set; }

    /// <summary>
    /// The list of professions that can use this skill.
    /// Each element can be resolved against <see cref="IGw2WebApiV2Client.Professions"/>.
    /// </summary>
    public List<string> Professions { get; set; } = new List<string>();

    /// <summary>
    /// The skill slot.
    /// If the skill does not have a slot, this value is <see langword="null"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public SkillSlot Slot { get; set; }

    /// <summary>
    /// The dual attunement.
    /// If the skill does not have a dual attunement, this value is <see langword="null"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public Attunement DualAttunement { get; set; }

    /// <summary>
    /// The skill flags.
    /// </summary>
    public List<SkillFlag> Flags { get; set; }

    /// <summary>
    /// The list of skill facts.
    /// If the skill doesn't have any facts, this value is <see langword="null"/>.
    /// </summary>
    /// BUGGY WITH COPY()
    //public List<SkillFact>? Facts { get; set; }

    /// <summary>
    /// The list of traited skill facts.
    /// If the skill doesn't have any traited facts, this value is <see langword="null"/>.
    /// </summary>
    /// BUGGY WITH COPY()
    //public List<SkillFact>? TraitedFacts { get; set; }

    /// <summary>
    /// The list of skill categories.
    /// If the skill doesn't have any categories, this value is <see langword="null"/>.
    /// </summary>
    public List<string>? Categories { get; set; }

    /// <summary>
    /// The list of sub skills.
    /// If the skill doesn't have any sub skills, this value is <see langword="null"/>.
    /// </summary>
    [JsonProperty("subskills")]
    public List<SkillSubSkill> SubSkills { get; set; }

    /// <summary>
    /// The attunement for elementalist weapon skills.
    /// If the skill isn't an elementalist weapon skill, this value is <see langword="null"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public Attunement Attunement { get; set; }

    /// <summary>
    /// The skill cost, for e.g. revenant, warrior and druid skills.
    /// If the skill doesn't have any cost, this value is <see langword="null"/>.
    /// </summary>
    public int Cost { get; set; }

    /// <summary>
    /// The dual wield weapon that needs to be equipped for this dual-wield skill to appear.
    /// If the skill is not part of a dual-wield weapon, this value is <see langword="null"/>.
    /// </summary>
    public string DualWield { get; set; }

    /// <summary>
    /// The skill id that this skill can flip to.
    /// Can be resolved against <see cref="IGw2WebApiV2Client.Skills"/>.
    /// If the skill does not have a flip skill, this value is <see langword="null"/>.
    /// </summary>
    public int FlipSkill { get; set; }

    /// <summary>
    /// The skill initiative cost for thief skills.
    /// If the skill is not a thief skill, this value is <see langword="null"/>.
    /// </summary>
    public int Initiative { get; set; }

    /// <summary>
    /// The next skill in the skill chain.
    /// Can be resolved against <see cref="IGw2WebApiV2Client.Skills"/>.
    /// If the skill does not have a next skill in the chain, or if the skill is not part of a skill chain at all, this value is <see langword="null"/>.
    /// </summary>
    public int NextChain { get; set; }

    /// <summary>
    /// The previous skill in the skill chain.
    /// Can be resolved against <see cref="IGw2WebApiV2Client.Skills"/>.
    /// If the skill does not have a previous skill in the chain, or if the skill is not part of a skill chain at all, this value is <see langword="null"/>.
    /// </summary>
    public int PrevChain { get; set; }

    /// <summary>
    /// The transform skills that will replace the player's skills when the skill is activated.
    /// Each element can be resolved against <see cref="IGw2WebApiV2Client.Skills"/>.
    /// If the skill is not a transform skill, this value is <see langword="null"/>.
    /// </summary>
    public List<int> TransformSkills { get; set; }

    /// <summary>
    /// The bundle skills that will replace the player's skills when the skill is activated.
    /// Each element can be resolved against <see cref="IGw2WebApiV2Client.Skills"/>.
    /// If the skill is not a bundle skill, this value is <see langword="null"/>.
    /// </summary>
    public List<int> BundleSkills { get; set; }

    /// <summary>
    /// The associated toolbelt skill.
    /// Can be resolved against <see cref="IGw2WebApiV2Client.Skills"/>.
    /// If the skill is not a toolbelt skill, this value is <see langword="null"/>.
    /// </summary>
    public int ToolbeltSkill { get; set; }

    [JsonIgnore]
    public AsyncTexture2D IconTexture { get; set; }

    public static Skill FromAPISkill(Gw2Sharp.WebApi.V2.Models.Skill skill)
    {
        Skill newSkill = new Skill
        {
            Category = SkillCategory.Skill,
            Id = skill.Id,
            Name = skill.Name,
            Description = skill.Description,
            Icon = skill.Icon?.Url?.AbsoluteUri,
            Specialization = skill.Specialization ?? 0,
            ChatLink = skill.ChatLink,
            Type = skill.Type?.IsUnknown ?? true ? SkillType.Unknown : skill.Type,
            WeaponType = skill.WeaponType?.IsUnknown ?? true ? SkillWeaponType.Unknown : skill.WeaponType,
            Professions = skill.Professions.ToList(),
            Slot = skill.Slot?.IsUnknown ?? true ? SkillSlot.Unknown : skill.Slot,
            DualAttunement = skill.DualAttunement?.IsUnknown ?? true ? Attunement.Unknown : skill.DualAttunement,
            Flags = skill.Flags?.List.Select(flag => flag.Value).ToList(),
            //Facts = skill.Facts?.ToList(),
            //TraitedFacts = skill.TraitedFacts?.ToList(),
            Categories = skill.Categories?.ToList(),
            SubSkills = skill.SubSkills?.ToList(),
            Attunement = skill.Attunement?.IsUnknown ?? true ? Attunement.Unknown : skill.Attunement,
            Cost = skill.Cost ?? 0,
            DualWield = skill.DualWield,
            FlipSkill = skill.FlipSkill ?? 0,
            Initiative = skill.Initiative ?? 0,
            NextChain = skill.NextChain ?? 0,
            PrevChain = skill.PrevChain ?? 0,
            TransformSkills = skill.TransformSkills?.ToList(),
            BundleSkills = skill.BundleSkills?.ToList(),
            ToolbeltSkill = skill.ToolbeltSkill ?? 0
        };

        return newSkill;
    }

    public static Skill FromAPITrait(Gw2Sharp.WebApi.V2.Models.Trait trait)
    {
        Skill newSkill = new Skill
        {
            Category = SkillCategory.Trait,
            Id = trait.Id,
            Name = trait.Name,
            Description = trait.Description,
            Icon = trait.Icon,
            Specialization = trait.Specialization,
        };

        return newSkill;
    }

    public static Skill FromAPITraitSkill(Gw2Sharp.WebApi.V2.Models.TraitSkill skill)
    {
        Skill newSkill = new Skill
        {
            Category = SkillCategory.TraitSkill,
            Id = skill.Id,
            Name = skill.Name,
            Description = skill.Description,
            Icon = skill.Icon,
            ChatLink = skill.ChatLink,
            Flags = skill.Flags?.List.Select(flag => flag.Value).ToList(),
            //Facts = skill.Facts?.ToList(),
            //TraitedFacts = skill.TraitedFacts?.ToList(),
            Categories = skill.Categories?.ToList(),
        };

        return newSkill;
    }

    public static Skill FromAPIUpgradeComponent(ItemUpgradeComponent upgradeComponent)
    {
        if (upgradeComponent.Details?.InfixUpgrade == null)
        {
            return null;
        }

        Skill newSkill = new Skill
        {
            Category = SkillCategory.UpgradeComponent,
            Id = upgradeComponent.Details?.InfixUpgrade.Buff.SkillId ?? 0,
            Name = upgradeComponent.Name,
            Description = upgradeComponent.Details?.InfixUpgrade.Buff.Description,
            Icon = upgradeComponent.Icon,
            ChatLink = upgradeComponent.ChatLink
        };

        return newSkill;
    }

    public void Dispose()
    {
        this.IconTexture = null;
    }

    public void LoadTexture(IconService iconService)
    {
        if (!string.IsNullOrWhiteSpace(this.Icon))
        {
            this.IconTexture = iconService.GetIcon(this.Icon);
        }
    }
}
