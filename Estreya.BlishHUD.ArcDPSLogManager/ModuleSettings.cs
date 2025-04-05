namespace Estreya.BlishHUD.ArcDPSLogManager;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Shared.Settings;

public class ModuleSettings : BaseModuleSettings
{
    public SettingEntry<bool> GenerateHTMLAfterParsing { get; private set; }
    public SettingEntry<bool> AnonymousPlayers { get; private set; }
    public SettingEntry<bool> SkipFailedTries { get; private set; } 
    public SettingEntry<bool> ParsePhases { get; private set; }
    public SettingEntry<bool> ParseCombatReplay {  get; private set; }
    public SettingEntry<bool> ComputeDamageModifiers {  get; private set; }
    public SettingEntry<int> TooShortLimit { get; private set; }
    public SettingEntry<bool> DetailedWvW {  get; private set; }

    public ModuleSettings(SettingCollection settings, SemVer.Version moduleVersion) : base(settings, moduleVersion, new KeyBinding())
    {
    }

    protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
    {
        this.GenerateHTMLAfterParsing = globalSettingCollection.DefineSetting(nameof(this.GenerateHTMLAfterParsing), false, () => "Generate HTML after Parsing", () => "Defines if a html file should be generated after log parsing is finished.");

        this.AnonymousPlayers = globalSettingCollection.DefineSetting(nameof(this.AnonymousPlayers), false, () => "Anonymous Players", () => "Replaces players character and account names with generic names.");

        this.SkipFailedTries = globalSettingCollection.DefineSetting(nameof(this.SkipFailedTries), false, () => "Skip Failed Tries", () => "Skip parsing of failed tries.");

        this.ParsePhases = globalSettingCollection.DefineSetting(nameof(this.ParsePhases), true, () => "Parse Phases", () => "Parses the phases of the log.");

        this.ParseCombatReplay = globalSettingCollection.DefineSetting(nameof(this.ParseCombatReplay), false, () => "Parse Combat Replay", () => "Parses a replay of the combat for the html output.");

        this.ComputeDamageModifiers = globalSettingCollection.DefineSetting(nameof(this.ComputeDamageModifiers), true, () => "Compute Damage Modifiers", () => null);

        this.TooShortLimit = globalSettingCollection.DefineSetting(nameof(this.TooShortLimit), 2000, () => "Too Short Limit", () => "The limit under which logs are not parsed for being too short.");

        this.DetailedWvW = globalSettingCollection.DefineSetting(nameof(this.DetailedWvW), false, () => "Detailed WvW", () => "Parsed detailed information for wvw logs.");
    }
}