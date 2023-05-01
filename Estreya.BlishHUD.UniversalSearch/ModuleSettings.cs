namespace Estreya.BlishHUD.UniversalSearch;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Settings;
using Estreya.BlishHUD.UniversalSearch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ModuleSettings : BaseModuleSettings
{

    private SettingCollection _searchHandlerSettings { get; set; }

    public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.U))
    {
    }

    protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
    {
    }

    protected override void InitializeAdditionalSettings(SettingCollection settings)
    {
        this._searchHandlerSettings = settings.AddSubCollection("SEARCH_HANDLERS");
    }

    public SearchHandlerConfiguration AddSearchHandler(string name, string displayName)
    {
        var enabled = this._searchHandlerSettings.DefineSetting($"{name}-enabled", true, () => "Enabled", () => "Defines if the search handler is enabled and shown in the search window. (Needs a window rebuild)");

        var searchMode = this._searchHandlerSettings.DefineSetting($"{name}-searchMode", SearchMode.Any, () => "Search Mode", () => "Defines the mode for filtering the results.");

        var minSearchResultsCount = 1;
        var maxSearchResultsCount = 50;
        var maxSearchResults = this._searchHandlerSettings.DefineSetting($"{name}-maxSearchResults", 5, () => "Max Search Results", () => $"Defines the max number of shown search results. (Min: {minSearchResultsCount}, Max: {maxSearchResultsCount})");
        maxSearchResults.SetRange(minSearchResultsCount, maxSearchResultsCount);

        var includeBrokenItems = this._searchHandlerSettings.DefineSetting($"{name}-includeBrokenItems", false, () => "Include Broken Items", () => "Defines if broken api items should be included. (e.g.: No Name, Placeholder Name, ...)");

        var notifyOnCopy = this._searchHandlerSettings.DefineSetting($"{name}-notifyOnCopy", true, () => "Notify on copying Result", () => "Whether a Screen Notification should be displayed after copying a result.");

        var closeWindowAfterCopy = this._searchHandlerSettings.DefineSetting($"{name}-closeWindowAfterCopy", true, () => "Close Window after Copy", () => "Whether the search window should be closed after a successful copy.");

        var pasteInChatAfterCopy = this._searchHandlerSettings.DefineSetting($"{name}-pasteInChatAfterCopy", true, () => "Paste in Chat after Copy", () => "Whether the copied information should be pasted in chat.");

        return new SearchHandlerConfiguration()
        {
            Name = displayName,
            Enabled = enabled,
            SearchMode = searchMode,
            MaxSearchResults = maxSearchResults,
            IncludeBrokenItem = includeBrokenItems,
            NotifyOnCopy = notifyOnCopy,
            CloseWindowAfterCopy = closeWindowAfterCopy,
            PasteInChatAfterCopy= pasteInChatAfterCopy,
        };
    }

    public void RemoveSearchHandler(string name)
    {
        this._searchHandlerSettings.UndefineSetting($"{name}-enabled");
        this._searchHandlerSettings.UndefineSetting($"{name}-searchMode");
        this._searchHandlerSettings.UndefineSetting($"{name}-maxSearchResults");
        this._searchHandlerSettings.UndefineSetting($"{name}-includeBrokenItems");
        this._searchHandlerSettings.UndefineSetting($"{name}-notifyOnCopy");
        this._searchHandlerSettings.UndefineSetting($"{name}-closeWindowAfterCopy");
        this._searchHandlerSettings.UndefineSetting($"{name}-pasteInChatAfterCopy");
    }
}
