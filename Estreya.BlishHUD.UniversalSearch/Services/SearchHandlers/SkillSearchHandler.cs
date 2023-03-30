namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SkillSearchHandler : SearchHandler<Skill>
{
    private HashSet<Skill> _skills = new HashSet<Skill>();
    private readonly IconState _iconState;

    public override string Name => "Skills"; // Strings.Common.SearchHandler_Skills;

    public override string Prefix => "s";

    public SkillSearchHandler(List<Skill> skills, IconState iconState)
    {
        this._skills = skills.ToHashSet();
        this._iconState = iconState;
    }

    protected override HashSet<Skill> SearchItems => _skills;

    protected override SearchResultItem CreateSearchResultItem(Skill item)
        => new SkillSearchResultItem(this._iconState) { Skill = item };

    protected override string GetSearchableProperty(Skill item)
        => item.Name;

    public override void UpdateSearchItems(List<Skill> items)
    {
        this._skills = items.ToHashSet();
    }
}
