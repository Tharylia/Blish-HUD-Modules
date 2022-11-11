namespace Estreya.BlishHUD.ScrollingCombatText.Models;

using Estreya.BlishHUD.Shared.Models.ArcDPS;
using Gw2Sharp.WebApi.V2.Models;
using HandlebarsDotNet;
using Humanizer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Blish_HUD.ContentService;

public class CombatEventFormatRule
{
    public CombatEventCategory Category { get; set; }

    public CombatEventType Type { get; set; }

    public CombatEventState State { get; set; }

    public string Name => $"{this.Category.Humanize()} - {this.Type.Humanize()}";

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

    public FontSize FontSize { get; set; }

    public Color TextColor { get; set; }

    public string FormatEvent(CombatEvent combatEvent)
    {
        if (combatEvent == null)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(this.Format))
        {
            return $"--Empty Format--";
        }

        string category = combatEvent.Category.Humanize();
        string type = combatEvent.Type.Humanize();

        var template = Handlebars.Compile(this.Format);

        var combatEventFields = new Dictionary<string, object>();
        var fieldInfos = combatEvent.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public);
        foreach (var fieldInfo in fieldInfos)
        {
            combatEventFields.Add(fieldInfo.Name, fieldInfo.GetValue(combatEvent));
        }

        return template.Invoke(new
        {
            category,
            type,
            source = combatEvent.Source,
            destination = combatEvent.Destination,
            skill = new
            {
                Id = combatEvent.Skill?.Id ?? 0,
                Name = combatEvent.Skill?.Name ?? "Unknown"
            },
            combatEvent = combatEventFields
        });
            //.Replace("{category}", category)
            //.Replace("{type}", type)
            //.Replace("{skillId}", skillId)
            //.Replace("{skillName}", skillName)
            //.Replace("{sourceName}", sourceName)
            //.Replace("{destinationName}", destinationName)
            //.Replace("{value}", value.ToString());
    }

    public bool Validate()
    {
        bool valid = true;

        valid &= !string.IsNullOrWhiteSpace(this.Format);
        valid &= this.FontSize != 0;
        valid &= this.TextColor != null;

        return valid;
    }
}
