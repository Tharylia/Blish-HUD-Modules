namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;

using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class SearchHandler
{
    public const int MAX_RESULT_COUNT = 5;

    public abstract string Name { get; }

    public abstract string Prefix { get; }

    public abstract IEnumerable<SearchResultItem> Search(string searchText);
}