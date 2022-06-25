namespace Estreya.BlishHUD.Shared.Models.GW2API.Skills;

using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.State;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Skill : Gw2Sharp.WebApi.V2.Models.Skill
{
    [JsonIgnore]
    public Texture2D IconTexture { get; set; }

    public static Skill FromAPISkill(Gw2Sharp.WebApi.V2.Models.Skill skill)
    {
        Skill newSkill = new Skill
        {
            Id = skill.Id,
            Name = skill.Name,
            Description = skill.Description,
            Icon = skill.Icon,
            Specialization = skill.Specialization,
            ChatLink = skill.ChatLink,
            Type = skill.Type,
            WeaponType = skill.WeaponType,
            Professions = skill.Professions,
            Slot = skill.Slot,
            DualAttunement = skill.DualAttunement,
            Flags = skill.Flags,
            Facts = skill.Facts,
            TraitedFacts = skill.TraitedFacts,
            Categories = skill.Categories,
            SubSkills = skill.SubSkills,
            Attunement = skill.Attunement,
            Cost = skill.Cost,
            DualWield = skill.DualWield,
            FlipSkill = skill.FlipSkill,
            Initiative = skill.Initiative,
            NextChain = skill.NextChain,
            PrevChain = skill.PrevChain,
            TransformSkills = skill.TransformSkills,
            BundleSkills = skill.BundleSkills,
            ToolbeltSkill = skill.ToolbeltSkill
        };


        return newSkill;
    }

    public async Task LoadTexture(IconState iconState)
    {
        if (this.Icon.Url != null && !string.IsNullOrWhiteSpace(this.Icon.Url.AbsoluteUri))
        {
            this.IconTexture = await iconState.GetIconAsync(this.Icon);
        }
    }
}
