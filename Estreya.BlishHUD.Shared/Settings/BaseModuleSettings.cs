namespace Estreya.BlishHUD.Shared.Settings;

using Blish_HUD;
using Blish_HUD.Input;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Extensions;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework.Input;
using Models;
using Models.Drawers;
using MonoGame.Extended;
using Newtonsoft.Json;
using SemVer;
using Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using static Blish_HUD.ContentService;

/// <summary>
///     Defines the base settings used by all modules.
/// </summary>
public abstract class BaseModuleSettings
{
    /// <summary>
    ///     The base settings collection passed by blish hud core.
    /// </summary>
    protected readonly SettingCollection _settings;

    private KeyBinding _globalEnabledKeybinding;
    protected readonly Logger Logger;

    /// <summary>
    ///     Creates a new base module settings instance.
    /// </summary>
    /// <param name="settings">The base settings passed by blish hud core.</param>
    /// <param name="globalEnabledKeybinding">The global keybinding used to trigger ui visibility.</param>
    protected BaseModuleSettings(SettingCollection settings, KeyBinding globalEnabledKeybinding)
    {
        this.Logger = Logger.GetLogger(this.GetType());

        this._settings = settings;
        this._globalEnabledKeybinding = globalEnabledKeybinding;
        this.BuildDefaultColor();

        this.InitializeGlobalSettings(this._settings);

        this.InitializeDrawerSettings(this._settings);

        this.InitializeAdditionalSettings(this._settings);
    }

    /// <summary>
    ///     Gets the default gw2 color (dye remover).
    /// </summary>
    public Color DefaultGW2Color { get; private set; }

    /// <summary>
    ///     Initializes the drawer settings collection.
    /// </summary>
    /// <param name="settings"></param>
    private void InitializeDrawerSettings(SettingCollection settings)
    {
        this.DrawerSettings = settings.AddSubCollection(DRAWER_SETTINGS);
    }

    /// <summary>
    ///     Builds the default gw2 color (dye remover).
    /// </summary>
    private void BuildDefaultColor()
    {
        this.DefaultGW2Color = new Color
        {
            Name = "Dye Remover",
            Id = 1,
            BaseRgb = new List<int>
            {
                128,
                26,
                26
            },
            Cloth = new ColorMaterial
            {
                Brightness = 15,
                Contrast = 1.25,
                Hue = 38,
                Saturation = 0.28125,
                Lightness = 1.44531,
                Rgb = new List<int>
                {
                    124,
                    108,
                    83
                }
            },
            Leather = new ColorMaterial
            {
                Brightness = -8,
                Contrast = 1.0,
                Hue = 34,
                Saturation = 0.3125,
                Lightness = 1.09375,
                Rgb = new List<int>
                {
                    65,
                    49,
                    29
                }
            },
            Metal = new ColorMaterial
            {
                Brightness = 5,
                Contrast = 1.05469,
                Hue = 38,
                Saturation = 0.101563,
                Lightness = 1.36719,
                Rgb = new List<int>
                {
                    96,
                    91,
                    83
                }
            },
            Fur = new ColorMaterial
            {
                Brightness = 15,
                Contrast = 1.25,
                Hue = 38,
                Saturation = 0.28125,
                Lightness = 1.44531,
                Rgb = new List<int>
                {
                    124,
                    108,
                    83
                }
            }
        };
    }

    /// <summary>
    ///     Used to add additional settings apart from the global settings.
    /// </summary>
    /// <param name="settings"></param>
    protected virtual void InitializeAdditionalSettings(SettingCollection settings) { /* NOOP */ }

    /// <summary>
    ///     Initializes all base defined global settings.
    /// </summary>
    /// <param name="settings"></param>
    private void InitializeGlobalSettings(SettingCollection settings)
    {
        this.GlobalSettings = settings.AddSubCollection(GLOBAL_SETTINGS);

        this.GlobalDrawerVisible = this.GlobalSettings.DefineSetting(nameof(this.GlobalDrawerVisible), true, () => "Global Visible", () => "Whether the modules drawers should be visible.");

        bool globalHotkeyEnabled = this._globalEnabledKeybinding != null;
        if (this._globalEnabledKeybinding == null)
        {
            this._globalEnabledKeybinding = new KeyBinding();
            this.Logger.Debug("No default keybinding defined. Building temp empty keybinding. Enabled = {0}", globalHotkeyEnabled);
        }

        this.GlobalDrawerVisibleHotkey = this.GlobalSettings.DefineSetting(nameof(this.GlobalDrawerVisibleHotkey), this._globalEnabledKeybinding, () => "Global Visible Hotkey", () => "Defines the hotkey used to toggle the global visibility.");
        this.GlobalDrawerVisibleHotkey.Value.Enabled = globalHotkeyEnabled;
        this.GlobalDrawerVisibleHotkey.Value.Activated += this.GlobalEnabledHotkey_Activated;
        this.GlobalDrawerVisibleHotkey.Value.IgnoreWhenInTextField = true;
        this.GlobalDrawerVisibleHotkey.Value.BlockSequenceFromGw2 = globalHotkeyEnabled;

        this.RegisterCornerIcon = this.GlobalSettings.DefineSetting(nameof(this.RegisterCornerIcon), true, () => "Register Corner Icon", () => "Whether the module should register a corner icon.");
        this.RegisterCornerIcon.SettingChanged += this.RegisterCornerIcon_SettingChanged;

        this.CornerIconLeftClickAction = this.GlobalSettings.DefineSetting(nameof(this.CornerIconLeftClickAction), CornerIconClickAction.Settings, () => "Corner Icon Left Click Action", () => "Defines the action of the corner icon when left clicked.");

        this.CornerIconRightClickAction = this.GlobalSettings.DefineSetting(nameof(this.CornerIconRightClickAction), CornerIconClickAction.None, () => "Corner Icon Right Click Action", () => "Defines the action of the corner icon when right clicked.");

        this.HideOnOpenMap = this.GlobalSettings.DefineSetting(nameof(this.HideOnOpenMap), true, () => "Hide on open Map", () => "Whether the modules drawers should hide when the map is open.");

        this.HideOnMissingMumbleTicks = this.GlobalSettings.DefineSetting(nameof(this.HideOnMissingMumbleTicks), true, () => "Hide on Cutscenes", () => "Whether the modules drawers should hide when cutscenes are played.");

        this.HideInCombat = this.GlobalSettings.DefineSetting(nameof(this.HideInCombat), false, () => "Hide in Combat", () => "Whether the modules drawers should hide when in combat.");

        this.HideInPvE_OpenWorld = this.GlobalSettings.DefineSetting(nameof(this.HideInPvE_OpenWorld), false, () => "Hide in PvE (Open World)", () => "Whether the drawers should hide when in PvE (Open World).");

        this.HideInPvE_Competetive = this.GlobalSettings.DefineSetting(nameof(this.HideInPvE_Competetive), false, () => "Hide in PvE (Competetive)", () => "Whether the drawers should hide when in PvE (Competetive).");

        this.HideInWvW = this.GlobalSettings.DefineSetting(nameof(this.HideInWvW), false, () => "Hide in WvW", () => "Whether the drawers should hide when in world vs. world.");

        this.HideInPvP = this.GlobalSettings.DefineSetting(nameof(this.HideInPvP), false, () => "Hide in PvP", () => "Whether the drawers should hide when in player vs. player.");

        this.DebugEnabled = this.GlobalSettings.DefineSetting(nameof(this.DebugEnabled), false, () => "Debug Enabled", () => "Whether the module runs in debug mode.");

        this.UseDebugAPI = this.GlobalSettings.DefineSetting(nameof(this.UseDebugAPI), false, () => "Use Debug API", () => "Whether the module connects to the debug blish-hud api.\nRequires a restart to take full effect.");

        this.BlishAPIUsername = this.GlobalSettings.DefineSetting(nameof(this.BlishAPIUsername), (string)null, () => "Blish API Username", () => "Defines the login username for the Estreya Blish HUD API.");

        this.RegisterContext = this.GlobalSettings.DefineSetting(nameof(this.RegisterContext), true, () => "Register Context", () => "Whether the module should register an api context for cross module interaction. Requires a restart.");

        this.SendMetrics = this.GlobalSettings.DefineSetting(nameof(this.SendMetrics), false, () => "Send Anonymous Metrics", () => "Allows the module to send anonymous metric data to a backend server to view advanced usage statistics.");

        this.AskedMetricsConsent = this.GlobalSettings.DefineSetting(nameof(this.AskedMetricsConsent), false, () => "Asked Metrics Consent", () => "Whether the module asked for metric consent.");

        this.MetricsConsentGivenVersion = this.GlobalSettings.DefineSetting(nameof(this.MetricsConsentGivenVersion), new SemVer.Version("0.0.0"), () => "Metrics Consent Version", () => "Defines the version at which point a metric consent was given.");

        //this.NotifiedNews = this.GlobalSettings.DefineSetting(nameof(this.NotifiedNews), new List<string>(), () => "Notified News", () => "The news already notified about.");
        //this.NotifyOnUnreadNews = this.GlobalSettings.DefineSetting(nameof(this.NotifyOnUnreadNews), true, () => "Notify on unread News", () => "Whether the module should notify you when new news arrive.");

        this.HandleEnabledStates();

        this.DoInitializeGlobalSettings(this.GlobalSettings);

        this.GlobalSettings.AddLoggingEvents();
    }

    /// <summary>
    ///     Handles the changed event for <see cref="RegisterCornerIcon"/>.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The value changed event args.</param>
    private void RegisterCornerIcon_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        this.HandleEnabledStates();
    }

    /// <summary>
    ///     Handles the changed event for <see cref="GlobalDrawerVisible"/>.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event args.</param>
    private void GlobalEnabledHotkey_Activated(object sender, EventArgs e)
    {
        this.GlobalDrawerVisible.Value = !this.GlobalDrawerVisible.Value;
    }

    /// <summary>
    ///     Handles the enabled state changes.
    /// </summary>
    private void HandleEnabledStates()
    {
        this.CornerIconLeftClickAction.SetDisabled(!this.RegisterCornerIcon.Value);
        this.CornerIconRightClickAction.SetDisabled(!this.RegisterCornerIcon.Value);
    }

    /// <summary>
    ///     Used to initialize additional global settings.
    /// </summary>
    /// <param name="globalSettingCollection"></param>
    protected virtual void DoInitializeGlobalSettings(SettingCollection globalSettingCollection) { /* NOOP */ }

    /// <summary>
    ///     Adds a new base drawer.
    /// </summary>
    /// <param name="name">The name of the new drawer.</param>
    /// <param name="defaultBuildDirection">The default build direction of the drawer.</param>
    /// <returns>The newly created configuration.</returns>
    public DrawerConfiguration AddDrawer(string name, BuildDirection defaultBuildDirection = BuildDirection.Top)
    {
        int maxHeight = 1080;
        int maxWidth = 1920;

        SettingEntry<bool> enabled = this.DrawerSettings.DefineSetting($"{name}-enabled", true, () => "Enabled", () => "Whether the drawer is enabled.");
        SettingEntry<KeyBinding> enabledKeybinding = this.DrawerSettings.DefineSetting($"{name}-enabledKeybinding", new KeyBinding(), () => "Enabled Keybinding", () => "Defines the keybinding to toggle this drawer on and off.");
        enabledKeybinding.Value.Enabled = true;
        enabledKeybinding.Value.IgnoreWhenInTextField = true;
        enabledKeybinding.Value.BlockSequenceFromGw2 = true;

        SettingEntry<int> locationX = this.DrawerSettings.DefineSetting($"{name}-locationX", (int)(maxWidth * 0.1), () => "Location X", () => "Defines the position on the x axis.");
        locationX.SetRange(0, maxWidth);
        SettingEntry<int> locationY = this.DrawerSettings.DefineSetting($"{name}-locationY", (int)(maxHeight * 0.1), () => "Location Y", () => "Defines the position on the y axis.");
        locationY.SetRange(0, maxHeight);
        SettingEntry<int> width = this.DrawerSettings.DefineSetting($"{name}-width", (int)(maxWidth * 0.5), () => "Width", () => "The width of the drawer.");
        width.SetRange(0, maxWidth);
        SettingEntry<int> height = this.DrawerSettings.DefineSetting($"{name}-height", (int)(maxHeight * 0.25), () => "Height", () => "The height of the drawer.");
        height.SetRange(0, maxHeight);

        SettingEntry<BuildDirection> buildDirection = this.DrawerSettings.DefineSetting($"{name}-buildDirection", defaultBuildDirection, () => "Build Direction", () => "The build direction of the drawer.");
        SettingEntry<float> opacity = this.DrawerSettings.DefineSetting($"{name}-opacity", 1f, () => "Opacity", () => "The opacity of the drawer.");
        opacity.SetRange(0f, 1f);
        SettingEntry<Color> backgroundColor = this.DrawerSettings.DefineSetting($"{name}-backgroundColor", this.DefaultGW2Color, () => "Background Color", () => "The background color of the drawer.");
        SettingEntry<FontSize> fontSize = this.DrawerSettings.DefineSetting($"{name}-fontSize", FontSize.Size16, () => "Font Size", () => "The font size of the drawer.");
        SettingEntry<Models.FontFace> fontFace = this.DrawerSettings.DefineSetting($"{name}-fontFace", Models.FontFace.Menomonia, () => "Font Face", () => "The font face of the drawer.");
        SettingEntry<string> customFontPath = this.DrawerSettings.DefineSetting($"{name}-customFontPath", (string)null, () => "Custom Font Path", () => "The path to a custom font file.");

        SettingEntry<Color> textColor = this.DrawerSettings.DefineSetting($"{name}-textColor", this.DefaultGW2Color, () => "Text Color", () => "The text color of the drawer.");

        DrawerConfiguration configuration = new DrawerConfiguration
        {
            Name = name,
            Enabled = enabled,
            EnabledKeybinding = enabledKeybinding,
            Location = new DrawerLocation
            {
                X = locationX,
                Y = locationY
            },
            Size = new DrawerSize
            {
                X = width,
                Y = height
            },
            BuildDirection = buildDirection,
            Opacity = opacity,
            BackgroundColor = backgroundColor,
            FontSize = fontSize,
            FontFace = fontFace,
            CustomFontPath = customFontPath,
            TextColor = textColor
        };

        return configuration;
    }

    /// <summary>
    ///     Removes drawer settings with the given name.
    /// </summary>
    /// <param name="name">The name of the drawer.</param>
    public void RemoveDrawer(string name)
    {
        this.DrawerSettings.UndefineSetting($"{name}-enabled");
        this.DrawerSettings.UndefineSetting($"{name}-enabledKeybinding");
        this.DrawerSettings.UndefineSetting($"{name}-locationX");
        this.DrawerSettings.UndefineSetting($"{name}-locationY");
        this.DrawerSettings.UndefineSetting($"{name}-width");
        this.DrawerSettings.UndefineSetting($"{name}-height");
        this.DrawerSettings.UndefineSetting($"{name}-buildDirection");
        this.DrawerSettings.UndefineSetting($"{name}-opacity");
        this.DrawerSettings.UndefineSetting($"{name}-backgroundColor");
        this.DrawerSettings.UndefineSetting($"{name}-fontSize");
        this.DrawerSettings.UndefineSetting($"{name}-fontFace");
        this.DrawerSettings.UndefineSetting($"{name}-customFontPath");
        this.DrawerSettings.UndefineSetting($"{name}-textColor");
    }

    public bool IsMaxResolutionValid(int width, int height)
    {
        return width >= 100 && height >= 100;
    }

    /// <summary>
    ///     Checks drawer size and position settings.
    /// </summary>
    /// <param name="configuration">The configuration to perform the check on.</param>
    public void CheckDrawerSizeAndPosition(DrawerConfiguration configuration)
    {
        bool buildFromBottom = configuration.BuildDirection.Value == BuildDirection.Bottom;
        int maxResX = (int)(GameService.Graphics.Resolution.X / GameService.Graphics.UIScaleMultiplier);
        int maxResY = (int)(GameService.Graphics.Resolution.Y / GameService.Graphics.UIScaleMultiplier);

        if (!this.IsMaxResolutionValid(maxResX, maxResY))
        {
            this.Logger.Warn($"Max drawer size and position resolution is invalid. X: {maxResX} - Y: {maxResY}");
            return;
        }

        int minLocationX = 0;
        int maxLocationX = maxResX - configuration.Size.X.Value;
        int minLocationY = buildFromBottom ? configuration.Size.Y.Value : 0;
        int maxLocationY = buildFromBottom ? maxResY : maxResY - configuration.Size.Y.Value;
        int minWidth = 0;
        int maxWidth = maxResX - configuration.Location.X.Value;
        int minHeight = 0;
        int maxHeight = maxResY - configuration.Location.Y.Value;

        if (maxLocationX < 50 || maxLocationY < 50)
        {
            //Logger.Debug($"Max Location X or Y has a small value which seems unreasonable. X: {maxLocationX}, Y: {maxLocationY}"); // Has the potential to spam log
            //return;
        }

        if (maxWidth < 50 || maxHeight < 50)
        {
            //Logger.Debug($"Max width or height has a small value which seems unreasonable. X: {maxWidth}, Y: {maxHeight}"); // Has the potential to spam log
            //return;
        }

        configuration.Location.X.SetRange(minLocationX, maxLocationX);
        configuration.Location.Y.SetRange(minLocationY, maxLocationY);
        configuration.Size.X.SetRange(minWidth, maxWidth);
        configuration.Size.Y.SetRange(minHeight, maxHeight);
    }

    /// <summary>
    ///     Updates the locatilizations of settings.
    /// </summary>
    /// <param name="translationService">The translation services used to fetch translations.</param>
    public virtual void UpdateLocalization(TranslationService translationService)
    {
        string globalDrawerVisibleDisplayNameDefault = this.GlobalDrawerVisible.DisplayName;
        string globalDrawerVisibleDescriptionDefault = this.GlobalDrawerVisible.Description;
        this.GlobalDrawerVisible.GetDisplayNameFunc = () => translationService.GetTranslation("setting-globalDrawerVisible-name", globalDrawerVisibleDisplayNameDefault);
        this.GlobalDrawerVisible.GetDescriptionFunc = () => translationService.GetTranslation("setting-globalDrawerVisible-description", globalDrawerVisibleDescriptionDefault);

        string globalDrawerVisibleHotkeyDisplayNameDefault = this.GlobalDrawerVisibleHotkey.DisplayName;
        string globalDrawerVisibleHotkeyDescriptionDefault = this.GlobalDrawerVisibleHotkey.Description;
        this.GlobalDrawerVisibleHotkey.GetDisplayNameFunc = () => translationService.GetTranslation("setting-globalDrawerVisibleHotkey-name", globalDrawerVisibleHotkeyDisplayNameDefault);
        this.GlobalDrawerVisibleHotkey.GetDescriptionFunc = () => translationService.GetTranslation("setting-globalDrawerVisibleHotkey-description", globalDrawerVisibleHotkeyDescriptionDefault);

        string registerCornerIconDisplayNameDefault = this.RegisterCornerIcon.DisplayName;
        string registerCornerIconDescriptionDefault = this.RegisterCornerIcon.Description;
        this.RegisterCornerIcon.GetDisplayNameFunc = () => translationService.GetTranslation("setting-registerCornerIcon-name", registerCornerIconDisplayNameDefault);
        this.RegisterCornerIcon.GetDescriptionFunc = () => translationService.GetTranslation("setting-registerCornerIcon-description", registerCornerIconDescriptionDefault);

        string cornerIconLeftClickActionDisplayNameDefault = this.CornerIconLeftClickAction.DisplayName;
        string cornerIconLeftClickActionDescriptionDefault = this.CornerIconLeftClickAction.Description;
        this.CornerIconLeftClickAction.GetDisplayNameFunc = () => translationService.GetTranslation("setting-cornerIconLeftClickAction-name", cornerIconLeftClickActionDisplayNameDefault);
        this.CornerIconLeftClickAction.GetDescriptionFunc = () => translationService.GetTranslation("setting-cornerIconLeftClickAction-description", cornerIconLeftClickActionDescriptionDefault);

        string cornerIconRightClickActionDisplayNameDefault = this.CornerIconRightClickAction.DisplayName;
        string cornerIconRightClickActionDescriptionDefault = this.CornerIconRightClickAction.Description;
        this.CornerIconRightClickAction.GetDisplayNameFunc = () => translationService.GetTranslation("setting-cornerIconRightClickAction-name", cornerIconRightClickActionDisplayNameDefault);
        this.CornerIconRightClickAction.GetDescriptionFunc = () => translationService.GetTranslation("setting-cornerIconRightClickAction-description", cornerIconRightClickActionDescriptionDefault);

        string hideOnOpenMapDisplayNameDefault = this.HideOnOpenMap.DisplayName;
        string hideOnOpenMapDescriptionDefault = this.HideOnOpenMap.Description;
        this.HideOnOpenMap.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideOnOpenMap-name", hideOnOpenMapDisplayNameDefault);
        this.HideOnOpenMap.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideOnOpenMap-description", hideOnOpenMapDescriptionDefault);

        string hideOnMissingMumbleTickDisplayNameDefault = this.HideOnMissingMumbleTicks.DisplayName;
        string hideOnMissingMumbleTickDescriptionDefault = this.HideOnMissingMumbleTicks.Description;
        this.HideOnMissingMumbleTicks.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideOnMissingMumbleTick-name", hideOnMissingMumbleTickDisplayNameDefault);
        this.HideOnMissingMumbleTicks.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideOnMissingMumbleTick-description", hideOnMissingMumbleTickDescriptionDefault);

        string hideInCombatDisplayNameDefault = this.HideInCombat.DisplayName;
        string hideInCombatDescriptionDefault = this.HideInCombat.Description;
        this.HideInCombat.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideInCombat-name", hideInCombatDisplayNameDefault);
        this.HideInCombat.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideInCombat-description", hideInCombatDescriptionDefault);

        string hideInPVEOpenWorldDisplayNameDefault = this.HideInPvE_OpenWorld.DisplayName;
        string hideInPVEOpenWorldDescriptionDefault = this.HideInPvE_OpenWorld.Description;
        this.HideInPvE_OpenWorld.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideInPVEOpenWorld-name", hideInPVEOpenWorldDisplayNameDefault);
        this.HideInPvE_OpenWorld.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideInPVEOpenWorld-description", hideInPVEOpenWorldDescriptionDefault);

        string hideInPVECompetetiveDisplayNameDefault = this.HideInPvE_Competetive.DisplayName;
        string hideInPVECompetetiveDescriptionDefault = this.HideInPvE_Competetive.Description;
        this.HideInPvE_Competetive.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideInPVECompetetive-name", hideInPVECompetetiveDisplayNameDefault);
        this.HideInPvE_Competetive.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideInPVECompetetive-description", hideInPVECompetetiveDescriptionDefault);

        string hideInWVWDisplayNameDefault = this.HideInWvW.DisplayName;
        string hideInWVWDescriptionDefault = this.HideInWvW.Description;
        this.HideInWvW.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideInWVW-name", hideInWVWDisplayNameDefault);
        this.HideInWvW.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideInWVW-description", hideInWVWDescriptionDefault);

        string hideInPVPDisplayNameDefault = this.HideInPvP.DisplayName;
        string hideInPVPDescriptionDefault = this.HideInPvP.Description;
        this.HideInPvP.GetDisplayNameFunc = () => translationService.GetTranslation("setting-hideInPVP-name", hideInPVPDisplayNameDefault);
        this.HideInPvP.GetDescriptionFunc = () => translationService.GetTranslation("setting-hideInPVP-description", hideInPVPDescriptionDefault);
    }

    /// <summary>
    ///     Updates drawer localizations.
    /// </summary>
    /// <param name="drawerConfiguration">The configuration to update.</param>
    /// <param name="translationService">The translation services used to fetch translations.</param>
    public void UpdateDrawerLocalization(DrawerConfiguration drawerConfiguration, TranslationService translationService)
    {
        string enabledDisplayNameDefault = drawerConfiguration.Enabled.DisplayName;
        string enabledDescriptionDefault = drawerConfiguration.Enabled.Description;
        drawerConfiguration.Enabled.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEnabled-name", enabledDisplayNameDefault);
        drawerConfiguration.Enabled.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEnabled-description", enabledDescriptionDefault);

        string enabledKeybindingDisplayNameDefault = drawerConfiguration.EnabledKeybinding.DisplayName;
        string enabledKeybindingDescriptionDefault = drawerConfiguration.EnabledKeybinding.Description;
        drawerConfiguration.EnabledKeybinding.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerEnabledKeybinding-name", enabledKeybindingDisplayNameDefault);
        drawerConfiguration.EnabledKeybinding.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerEnabledKeybinding-description", enabledKeybindingDescriptionDefault);

        string locationXDisplayNameDefault = drawerConfiguration.Location.X.DisplayName;
        string locationXDescriptionDefault = drawerConfiguration.Location.X.Description;
        drawerConfiguration.Location.X.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerLocationX-name", locationXDisplayNameDefault);
        drawerConfiguration.Location.X.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerLocationX-description", locationXDescriptionDefault);

        string locationYDisplayNameDefault = drawerConfiguration.Location.Y.DisplayName;
        string locationYDescriptionDefault = drawerConfiguration.Location.Y.Description;
        drawerConfiguration.Location.Y.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerLocationY-name", locationYDisplayNameDefault);
        drawerConfiguration.Location.Y.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerLocationY-description", locationYDescriptionDefault);

        string widthDisplayNameDefault = drawerConfiguration.Size.X.DisplayName;
        string widthDescriptionDefault = drawerConfiguration.Size.X.Description;
        drawerConfiguration.Size.X.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerWidth-name", widthDisplayNameDefault);
        drawerConfiguration.Size.X.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerWidth-description", widthDescriptionDefault);

        string heightDisplayNameDefault = drawerConfiguration.Size.Y.DisplayName;
        string heightDescriptionDefault = drawerConfiguration.Size.Y.Description;
        drawerConfiguration.Size.Y.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerHeight-name", heightDisplayNameDefault);
        drawerConfiguration.Size.Y.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerHeight-description", heightDescriptionDefault);

        string buildDirectionDisplayNameDefault = drawerConfiguration.BuildDirection.DisplayName;
        string buildDirectionDescriptionDefault = drawerConfiguration.BuildDirection.Description;
        drawerConfiguration.BuildDirection.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerBuildDirection-name", buildDirectionDisplayNameDefault);
        drawerConfiguration.BuildDirection.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerBuildDirection-description", buildDirectionDescriptionDefault);

        string opacityDisplayNameDefault = drawerConfiguration.Opacity.DisplayName;
        string opacityDescriptionDefault = drawerConfiguration.Opacity.Description;
        drawerConfiguration.Opacity.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerOpacity-name", opacityDisplayNameDefault);
        drawerConfiguration.Opacity.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerOpacity-description", opacityDescriptionDefault);

        string backgroundColorDisplayNameDefault = drawerConfiguration.BackgroundColor.DisplayName;
        string backgroundColorDescriptionDefault = drawerConfiguration.BackgroundColor.Description;
        drawerConfiguration.BackgroundColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerBackgroundColor-name", backgroundColorDisplayNameDefault);
        drawerConfiguration.BackgroundColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerBackgroundColor-description", backgroundColorDescriptionDefault);

        string fontSizeDisplayNameDefault = drawerConfiguration.FontSize.DisplayName;
        string fontSizeDescriptionDefault = drawerConfiguration.FontSize.Description;
        drawerConfiguration.FontSize.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerFontSize-name", fontSizeDisplayNameDefault);
        drawerConfiguration.FontSize.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerFontSize-description", fontSizeDescriptionDefault);

        string textColorDisplayNameDefault = drawerConfiguration.TextColor.DisplayName;
        string textColorDescriptionDefault = drawerConfiguration.TextColor.Description;
        drawerConfiguration.TextColor.GetDisplayNameFunc = () => translationService.GetTranslation("setting-drawerTextColor-name", textColorDisplayNameDefault);
        drawerConfiguration.TextColor.GetDescriptionFunc = () => translationService.GetTranslation("setting-drawerTextColor-description", textColorDescriptionDefault);
    }

    /// <summary>
    ///     Unloads the base module settings.
    /// </summary>
    public virtual void Unload()
    {
        this.GlobalSettings.RemoveLoggingEvents();
        this.DrawerSettings.RemoveLoggingEvents();
    }

    #region Global Settings

    private const string GLOBAL_SETTINGS = "global-settings";
    public SettingCollection GlobalSettings { get; private set; }
    public SettingEntry<bool> GlobalDrawerVisible { get; private set; }
    public SettingEntry<KeyBinding> GlobalDrawerVisibleHotkey { get; private set; }
    public SettingEntry<bool> RegisterCornerIcon { get; private set; }
    public SettingEntry<CornerIconClickAction> CornerIconLeftClickAction { get; private set; }
    public SettingEntry<CornerIconClickAction> CornerIconRightClickAction { get; private set; }
    public SettingEntry<bool> HideOnMissingMumbleTicks { get; private set; }
    public SettingEntry<bool> HideInCombat { get; private set; }
    public SettingEntry<bool> HideOnOpenMap { get; private set; }
    public SettingEntry<bool> HideInPvE_OpenWorld { get; private set; }
    public SettingEntry<bool> HideInPvE_Competetive { get; private set; }
    public SettingEntry<bool> HideInWvW { get; private set; }
    public SettingEntry<bool> HideInPvP { get; private set; }
    public SettingEntry<bool> DebugEnabled { get; private set; }
    public SettingEntry<bool> UseDebugAPI { get; private set; }
    public SettingEntry<string> BlishAPIUsername { get; private set; }

    public SettingEntry<bool> RegisterContext { get; private set; }

    public SettingEntry<bool> SendMetrics { get; private set; }
    public SettingEntry<bool> AskedMetricsConsent { get; private set; }
    public SettingEntry<SemVer.Version> MetricsConsentGivenVersion { get; private set; }



    //public SettingEntry<bool> NotifyOnUnreadNews { get; private set; }
    //public SettingEntry<List<string>> NotifiedNews { get; private set; }

    #endregion

    #region Drawers

    private const string DRAWER_SETTINGS = "drawer-settings";
    public SettingCollection DrawerSettings { get; private set; }

    #endregion
}