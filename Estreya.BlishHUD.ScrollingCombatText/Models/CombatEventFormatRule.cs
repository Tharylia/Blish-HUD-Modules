namespace Estreya.BlishHUD.ScrollingCombatText.Models;

using Estreya.BlishHUD.Shared.Models.ArcDPS;
using HandlebarsDotNet;
using Humanizer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CombatEventFormatRule
{
    public CombatEventCategory Category { get; init; }

    public CombatEventType Type { get; init; }

    [Description(
        "Supports the following attributes:\n\n" +
        "{{category}} = The category of the event.\n" +
        "{{type}} = The type of the event.\n" +
        "{{skill.id}} = The id of the skill.\n" +
        "{{skill.name}} = The name of the skill.\n" +
        "{{source.id}} = The id of the source entity.\n" +
        "{{source.name}} = The name of the source entity.\n" +
        "{{source.profession}} = The profession id of the source entity\n" +
        "{{source.elite}} = The elite id of the source entity\n" +
        "{{source.team}} = The team id of the source entity\n" +
        "{{destination.id}} = The id of the destination entity.\n" +
        "{{destination.name}} = The name of the destination entity.\n" +
        "{{destination.profession}} = The profession id of the destination entity.\n" +
        "{{destination.elite}} = The elite id of the destination entity.\n" +
        "{{destination.team}} = The team id of the destination entity.\n" +
        "{{value}} = The dmg/buff value of the event.")]
    public string Format { get; set; }

    public string FormatEvent(CombatEvent combatEvent)
    {
        if (combatEvent == null)
        {
            return string.Empty;
        }

        int value = 0;

        if (combatEvent.Ev != null)
        {
            value = combatEvent.Ev.Buff ? combatEvent.Ev.BuffDmg : combatEvent.Ev.Value;
        }

        string category = combatEvent.Category.Humanize();
        string type = combatEvent.Type.Humanize();
        string skillId = combatEvent.Ev.SkillId.ToString();
        string skillName = combatEvent.Skill?.Name ?? string.Empty;
        string sourceName = combatEvent.Src?.Name ?? string.Empty;
        string destinationName = combatEvent.Dst?.Name ?? string.Empty;

        if (this.Format == null)
        {
            return $"{category}: {skillName ?? type} ({type}): {value}";
        }

        var template = Handlebars.Compile(this.Format);

        return template.Invoke(new
        {
            category,
            type,
            source = new 
            {
                id = combatEvent.Src.Id,
                name = combatEvent.Src.Name,
                profession = combatEvent.Src.Profession,
                elite = combatEvent.Src.Elite,
                team = combatEvent.Src.Team
            },
            destination = new
            {
                id = combatEvent.Dst.Id,
                name = combatEvent.Dst.Name,
                profession = combatEvent.Dst.Profession,
                elite = combatEvent.Dst.Elite,
                team = combatEvent.Dst.Team
            },
            skill = new
            {
                id = combatEvent.Skill?.Id ?? 0,
                name = combatEvent.Skill?.Name ?? "Unknown"
            },
            value
        });
            //.Replace("{category}", category)
            //.Replace("{type}", type)
            //.Replace("{skillId}", skillId)
            //.Replace("{skillName}", skillName)
            //.Replace("{sourceName}", sourceName)
            //.Replace("{destinationName}", destinationName)
            //.Replace("{value}", value.ToString());
    }
}
