namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;

using Blish_HUD;
using Controls.SearchResults;
using Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using StringUtil = Utils.StringUtil;

public abstract class SearchHandler<T> : SearchHandler
{
    private readonly Logger _logger;

    public SearchHandler(IEnumerable<T> searchItems, SearchHandlerConfiguration configuration) : base(configuration)
    {
        this._logger = Logger.GetLogger(this.GetType());
        this.UpdateSearchItems(searchItems);
    }

    protected HashSet<T> SearchItems { get; set; }

    public virtual void UpdateSearchItems(IEnumerable<T> items)
    {
        this.SearchItems = items.ToHashSet();
    }

    protected abstract string GetSearchableProperty(T item);

    protected abstract SearchResultItem CreateSearchResultItem(T item);

    protected abstract bool IsBroken(T item);

    public override Task<IEnumerable<SearchResultItem>> SearchAsync(string searchText)
    {
        List<WordScoreResult<T>> diffs = new List<WordScoreResult<T>>();

        Stopwatch sw = Stopwatch.StartNew();
        foreach (T item in this.SearchItems)
        {
            if (!this.Configuration.IncludeBrokenItem.Value && this.IsBroken(item))
            {
                continue;
            }

            int score = -1;
            string name = this.GetSearchableProperty(item);
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
                score = StringUtil.ComputeLevenshteinDistance(searchText.ToLower(), name /*.Substring(0, Math.Min(searchText.Length, name.Length))*/.ToLower());
            }

            if (score > -1)
            {
                diffs.Add(new WordScoreResult<T>(item, score));
            }
        }

        sw.Stop();
        this._logger.Debug($"Finished searching for \"{searchText}\" in {sw.Elapsed.TotalMilliseconds}ms. Found {diffs.Count} results.");

        IOrderedEnumerable<WordScoreResult<T>> ordered = diffs.OrderBy(x => x.DiffScore).ThenBy(x => this.GetSearchableProperty(x.Result).Length);
        return Task.FromResult(ordered.Take(this.Configuration.MaxSearchResults.Value).Select(x => this.CreateSearchResultItem(x.Result)));
    }

    public override void Dispose()
    {
        this.SearchItems?.Clear();
    }
}