namespace Estreya.BlishHUD.UniversalSearch;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework.Input;
using Models;
using Shared.Settings;

public class ModuleSettings : BaseModuleSettings
{
    public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(ModifierKeys.Alt, Keys.U))
    {
    }

    private SettingCollection _searchHandlerSettings { get; set; }

    protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
    {
    }

    protected override void InitializeAdditionalSettings(SettingCollection settings)
    {
        this._searchHandlerSettings = settings.AddSubCollection("SEARCH_HANDLERS");
    }

    public SearchHandlerConfiguration AddSearchHandler(string name, string displayName)
    {
        SettingEntry<bool> enabled = this._searchHandlerSettings.DefineSetting($"{name}-enabled", true, () => "Enabled", () => "Defines if the search handler is enabled and shown in the search window. (Needs a window rebuild)");

        SettingEntry<SearchMode> searchMode = this._searchHandlerSettings.DefineSetting($"{name}-searchMode", SearchMode.Any, () => "Search Mode", () => "Defines the mode for filtering the results.");

        int minSearchResultsCount = 1;
        int maxSearchResultsCount = 50;
        SettingEntry<int> maxSearchResults = this._searchHandlerSettings.DefineSetting($"{name}-maxSearchResults", 5, () => "Max Search Results", () => $"Defines the max number of shown search results. (Min: {minSearchResultsCount}, Max: {maxSearchResultsCount})");
        maxSearchResults.SetRange(minSearchResultsCount, maxSearchResultsCount);

        SettingEntry<bool> includeBrokenItems = this._searchHandlerSettings.DefineSetting($"{name}-includeBrokenItems", false, () => "Include Broken Items", () => "Defines if broken api items should be included. (e.g.: No Name, Placeholder Name, ...)");

        SettingEntry<bool> notifyOnCopy = this._searchHandlerSettings.DefineSetting($"{name}-notifyOnCopy", true, () => "Notify on copying Result", () => "Whether a Screen Notification should be displayed after copying a result.");

        SettingEntry<bool> closeWindowAfterCopy = this._searchHandlerSettings.DefineSetting($"{name}-closeWindowAfterCopy", true, () => "Close Window after Copy", () => "Whether the search window should be closed after a successful copy.");

        SettingEntry<bool> pasteInChatAfterCopy = this._searchHandlerSettings.DefineSetting($"{name}-pasteInChatAfterCopy", true, () => "Paste in Chat after Copy", () => "Whether the copied information should be pasted in chat.");

        return new SearchHandlerConfiguration
        {
            Name = displayName,
            Enabled = enabled,
            SearchMode = searchMode,
            MaxSearchResults = maxSearchResults,
            IncludeBrokenItem = includeBrokenItems,
            NotifyOnCopy = notifyOnCopy,
            CloseWindowAfterCopy = closeWindowAfterCopy,
            PasteInChatAfterCopy = pasteInChatAfterCopy
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