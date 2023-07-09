namespace Estreya.BlishHUD.UniversalSearch.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Controls.SearchResults;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using Services.SearchHandlers;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dropdown = Shared.Controls.Dropdown;

public class SearchWindowView : BaseView
{
    private readonly ModuleSettings _moduleSettings;
    private readonly IDictionary<string, SearchHandler> _searchHandlers;
    private readonly SemaphoreSlim _searchSemaphore = new SemaphoreSlim(1, 1);
    private CancellationTokenSource _delayCancellationToken;

    private Task _delayTask;
    private Label _noResultsLabel;

    private FlowPanel _resultPanel;

    private readonly List<SearchResultItem> _results = new List<SearchResultItem>();

    private TextBox _searchbox;
    private Dropdown _searchHandlerSelect;
    private string _searchString;
    private SearchHandler _selectedSearchHandler;
    private LoadingSpinner _spinner;

    public SearchWindowView(IEnumerable<SearchHandler> searchHandlers, ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconState, TranslationService translationState, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
    {
        this._searchHandlers = searchHandlers.ToDictionary(x => x.Name, y => y);

        this._selectedSearchHandler = this._searchHandlers.FirstOrDefault().Value;
        this._moduleSettings = moduleSettings;
    }

    public event EventHandler RequestClose;

    protected override void InternalBuild(Panel parent)
    {
        Thickness outerPadding = new Thickness(10, 10, 0);
        this._searchHandlerSelect = this.RenderDropdown(parent, new Point((int)outerPadding.Left, (int)outerPadding.Top), 150, this._searchHandlers.Keys.ToArray(), this._selectedSearchHandler?.Name);
        this._searchHandlerSelect.ValueChanged += this.SearchHandlerSelectValueChanged;
        this._searchHandlerSelect.PanelHeight = 200;

        this._searchbox = this.RenderTextbox(parent,
            new Point(this._searchHandlerSelect.Right + 5, this._searchHandlerSelect.Top),
            parent.ContentRegion.Width - this._searchHandlerSelect.Width - ((int)outerPadding.Right + (int)outerPadding.Left), this._searchString,
            "Search");
        this._searchbox.TextChanged += this.SearchboxOnTextChanged;

        this._spinner = new LoadingSpinner
        {
            Location = (parent.ContentRegion.Size / new Point(2)) - new Point(32, 32),
            Visible = false,
            Parent = parent
        };

        this._noResultsLabel = new Label
        {
            Top = this._searchbox.Bottom + 5,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Middle,
            Width = parent.ContentRegion.Width,
            Visible = false,
            Text = "No Results",
            Parent = parent,
            Font = GameService.Content.DefaultFont16
        };
        this._noResultsLabel.Height = parent.ContentRegion.Height - this._noResultsLabel.Top;

        this._resultPanel = new FlowPanel
        {
            Parent = parent,
            Top = this._searchbox.Bottom + 3,
            Left = (int)outerPadding.Left,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true
        };
        this._resultPanel.Size = new Point(parent.ContentRegion.Width - ((int)outerPadding.Right + (int)outerPadding.Left), parent.ContentRegion.Height - this._resultPanel.Top);

        List<SearchResultItem> tempResults = this._results.ToArray().ToList();
        this.ClearResults(false);

        this.AddSearchResultItems(tempResults);

        if (!this._results.Any())
        {
            this._noResultsLabel.Show();
        }
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }

    private void SearchHandlerSelectValueChanged(object sender, ValueChangedEventArgs e)
    {
        this._selectedSearchHandler = this._searchHandlers[e.CurrentValue];
        this.Search();
    }

    private void AddSearchResultItems(IEnumerable<SearchResultItem> items)
    {
        foreach (SearchResultItem searchItem in items)
        {
            searchItem.Width = this._resultPanel.ContentRegion.Width - 20; // - 20 to account for scroll bar.
            searchItem.ClickActionExecuted += this.SearchItem_ClickActionExecuted;
            searchItem.Parent = this._resultPanel;
            this._results.Add(searchItem);
        }
    }

    private void SearchItem_ClickActionExecuted(object sender, bool successful)
    {
        SearchResultItem item = sender as SearchResultItem;
        if (!successful)
        {
            ScreenNotification.ShowNotification($"Could not copy data {item.Name}", ScreenNotification.NotificationType.Error);
            return;
        }

        if (this._selectedSearchHandler?.Configuration.NotifyOnCopy.Value ?? false)
        {
            ScreenNotification.ShowNotification($"Copied data for {item.Name}");
        }

        if (this._selectedSearchHandler?.Configuration.PasteInChatAfterCopy.Value ?? false)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    string clipboardContent = await ClipboardUtil.WindowsClipboardService.GetTextAsync();
                    GameService.GameIntegration.Chat.Send(clipboardContent);
                });
            }
            catch (Exception ex)
            {
                ScreenNotification.ShowNotification(ex.Message, ScreenNotification.NotificationType.Error);
            }
        }

        if (this._selectedSearchHandler?.Configuration.CloseWindowAfterCopy.Value ?? false)
        {
            this.RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ClearResults(bool dispose = true)
    {
        if (dispose)
        {
            this._results?.ForEach(r => r?.Dispose());
        }

        this._results?.Clear();

        this._resultPanel?.ClearChildren();
    }

    private bool HandlePrefix(string searchText)
    {
        const int MAX_PREFIX_LENGTH = 2;

        if (searchText.Length > 1 && searchText.Length <= MAX_PREFIX_LENGTH && searchText.EndsWith(" "))
        {
            searchText = searchText.Replace(" ", string.Empty);
            foreach (KeyValuePair<string, SearchHandler> possibleSearchHandler in this._searchHandlers)
            {
                if (possibleSearchHandler.Value.Prefix.Equals(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    // Temporarily remove event handler to prevent another search on combox change
                    this._searchHandlerSelect.ValueChanged -= this.SearchHandlerSelectValueChanged;
                    this._searchHandlerSelect.SelectedItem = possibleSearchHandler.Value.Name;
                    this._selectedSearchHandler = possibleSearchHandler.Value;
                    this._searchHandlerSelect.ValueChanged += this.SearchHandlerSelectValueChanged;

                    this._searchbox.Text = string.Empty;
                    return false;
                }
            }
        }

        return true;
    }

    private async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        await this._searchSemaphore.WaitAsync(cancellationToken);
        try
        {
            this.ClearResults();

            cancellationToken.ThrowIfCancellationRequested();

            string searchText = this._searchbox.Text;

            if (!this.HandlePrefix(searchText) || searchText.Length <= 2)
            {
                this._noResultsLabel.Show();
                return;
            }

            this._noResultsLabel.Hide();
            this._spinner.Show();

            IEnumerable<SearchResultItem> results = await this._selectedSearchHandler.SearchAsync(searchText);
            this.AddSearchResultItems(results);

            this._spinner.Hide();

            if (!this._results.Any())
            {
                this._noResultsLabel.Show();
            }
        }
        finally
        {
            this._searchSemaphore.Release();
        }
    }

    private void SearchboxOnTextChanged(object sender, EventArgs e)
    {
        this._searchString = this._searchbox.Text;
        this.Search();
    }

    private void Search()
    {
        try
        {
            if (!this.HandlePrefix(this._searchbox.Text))
            {
                return;
            }

            if (this._delayTask != null)
            {
                this._delayCancellationToken.Cancel();
                this._delayTask = null;
                this._delayCancellationToken = null;
            }

            this._delayCancellationToken = new CancellationTokenSource();
            this._delayTask = new Task(async () => await this.DelaySeach(this._delayCancellationToken.Token), this._delayCancellationToken.Token);
            this._delayTask.Start();
        }
        catch (Exception)
        {
        }
    }

    private async Task DelaySeach(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(300, cancellationToken);
            await this.SearchAsync(cancellationToken);
        }
        catch (OperationCanceledException) { }
    }

    protected override void Unload()
    {
        this.ClearResults();

        base.Unload();
    }
}