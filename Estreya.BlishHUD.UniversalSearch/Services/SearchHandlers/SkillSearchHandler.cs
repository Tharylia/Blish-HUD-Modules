namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Estreya.BlishHUD.UniversalSearch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SkillSearchHandler : SearchHandler<Skill>
{
    private readonly IconService _iconState;

    public override string Prefix => "s";

    public SkillSearchHandler(IEnumerable<Skill> skills, SearchHandlerConfiguration configuration, IconService iconState) : base(skills, configuration)
    {
        this._iconState = iconState;
    }

    protected override SearchResultItem CreateSearchResultItem(Skill item)
        => new SkillSearchResultItem(this._iconState) { Skill = item };

    protected override string GetSearchableProperty(Skill item)
        => item.Name;

    protected override bool IsBroken(Skill item)
    {
        return string.IsNullOrWhiteSpace(item.Name) || (item.Name.StartsWith("((") && item.Name.EndsWith("))"));
    }
}
