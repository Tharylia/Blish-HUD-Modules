namespace Estreya.BlishHUD.UniversalSearch.Models;

using Blish_HUD.Settings;

public class SearchHandlerConfiguration
{
    public string Name { get; set; }
    public SettingEntry<bool> Enabled { get; set; }

    public SettingEntry<SearchMode> SearchMode { get; set; }

    public SettingEntry<int> MaxSearchResults { get; set; }

    public SettingEntry<bool> IncludeBrokenItem { get; set; }

    public SettingEntry<bool> NotifyOnCopy { get; set; }
    public SettingEntry<bool> CloseWindowAfterCopy { get; set; }

    public SettingEntry<bool> PasteInChatAfterCopy { get; set; }
}