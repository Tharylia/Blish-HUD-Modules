namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;

using Controls.SearchResults;
using Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public abstract class SearchHandler : IDisposable
{
    public SearchHandler(SearchHandlerConfiguration configuration)
    {
        this.Configuration = configuration;
    }

    public SearchHandlerConfiguration Configuration { get; }

    public string Name => this.Configuration.Name;

    public abstract string Prefix { get; }

    public abstract void Dispose();

    public abstract Task<IEnumerable<SearchResultItem>> SearchAsync(string searchText);
}