namespace Estreya.BlishHUD.Shared.Settings
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Models;
    using Estreya.BlishHUD.Shared.Models.Drawers;
    using Estreya.BlishHUD.Shared.State;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using static Blish_HUD.ContentService;

    public abstract class BaseModuleSettings
    {
        protected Logger Logger;
        private KeyBinding _globalEnabledKeybinding;

        private Gw2Sharp.WebApi.V2.Models.Color _defaultColor;

        public Gw2Sharp.WebApi.V2.Models.Color DefaultGW2Color
        {
            get => this._defaultColor;
            private set => this._defaultColor = value;
        }

        public event EventHandler<ModuleSettingsChangedEventArgs> ModuleSettingsChanged;

        protected readonly SettingCollection _settings;

        #region Global Settings
        private const string GLOBAL_SETTINGS = "global-settings";
        public SettingCollection GlobalSettings { get; private set; }
        public SettingEntry<bool> GlobalDrawerVisible { get; private set; }
        public SettingEntry<KeyBinding> GlobalDrawerVisibleHotkey { get; private set; }
        public SettingEntry<bool> RegisterCornerIcon { get; private set; }
        public SettingEntry<CornerIconLeftClickAction> CornerIconLeftClickAction { get; private set; }
        public SettingEntry<CornerIconRightClickAction> CornerIconRightClickAction { get; private set; }
        public SettingEntry<bool> HideOnMissingMumbleTicks { get; private set; }
        public SettingEntry<bool> HideInCombat { get; private set; }
        public SettingEntry<bool> HideOnOpenMap { get; private set; }
        public SettingEntry<bool> HideInPvE_OpenWorld { get; private set; }
        public SettingEntry<bool> HideInPvE_Competetive { get; private set; }
        public SettingEntry<bool> HideInWvW { get; private set; }
        public SettingEntry<bool> HideInPvP { get; private set; }
        public SettingEntry<bool> DebugEnabled { get; private set; }
        #endregion

        #region Drawers
        private const string DRAWER_SETTINGS = "drawer-settings";
        public SettingCollection DrawerSettings { get; private set; }
        #endregion

        public BaseModuleSettings(SettingCollection settings, KeyBinding globalEnabledKeybinding)
        {
            this.Logger = Logger.GetLogger(this.GetType());

            this._settings = settings;
            this._globalEnabledKeybinding = globalEnabledKeybinding;
            this.BuildDefaultColor();

            this.InitializeGlobalSettings(this._settings);

            this.InitializeDrawerSettings(this._settings);

            this.InitializeAdditionalSettings(this._settings);
        }

        private void InitializeDrawerSettings(SettingCollection settings)
        {
            this.DrawerSettings = settings.AddSubCollection(DRAWER_SETTINGS);
        }

        private void BuildDefaultColor()
        {
            this._defaultColor = new Gw2Sharp.WebApi.V2.Models.Color()
            {
                Name = "Dye Remover",
                Id = 1,
                BaseRgb = new List<int>() { 128, 26, 26 },
                Cloth = new Gw2Sharp.WebApi.V2.Models.ColorMaterial()
                {
                    Brightness = 15,
                    Contrast = 1.25,
                    Hue = 38,
                    Saturation = 0.28125,
                    Lightness = 1.44531,
                    Rgb = new List<int>() { 124, 108, 83 }
                },
                Leather = new Gw2Sharp.WebApi.V2.Models.ColorMaterial()
                {
                    Brightness = -8,
                    Contrast = 1.0,
                    Hue = 34,
                    Saturation = 0.3125,
                    Lightness = 1.09375,
                    Rgb = new List<int>() { 65, 49, 29 }
                },
                Metal = new Gw2Sharp.WebApi.V2.Models.ColorMaterial()
                {
                    Brightness = 5,
                    Contrast = 1.05469,
                    Hue = 38,
                    Saturation = 0.101563,
                    Lightness = 1.36719,
                    Rgb = new List<int>() { 96, 91, 83 }
                },
                Fur = new Gw2Sharp.WebApi.V2.Models.ColorMaterial()
                {
                    Brightness = 15,
                    Contrast = 1.25,
                    Hue = 38,
                    Saturation = 0.28125,
                    Lightness = 1.44531,
                    Rgb = new List<int>() { 124, 108, 83 }
                },
            };
        }

        protected virtual void InitializeAdditionalSettings(SettingCollection settings) { /* NOOP */ }

        private void InitializeGlobalSettings(SettingCollection settings)
        {
            this.GlobalSettings = settings.AddSubCollection(GLOBAL_SETTINGS);

            this.GlobalDrawerVisible = this.GlobalSettings.DefineSetting(nameof(this.GlobalDrawerVisible), true, () => "Global Visible", () => "Whether the modules drawers should be visible.");
            this.GlobalDrawerVisible.SettingChanged += this.SettingChanged;

            bool globalHotkeyEnabled = this._globalEnabledKeybinding != null;
            if (this._globalEnabledKeybinding == null)
            {
                this._globalEnabledKeybinding = new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Ctrl | Microsoft.Xna.Framework.Input.ModifierKeys.Alt | Microsoft.Xna.Framework.Input.ModifierKeys.Shift, Microsoft.Xna.Framework.Input.Keys.Enter);
                Logger.Debug("No default keybinding defined. Building temp keybinding. Enabled = {0}", globalHotkeyEnabled);
            }

            this.GlobalDrawerVisibleHotkey = this.GlobalSettings.DefineSetting(nameof(this.GlobalDrawerVisibleHotkey), this._globalEnabledKeybinding, () => "Global Visible Hotkey", () => "Defines the hotkey used to toggle the global visibility.");
            this.GlobalDrawerVisibleHotkey.SettingChanged += this.SettingChanged;
            this.GlobalDrawerVisibleHotkey.Value.Enabled = globalHotkeyEnabled;
            this.GlobalDrawerVisibleHotkey.Value.Activated += this.GlobalEnabledHotkey_Activated;
            this.GlobalDrawerVisibleHotkey.Value.IgnoreWhenInTextField = true;
            this.GlobalDrawerVisibleHotkey.Value.BlockSequenceFromGw2 = globalHotkeyEnabled;

            this.RegisterCornerIcon = this.GlobalSettings.DefineSetting(nameof(this.RegisterCornerIcon), true, () => "Register Corner Icon", () => "Whether the module should register a corner icon.");
            this.RegisterCornerIcon.SettingChanged += this.SettingChanged;
            this.RegisterCornerIcon.SettingChanged += this.RegisterCornerIcon_SettingChanged;

            this.CornerIconLeftClickAction = this.GlobalSettings.DefineSetting(nameof(this.CornerIconLeftClickAction), Models.CornerIconLeftClickAction.Settings, () => "Corner Icon Left Click Action", () => "Defines the action of the corner icon when left clicked.");
            this.CornerIconLeftClickAction.SettingChanged += this.SettingChanged;

            this.CornerIconRightClickAction = this.GlobalSettings.DefineSetting(nameof(this.CornerIconRightClickAction), Models.CornerIconRightClickAction.None, () => "Corner Icon Right Click Action", () => "Defines the action of the corner icon when right clicked.");
            this.CornerIconRightClickAction.SettingChanged += this.SettingChanged;

            this.HideOnOpenMap = this.GlobalSettings.DefineSetting(nameof(this.HideOnOpenMap), true, () => "Hide on open Map", () => "Whether the modules drawers should hide when the map is open.");
            this.HideOnOpenMap.SettingChanged += this.SettingChanged;

            this.HideOnMissingMumbleTicks = this.GlobalSettings.DefineSetting(nameof(this.HideOnMissingMumbleTicks), true, () => "Hide on Cutscenes", () => "Whether the modules drawers should hide when cutscenes are played.");
            this.HideOnMissingMumbleTicks.SettingChanged += this.SettingChanged;

            this.HideInCombat = this.GlobalSettings.DefineSetting(nameof(this.HideInCombat), false, () => "Hide in Combat", () => "Whether the modules drawers should hide when in combat.");
            this.HideInCombat.SettingChanged += this.SettingChanged;

            this.HideInPvE_OpenWorld = this.GlobalSettings.DefineSetting(nameof(this.HideInPvE_OpenWorld), false, () => "Hide in PvE (Open World)", () => "Whether the drawers should hide when in PvE (Open World).");
            this.HideInPvE_OpenWorld.SettingChanged += this.SettingChanged;

            this.HideInPvE_Competetive = this.GlobalSettings.DefineSetting(nameof(this.HideInPvE_Competetive), false, () => "Hide in PvE (Competetive)", () => "Whether the drawers should hide when in PvE (Competetive).");
            this.HideInPvE_Competetive.SettingChanged += this.SettingChanged;

            this.HideInWvW = this.GlobalSettings.DefineSetting(nameof(this.HideInWvW), false, () => "Hide in WvW", () => "Whether the drawers should hide when in world vs. world.");
            this.HideInWvW.SettingChanged += this.SettingChanged;

            this.HideInPvP = this.GlobalSettings.DefineSetting(nameof(this.HideInPvP), false, () => "Hide in PvP", () => "Whether the drawers should hide when in player vs. player.");
            this.HideInPvP.SettingChanged += this.SettingChanged;

            this.DebugEnabled = this.GlobalSettings.DefineSetting(nameof(this.DebugEnabled), false, () => "Debug Enabled", () => "Whether the module runs in debug mode.");
            this.DebugEnabled.SettingChanged += this.SettingChanged;

            this.HandleEnabledStates();

            this.DoInitializeGlobalSettings(this.GlobalSettings);
        }

        private void RegisterCornerIcon_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            this.HandleEnabledStates();
        }

        private void GlobalEnabledHotkey_Activated(object sender, EventArgs e)
        {
            this.GlobalDrawerVisible.Value = !this.GlobalDrawerVisible.Value;
        }
        private void HandleEnabledStates()
        {
            this.CornerIconLeftClickAction.SetDisabled(!this.RegisterCornerIcon.Value);
            this.CornerIconRightClickAction.SetDisabled(!this.RegisterCornerIcon.Value);
        }

        protected virtual void DoInitializeGlobalSettings(SettingCollection globalSettingCollection) { /* NOOP */ }

        protected virtual void DoInitializeLocationSettings(SettingCollection locationSettingCollection) { /* NOOP */ }

        public DrawerConfiguration AddDrawer(string name, BuildDirection defaultBuildDirection = BuildDirection.Top)
        {
            int maxHeight = 1080;
            int maxWidth = 1920;

            var enabled = this.DrawerSettings.DefineSetting($"{name}-enabled", true, () => "Enabled", () => "Whether the drawer is enabled.");
            var enabledKeybinding = this.DrawerSettings.DefineSetting($"{name}-enabledKeybinding", new KeyBinding(), () => "Enabled Keybinding", () => "Defines the keybinding to toggle this drawer on and off.");
            enabledKeybinding.Value.Enabled = true;
            enabledKeybinding.Value.IgnoreWhenInTextField = true;
            enabledKeybinding.Value.BlockSequenceFromGw2 = true;

            var locationX = this.DrawerSettings.DefineSetting($"{name}-locationX", (int)(maxWidth * 0.1), () => "Location X", () => "Defines the position on the x axis.");
            locationX.SetRange(0, maxWidth);
            var locationY = this.DrawerSettings.DefineSetting($"{name}-locationY", (int)(maxHeight * 0.1), () => "Location Y", () => "Defines the position on the y axis.");
            locationY.SetRange(0, maxHeight);
            var width = this.DrawerSettings.DefineSetting($"{name}-width", (int)(maxWidth * 0.5), () => "Width", () => "The width of the drawer.");
            width.SetRange(0, maxWidth);
            var height = this.DrawerSettings.DefineSetting($"{name}-height", (int)(maxHeight * 0.25), () => "Height", () => "The height of the drawer.");
            height.SetRange(0, maxHeight);

            var buildDirection = this.DrawerSettings.DefineSetting($"{name}-buildDirection", defaultBuildDirection, () => "Build Direction", () => "The build direction of the drawer.");
            var opacity = this.DrawerSettings.DefineSetting($"{name}-opacity", 1f, () => "Opacity", () => "The opacity of the drawer.");
            opacity.SetRange(0f, 1f);
            var backgroundColor = this.DrawerSettings.DefineSetting($"{name}-backgroundColor", this.DefaultGW2Color, () => "Background Color", () => "The background color of the drawer.");
            var fontSize = this.DrawerSettings.DefineSetting($"{name}-fontSize", ContentService.FontSize.Size16, () => "Font Size", () => "The font size of the drawer.");
            var textColor = this.DrawerSettings.DefineSetting($"{name}-textColor", this.DefaultGW2Color, () => "Text Color", () => "The text color of the drawer.");

            DrawerConfiguration configuration = new DrawerConfiguration()
            {
                Name = name,
                Enabled = enabled,
                EnabledKeybinding = enabledKeybinding,
                Location = new DrawerLocation()
                {
                    X = locationX,
                    Y = locationY
                },
                Size = new DrawerSize()
                {
                    X = width,
                    Y = height
                },
                BuildDirection = buildDirection,
                Opacity = opacity,
                BackgroundColor = backgroundColor,
                FontSize = fontSize,
                TextColor = textColor
            };

            return configuration;
        }

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
            this.DrawerSettings.UndefineSetting($"{name}-textColor");
        }

        public void CheckDrawerSizeAndPosition(DrawerConfiguration configuration)
        {
            bool buildFromBottom = configuration.BuildDirection.Value == BuildDirection.Bottom;
            int maxResX = (int)(GameService.Graphics.Resolution.X / GameService.Graphics.UIScaleMultiplier);
            int maxResY = (int)(GameService.Graphics.Resolution.Y / GameService.Graphics.UIScaleMultiplier);

            int minLocationX = 0;
            int maxLocationX = maxResX - configuration.Size.X.Value;
            int minLocationY = buildFromBottom ? configuration.Size.Y.Value : 0;
            int maxLocationY = buildFromBottom ? maxResY : maxResY - configuration.Size.Y.Value;
            int minWidth = 0;
            int maxWidth = maxResX - configuration.Location.X.Value;
            int minHeight = 0;
            int maxHeight = maxResY - configuration.Location.Y.Value;

            configuration.Location.X.SetRange(minLocationX, maxLocationX);
            configuration.Location.Y.SetRange(minLocationY, maxLocationY);
            configuration.Size.X.SetRange(minWidth, maxWidth);
            configuration.Size.Y.SetRange(minHeight, maxHeight);
        }

        public virtual void UpdateLocalization(TranslationState translationState)
        {
            var globalDrawerVisibleDisplayNameDefault = this.GlobalDrawerVisible.DisplayName;
            var globalDrawerVisibleDescriptionDefault = this.GlobalDrawerVisible.Description;
            this.GlobalDrawerVisible.GetDisplayNameFunc = () => translationState.GetTranslation("setting-globalDrawerVisible-name", globalDrawerVisibleDisplayNameDefault);
            this.GlobalDrawerVisible.GetDescriptionFunc = () => translationState.GetTranslation("setting-globalDrawerVisible-description", globalDrawerVisibleDescriptionDefault);

            var globalDrawerVisibleHotkeyDisplayNameDefault = this.GlobalDrawerVisibleHotkey.DisplayName;
            var globalDrawerVisibleHotkeyDescriptionDefault = this.GlobalDrawerVisibleHotkey.Description;
            this.GlobalDrawerVisibleHotkey.GetDisplayNameFunc = () => translationState.GetTranslation("setting-globalDrawerVisibleHotkey-name", globalDrawerVisibleHotkeyDisplayNameDefault);
            this.GlobalDrawerVisibleHotkey.GetDescriptionFunc = () => translationState.GetTranslation("setting-globalDrawerVisibleHotkey-description", globalDrawerVisibleHotkeyDescriptionDefault);

            var registerCornerIconDisplayNameDefault = this.RegisterCornerIcon.DisplayName;
            var registerCornerIconDescriptionDefault = this.RegisterCornerIcon.Description;
            this.RegisterCornerIcon.GetDisplayNameFunc = () => translationState.GetTranslation("setting-registerCornerIcon-name", registerCornerIconDisplayNameDefault);
            this.RegisterCornerIcon.GetDescriptionFunc = () => translationState.GetTranslation("setting-registerCornerIcon-description", registerCornerIconDescriptionDefault);

            var hideOnOpenMapDisplayNameDefault = this.HideOnOpenMap.DisplayName;
            var hideOnOpenMapDescriptionDefault = this.HideOnOpenMap.Description;
            this.HideOnOpenMap.GetDisplayNameFunc = () => translationState.GetTranslation("setting-hideOnOpenMap-name", hideOnOpenMapDisplayNameDefault);
            this.HideOnOpenMap.GetDescriptionFunc = () => translationState.GetTranslation("setting-hideOnOpenMap-description", hideOnOpenMapDescriptionDefault);

            var hideOnMissingMumbleTickDisplayNameDefault = this.HideOnMissingMumbleTicks.DisplayName;
            var hideOnMissingMumbleTickDescriptionDefault = this.HideOnMissingMumbleTicks.Description;
            this.HideOnMissingMumbleTicks.GetDisplayNameFunc = () => translationState.GetTranslation("setting-hideOnMissingMumbleTick-name", hideOnMissingMumbleTickDisplayNameDefault);
            this.HideOnMissingMumbleTicks.GetDescriptionFunc = () => translationState.GetTranslation("setting-hideOnMissingMumbleTick-description", hideOnMissingMumbleTickDescriptionDefault);

            var hideInCombatDisplayNameDefault = this.HideInCombat.DisplayName;
            var hideInCombatDescriptionDefault = this.HideInCombat.Description;
            this.HideInCombat.GetDisplayNameFunc = () => translationState.GetTranslation("setting-hideInCombat-name", hideInCombatDisplayNameDefault);
            this.HideInCombat.GetDescriptionFunc = () => translationState.GetTranslation("setting-hideInCombat-description", hideInCombatDescriptionDefault);

            var hideInPVEOpenWorldDisplayNameDefault = this.HideInPvE_OpenWorld.DisplayName;
            var hideInPVEOpenWorldDescriptionDefault = this.HideInPvE_OpenWorld.Description;
            this.HideInPvE_OpenWorld.GetDisplayNameFunc = () => translationState.GetTranslation("setting-hideInPVEOpenWorld-name", hideInPVEOpenWorldDisplayNameDefault);
            this.HideInPvE_OpenWorld.GetDescriptionFunc = () => translationState.GetTranslation("setting-hideInPVEOpenWorld-description", hideInPVEOpenWorldDescriptionDefault);

            var hideInPVECompetetiveDisplayNameDefault = this.HideInPvE_Competetive.DisplayName;
            var hideInPVECompetetiveDescriptionDefault = this.HideInPvE_Competetive.Description;
            this.HideInPvE_Competetive.GetDisplayNameFunc = () => translationState.GetTranslation("setting-hideInPVECompetetive-name", hideInPVECompetetiveDisplayNameDefault);
            this.HideInPvE_Competetive.GetDescriptionFunc = () => translationState.GetTranslation("setting-hideInPVECompetetive-description", hideInPVECompetetiveDescriptionDefault);

            var hideInWVWDisplayNameDefault = this.HideInWvW.DisplayName;
            var hideInWVWDescriptionDefault = this.HideInWvW.Description;
            this.HideInWvW.GetDisplayNameFunc = () => translationState.GetTranslation("setting-hideInWVW-name", hideInWVWDisplayNameDefault);
            this.HideInWvW.GetDescriptionFunc = () => translationState.GetTranslation("setting-hideInWVW-description", hideInWVWDescriptionDefault);

            var hideInPVPDisplayNameDefault = this.HideInPvP.DisplayName;
            var hideInPVPDescriptionDefault = this.HideInPvP.Description;
            this.HideInPvP.GetDisplayNameFunc = () => translationState.GetTranslation("setting-hideInPVP-name", hideInPVPDisplayNameDefault);
            this.HideInPvP.GetDescriptionFunc = () => translationState.GetTranslation("setting-hideInPVP-description", hideInPVPDescriptionDefault);
        }

        public void UpdateDrawerLocalization(DrawerConfiguration drawerConfiguration, TranslationState translationState)
        {
            var enabledDisplayNameDefault = drawerConfiguration.Enabled.DisplayName;
            var enabledDescriptionDefault = drawerConfiguration.Enabled.Description;
            drawerConfiguration.Enabled.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerEnabled-name", enabledDisplayNameDefault);
            drawerConfiguration.Enabled.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerEnabled-description", enabledDescriptionDefault);

            var enabledKeybindingDisplayNameDefault = drawerConfiguration.EnabledKeybinding.DisplayName;
            var enabledKeybindingDescriptionDefault = drawerConfiguration.EnabledKeybinding.Description;
            drawerConfiguration.EnabledKeybinding.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerEnabledKeybinding-name", enabledKeybindingDisplayNameDefault);
            drawerConfiguration.EnabledKeybinding.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerEnabledKeybinding-description", enabledKeybindingDescriptionDefault);

            var locationXDisplayNameDefault = drawerConfiguration.Location.X.DisplayName;
            var locationXDescriptionDefault = drawerConfiguration.Location.X.Description;
            drawerConfiguration.Location.X.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerLocationX-name", locationXDisplayNameDefault);
            drawerConfiguration.Location.X.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerLocationX-description", locationXDescriptionDefault);

            var locationYDisplayNameDefault = drawerConfiguration.Location.Y.DisplayName;
            var locationYDescriptionDefault = drawerConfiguration.Location.Y.Description;
            drawerConfiguration.Location.Y.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerLocationY-name", locationYDisplayNameDefault);
            drawerConfiguration.Location.Y.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerLocationY-description", locationYDescriptionDefault);

            var widthDisplayNameDefault = drawerConfiguration.Size.X.DisplayName;
            var widthDescriptionDefault = drawerConfiguration.Size.X.Description;
            drawerConfiguration.Size.X.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerWidth-name", widthDisplayNameDefault);
            drawerConfiguration.Size.X.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerWidth-description", widthDescriptionDefault);

            var heightDisplayNameDefault = drawerConfiguration.Size.Y.DisplayName;
            var heightDescriptionDefault = drawerConfiguration.Size.Y.Description;
            drawerConfiguration.Size.Y.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerHeight-name", heightDisplayNameDefault);
            drawerConfiguration.Size.Y.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerHeight-description", heightDescriptionDefault);

            var buildDirectionDisplayNameDefault = drawerConfiguration.BuildDirection.DisplayName;
            var buildDirectionDescriptionDefault = drawerConfiguration.BuildDirection.Description;
            drawerConfiguration.BuildDirection.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerBuildDirection-name", buildDirectionDisplayNameDefault);
            drawerConfiguration.BuildDirection.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerBuildDirection-description", buildDirectionDescriptionDefault);

            var opacityDisplayNameDefault = drawerConfiguration.Opacity.DisplayName;
            var opacityDescriptionDefault = drawerConfiguration.Opacity.Description;
            drawerConfiguration.Opacity.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerOpacity-name", opacityDisplayNameDefault);
            drawerConfiguration.Opacity.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerOpacity-description", opacityDescriptionDefault);

            var backgroundColorDisplayNameDefault = drawerConfiguration.BackgroundColor.DisplayName;
            var backgroundColorDescriptionDefault = drawerConfiguration.BackgroundColor.Description;
            drawerConfiguration.BackgroundColor.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerBackgroundColor-name", backgroundColorDisplayNameDefault);
            drawerConfiguration.BackgroundColor.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerBackgroundColor-description", backgroundColorDescriptionDefault);

            var fontSizeDisplayNameDefault = drawerConfiguration.FontSize.DisplayName;
            var fontSizeDescriptionDefault = drawerConfiguration.FontSize.Description;
            drawerConfiguration.FontSize.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerFontSize-name", fontSizeDisplayNameDefault);
            drawerConfiguration.FontSize.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerFontSize-description", fontSizeDescriptionDefault);

            var textColorDisplayNameDefault = drawerConfiguration.TextColor.DisplayName;
            var textColorDescriptionDefault = drawerConfiguration.TextColor.Description;
            drawerConfiguration.TextColor.GetDisplayNameFunc = () => translationState.GetTranslation("setting-drawerTextColor-name", textColorDisplayNameDefault);
            drawerConfiguration.TextColor.GetDescriptionFunc = () => translationState.GetTranslation("setting-drawerTextColor-description", textColorDescriptionDefault);
        }

        public virtual void Unload()
        {
            // Global Settings
            this.GlobalDrawerVisible.SettingChanged -= this.SettingChanged;
            this.GlobalDrawerVisibleHotkey.SettingChanged -= this.SettingChanged;
            this.GlobalDrawerVisibleHotkey.Value.Enabled = false;
            this.GlobalDrawerVisibleHotkey.Value.Activated -= this.GlobalEnabledHotkey_Activated;
            this.RegisterCornerIcon.SettingChanged -= this.SettingChanged;
            this.RegisterCornerIcon.SettingChanged -= this.RegisterCornerIcon_SettingChanged;
            this.HideOnOpenMap.SettingChanged -= this.SettingChanged;
            this.HideOnMissingMumbleTicks.SettingChanged -= this.SettingChanged;
            this.HideInPvE_OpenWorld.SettingChanged -= this.SettingChanged;
            this.HideInPvE_Competetive.SettingChanged -= this.SettingChanged;
            this.HideInCombat.SettingChanged -= this.SettingChanged;
            this.HideInPvP.SettingChanged -= this.SettingChanged;
            this.DebugEnabled.SettingChanged -= this.SettingChanged;
        }

        protected void SettingChanged<T>(object sender, ValueChangedEventArgs<T> e)
        {
            SettingEntry<T> settingEntry = (SettingEntry<T>)sender;
            string prevValue = e.PreviousValue.GetType() == typeof(string) ? e.PreviousValue.ToString() : JsonConvert.SerializeObject(e.PreviousValue);
            string newValue = e.NewValue.GetType() == typeof(string) ? e.NewValue.ToString() : JsonConvert.SerializeObject(e.NewValue);
            Logger.Debug($"Changed setting \"{settingEntry.EntryKey}\" from \"{prevValue}\" to \"{newValue}\"");

            ModuleSettingsChanged?.Invoke(this, new ModuleSettingsChangedEventArgs() { Name = settingEntry.EntryKey, NewValue = e.NewValue, PreviousValue = e.PreviousValue });
        }

        public class ModuleSettingsChangedEventArgs
        {
            public string Name { get; set; }
            public object NewValue { get; set; }
            public object PreviousValue { get; set; }
        }
    }
}
