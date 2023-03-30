namespace Estreya.BlishHUD.UniversalSearch.UI.Views;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class SearchWindowView : BaseView
{
    private readonly IDictionary<string, SearchHandler> _searchHandlers;

    private List<SearchResultItem> _results;
    private SearchHandler _selectedSearchHandler;

    private FlowPanel _resultPanel;

    private TextBox _searchbox;
    private LoadingSpinner _spinner;
    private Label _noneLabel;
    private Dropdown _searchHandlerSelect;

    private Task _delayTask;
    private CancellationTokenSource _delayCancellationToken;
    private readonly SemaphoreSlim _searchSemaphore = new SemaphoreSlim(1, 1);
    private readonly ModuleSettings _moduleSettings;

    public event EventHandler RequestClose;

    public SearchWindowView(IEnumerable<SearchHandler> searchHandlers, ModuleSettings moduleSettings,  Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
    {
        this._searchHandlers = searchHandlers.ToDictionary(x => x.Name, y => y);

        this._selectedSearchHandler = this._searchHandlers.FirstOrDefault().Value;
        this._moduleSettings = moduleSettings;
    }

    protected override void InternalBuild(Panel parent)
    {
        this._searchHandlerSelect = new Dropdown()
        {
            Top = 10,
            Size = new Point(100, Dropdown.Standard.Size.Y),
            SelectedItem = this._selectedSearchHandler?.Name,
            Parent = parent,
        };

        foreach (KeyValuePair<string, SearchHandler> searchHandler in this._searchHandlers)
        {
            this._searchHandlerSelect.Items.Add(searchHandler.Key);
        }

        this._searchHandlerSelect.ValueChanged += this.SearchHandlerSelectValueChanged;

        this._searchbox = new TextBox()
        {
            Top = _searchHandlerSelect.Top,
            Left = this._searchHandlerSelect.Right + 5,
            Size = new Point(parent.ContentRegion.Width - _searchHandlerSelect.Width, TextBox.Standard.Size.Y),
            PlaceholderText = "Search",
            Parent = parent
        };

        this._spinner = new LoadingSpinner()
        {
            Location = parent.ContentRegion.Size / new Point(2) - new Point(32, 32),
            Visible = false,
            Parent = parent
        };

        this._noneLabel = new Label()
        {
            Size = parent.ContentRegion.Size - new Point(0, TextBox.Standard.Size.Y * 2),
            Location = new Point(0, TextBox.Standard.Size.Y),
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = false,
            Text = "No Results",
            Parent = parent
        };

        this._results = new List<SearchResultItem>(SearchHandler.MAX_RESULT_COUNT); 
        this._searchbox.TextChanged += this.SearchboxOnTextChanged;

        this._resultPanel = new FlowPanel()
        {
            Parent = parent,
            Top = _searchbox.Bottom+3,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true
        };
        this._resultPanel.Size = new Point(parent.ContentRegion.Width, parent.ContentRegion.Height - this._resultPanel.Top);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);

    private void SearchHandlerSelectValueChanged(object sender, ValueChangedEventArgs e)
    {
        this._selectedSearchHandler = this._searchHandlers[e.CurrentValue];
        this.Search();

    }

    private void AddSearchResultItems(IEnumerable<SearchResultItem> items)
    {
        foreach (SearchResultItem searchItem in items)
        {
            searchItem.Width = this._resultPanel.ContentRegion.Width;
            searchItem.ClickActionExecuted += this.SearchItem_ClickActionExecuted;
            searchItem.Parent = this._resultPanel;
            this._results.Add(searchItem);
        }
    }

    private void SearchItem_ClickActionExecuted(object sender, bool successful)
    {
        var item = sender as SearchResultItem;
        if (!successful)
        {
            ScreenNotification.ShowNotification($"Could not copy data {item.Name}", ScreenNotification.NotificationType.Error);
            return;
        }

        if (this._moduleSettings.NotifyOnCopy.Value)
        {
            ScreenNotification.ShowNotification($"Copied data for {item.Name}");
        }

        if (this._moduleSettings.PasteInChatAfterCopy.Value)
        {
            try
            {
                _ = Task.Run(async () =>
                   {
                       var clipboardContent = await ClipboardUtil.WindowsClipboardService.GetTextAsync();
                       GameService.GameIntegration.Chat.Send(clipboardContent);
                   });
            }
            catch (Exception ex)
            {
                
            }
        }

        if (this._moduleSettings.CloseWindowAfterCopy.Value)
        {
            this.RequestClose?.Invoke(this, EventArgs.Empty);
        }
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
            this._results.ForEach(r => r.Dispose());
            this._results.Clear();

            cancellationToken.ThrowIfCancellationRequested();

            string searchText = this._searchbox.Text;

            if (!this.HandlePrefix(searchText) || searchText.Length <= 2)
            {
                this._noneLabel.Show();
                return;
            }

            this._noneLabel.Hide();
            this._spinner.Show();

            this.AddSearchResultItems(this._selectedSearchHandler.Search(searchText));

            this._spinner.Hide();

            if (!this._results.Any())
            {
                this._noneLabel.Show();
            }
        }
        finally
        {
            this._searchSemaphore.Release();
        }
    }

    private void SearchboxOnTextChanged(object sender, EventArgs e)
    {
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
        catch (OperationCanceledException)
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
}
