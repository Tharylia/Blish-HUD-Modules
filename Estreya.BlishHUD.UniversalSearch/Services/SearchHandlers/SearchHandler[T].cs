namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using Blish_HUD;
using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class SearchHandler<T> : SearchHandler
{
    protected abstract HashSet<T> SearchItems { get; }

    public abstract void UpdateSearchItems(List<T> items);

    protected abstract string GetSearchableProperty(T item);

    protected abstract SearchResultItem CreateSearchResultItem(T item);

    public override IEnumerable<SearchResultItem> Search(string searchText)
    {
        var diffs = new List<WordScoreResult<T>>();

        foreach (var item in SearchItems)
        {
            int score;
            var name = GetSearchableProperty(item);
            if (name.StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase))
            {
                score = 0;
            }
            else if (name.ToUpper().Contains(searchText.ToUpper()))
            {
                score = 3;
            }
            else
            {
                score = Utils.StringUtil.ComputeLevenshteinDistance(searchText.ToLower(), name.Substring(0, Math.Min(searchText.Length, name.Length)).ToLower());
            }

            diffs.Add(new WordScoreResult<T>(item, score));
        }

        return diffs.OrderBy(x => x.DiffScore).ThenBy(x => GetSearchableProperty(x.Result).Length).Take(MAX_RESULT_COUNT).Select(x => CreateSearchResultItem(x.Result));

    }
}
