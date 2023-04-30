namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;

using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Estreya.BlishHUD.UniversalSearch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class SearchHandler: IDisposable
{
    public SearchHandlerConfiguration Configuration { get; private set; }

    public string Name => this.Configuration.Name;

    public abstract string Prefix { get; }

    public SearchHandler(SearchHandlerConfiguration configuration)
    {
        this.Configuration = configuration;
    }

    public abstract Task<IEnumerable<SearchResultItem>> SearchAsync(string searchText);

    public abstract void Dispose();
}