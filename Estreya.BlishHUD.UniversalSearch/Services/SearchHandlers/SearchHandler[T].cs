namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;

using Blish_HUD;
using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Estreya.BlishHUD.UniversalSearch.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public abstract class SearchHandler<T> : SearchHandler
{
    private Logger _logger;
    protected HashSet<T> SearchItems { get; set; }

    public SearchHandler(IEnumerable<T> searchItems, SearchHandlerConfiguration configuration) : base(configuration)
    {
        this._logger = Logger.GetLogger(this.GetType());
        this.UpdateSearchItems(searchItems);
    }

    public virtual void UpdateSearchItems(IEnumerable<T> items)
    {
        this.SearchItems = items.ToHashSet();
    }

    protected abstract string GetSearchableProperty(T item);

    protected abstract SearchResultItem CreateSearchResultItem(T item);

    protected abstract bool IsBroken(T item);

    public override Task<IEnumerable<SearchResultItem>> SearchAsync(string searchText)
    {
        var diffs = new List<WordScoreResult<T>>();

        Stopwatch sw = Stopwatch.StartNew();
        foreach (var item in this.SearchItems)
        {
            if (!this.Configuration.IncludeBrokenItem.Value && this.IsBroken(item)) continue;

            int score = -1;
            var name = this.GetSearchableProperty(item);
            if (this.Configuration.SearchMode.Value is SearchMode.StartsWith or SearchMode.Any && name.StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase))
            {
                score = 0;
            }
            else if (this.Configuration.SearchMode.Value is SearchMode.Contains or SearchMode.Any && name.ToUpper().Contains(searchText.ToUpper()))
            {
                score = 3;
            }
            else if (this.Configuration.SearchMode.Value is SearchMode.Levenshtein or SearchMode.Any)
            {
                score = Utils.StringUtil.ComputeLevenshteinDistance(searchText.ToLower(), name/*.Substring(0, Math.Min(searchText.Length, name.Length))*/.ToLower());
            }

            if (score > -1)
            {
                diffs.Add(new WordScoreResult<T>(item, score));
            }
        }

        sw.Stop();
        this._logger.Debug($"Finished searching for \"{searchText}\" in {sw.Elapsed.TotalMilliseconds}ms. Found {diffs.Count} results.");

        var ordered = diffs.OrderBy(x => x.DiffScore).ThenBy(x => this.GetSearchableProperty(x.Result).Length);
        return Task.FromResult(ordered.Take(this.Configuration.MaxSearchResults.Value).Select(x => this.CreateSearchResultItem(x.Result)));

    }

    public override void Dispose()
    {
        this.SearchItems?.Clear();
    }
}
