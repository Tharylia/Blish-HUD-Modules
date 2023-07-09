namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;

using Controls.SearchResults;
using Models;
using Shared.Models.GW2API.Skills;
using Shared.Services;
using System.Collections.Generic;

public class SkillSearchHandler : SearchHandler<Skill>
{
    private readonly IconService _iconState;

    public SkillSearchHandler(IEnumerable<Skill> skills, SearchHandlerConfiguration configuration, IconService iconState) : base(skills, configuration)
    {
        this._iconState = iconState;
    }

    public override string Prefix => "s";

    protected override SearchResultItem CreateSearchResultItem(Skill item)
    {
        return new SkillSearchResultItem(this._iconState) { Skill = item };
    }

    protected override string GetSearchableProperty(Skill item)
    {
        return item.Name;
    }

    protected override bool IsBroken(Skill item)
    {
        return string.IsNullOrWhiteSpace(item.Name) || (item.Name.StartsWith("((") && item.Name.EndsWith("))"));
    }
}