namespace Estreya.BlishHUD.Shared.Settings
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Models;
    using Estreya.BlishHUD.Shared.Models.Drawers;
    using Estreya.BlishHUD.Shared.Resources;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using static Blish_HUD.ContentService;

    public abstract class BaseModuleSettings
    {
        private static readonly Logger Logger = Logger.GetLogger<BaseModuleSettings>();
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
        public SettingEntry<bool> HideOnMissingMumbleTicks { get; private set; }
        public SettingEntry<bool> HideInCombat { get; private set; }
        public SettingEntry<bool> HideOnOpenMap { get; private set; }
        public SettingEntry<bool> HideInWvW { get; private set; }
        public SettingEntry<bool> HideInPvP { get; private set; }
        public SettingEntry<bool> DebugEnabled { get; private set; }
        public SettingEntry<FontSize> FontSize { get; private set; }
        #endregion

        #region Drawers
        private const string DRAWER_SETTINGS = "drawer-settings";
        public SettingCollection DrawerSettings { get; private set; }
        #endregion

        public BaseModuleSettings(SettingCollection settings, KeyBinding globalEnabledKeybinding)
        {
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

            this.GlobalDrawerVisible = this.GlobalSettings.DefineSetting(nameof(this.GlobalDrawerVisible), true, () => Strings.Setting_GlobalEnabled_Name, () => Strings.Setting_GlobalEnabled_Description);
            this.GlobalDrawerVisible.SettingChanged += this.SettingChanged;

            bool globalHotkeyEnabled = this._globalEnabledKeybinding != null;
            if (this._globalEnabledKeybinding == null)
            {
                this._globalEnabledKeybinding = new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Ctrl | Microsoft.Xna.Framework.Input.ModifierKeys.Alt | Microsoft.Xna.Framework.Input.ModifierKeys.Shift, Microsoft.Xna.Framework.Input.Keys.Enter);
                Logger.Debug("No default keybinding defined. Building temp keybinding. Enabled = {0}", globalHotkeyEnabled);
            }

            this.GlobalDrawerVisibleHotkey = this.GlobalSettings.DefineSetting(nameof(this.GlobalDrawerVisibleHotkey), this._globalEnabledKeybinding, () => Strings.Setting_GlobalEnabledHotkey_Name, () => Strings.Setting_GlobalEnabledHotkey_Description);
            this.GlobalDrawerVisibleHotkey.SettingChanged += this.SettingChanged;
            this.GlobalDrawerVisibleHotkey.Value.Enabled = globalHotkeyEnabled;
            this.GlobalDrawerVisibleHotkey.Value.Activated += this.GlobalEnabledHotkey_Activated;
            this.GlobalDrawerVisibleHotkey.Value.BlockSequenceFromGw2 = globalHotkeyEnabled;

            this.RegisterCornerIcon = this.GlobalSettings.DefineSetting(nameof(this.RegisterCornerIcon), true, () => Strings.Setting_RegisterCornerIcon_Name, () => Strings.Setting_RegisterCornerIcon_Description);
            this.RegisterCornerIcon.SettingChanged += this.SettingChanged;

            this.HideOnOpenMap = this.GlobalSettings.DefineSetting(nameof(this.HideOnOpenMap), true, () => Strings.Setting_HideOnMap_Name, () => Strings.Setting_HideOnMap_Description);
            this.HideOnOpenMap.SettingChanged += this.SettingChanged;

            this.HideOnMissingMumbleTicks = this.GlobalSettings.DefineSetting(nameof(this.HideOnMissingMumbleTicks), true, () => Strings.Setting_HideOnMissingMumbleTicks_Name, () => Strings.Setting_HideOnMissingMumbleTicks_Description);
            this.HideOnMissingMumbleTicks.SettingChanged += this.SettingChanged;

            this.HideInCombat = this.GlobalSettings.DefineSetting(nameof(this.HideInCombat), false, () => Strings.Setting_HideInCombat_Name, () => Strings.Setting_HideInCombat_Description);
            this.HideInCombat.SettingChanged += this.SettingChanged;

            this.HideInWvW = this.GlobalSettings.DefineSetting(nameof(this.HideInWvW), false, () => "Hide in WvW", () => "Whether the event table should hide when in world vs. world.");
            this.HideInWvW.SettingChanged += this.SettingChanged;

            this.HideInPvP = this.GlobalSettings.DefineSetting(nameof(this.HideInPvP), false, () => "Hide in PvP", () => "Whether the event table should hide when in player vs. player.");
            this.HideInPvP.SettingChanged += this.SettingChanged;
            /*
            this.BackgroundColor = this.GlobalSettings.DefineSetting(nameof(this.BackgroundColor), this.DefaultGW2Color, () => Strings.Setting_BackgroundColor_Name, () => Strings.Setting_BackgroundColor_Description);
            this.BackgroundColor.SettingChanged += this.SettingChanged;

            this.BackgroundColorOpacity = this.GlobalSettings.DefineSetting(nameof(this.BackgroundColorOpacity), 0.0f, () => Strings.Setting_BackgroundColorOpacity_Name, () => Strings.Setting_BackgroundColorOpacity_Description);
            this.BackgroundColorOpacity.SetRange(0.0f, 1f);
            this.BackgroundColorOpacity.SettingChanged += this.SettingChanged;
            */
            this.DebugEnabled = this.GlobalSettings.DefineSetting(nameof(this.DebugEnabled), false, () => Strings.Setting_DebugEnabled_Name, () => Strings.Setting_DebugEnabled_Description);
            this.DebugEnabled.SettingChanged += this.SettingChanged;

            /*
            this.BuildDirection = this.GlobalSettings.DefineSetting(nameof(this.BuildDirection), Shared.Models.BuildDirection.Top, () => Strings.Setting_BuildDirection_Name, () => Strings.Setting_BuildDirection_Description);
            this.BuildDirection.SettingChanged += this.SettingChanged;

            this.Opacity = this.GlobalSettings.DefineSetting(nameof(this.Opacity), 1f, () => Strings.Setting_Opacity_Name, () => Strings.Setting_Opacity_Description);
            this.Opacity.SetRange(0.1f, 1f);
            this.Opacity.SettingChanged += this.SettingChanged;
            */

            this.FontSize = this.GlobalSettings.DefineSetting(nameof(this.FontSize), ContentService.FontSize.Size16, () => Strings.Setting_FontSize_Name, () => Strings.Setting_FontSize_Description);
            this.FontSize.SettingChanged += this.SettingChanged;

            this.DoInitializeGlobalSettings(this.GlobalSettings);
        }

        private void GlobalEnabledHotkey_Activated(object sender, EventArgs e)
        {
            this.GlobalDrawerVisible.Value = !this.GlobalDrawerVisible.Value;
        }

        protected virtual void DoInitializeGlobalSettings(SettingCollection globalSettingCollection) { /* NOOP */ }

        protected virtual void DoInitializeLocationSettings(SettingCollection locationSettingCollection) { /* NOOP */ }

        public DrawerConfiguration AddDrawer(string name, BuildDirection defaultBuildDirection = BuildDirection.Top)
        {
            int maxHeight = 1080;
            int maxWidth = 1920;

            var locationX = this.DrawerSettings.DefineSetting($"{name}-locationX", (int)(maxWidth * 0.1), () => Strings.Setting_LocationX_Name, () => Strings.Setting_LocationX_Description);
            locationX.SetRange(0, maxWidth);
            var locationY = this.DrawerSettings.DefineSetting($"{name}-locationY", (int)(maxHeight * 0.1), () => Strings.Setting_LocationY_Name, () => Strings.Setting_LocationY_Description);
            locationY.SetRange(0, maxHeight);
            var width = this.DrawerSettings.DefineSetting($"{name}-width", (int)(maxWidth * 0.5), () => Strings.Setting_Width_Name, () => Strings.Setting_Width_Description);
            width.SetRange(0, maxWidth);
            var height = this.DrawerSettings.DefineSetting($"{name}-height", (int)(maxHeight * 0.25), () => "Height", () => "The height of the drawer.");
            height.SetRange(0, maxHeight);

            var buildDirection = this.DrawerSettings.DefineSetting($"{name}-buildDirection", defaultBuildDirection, () => "Build Direction", () => "The build direction of the drawer.");
            var opacity = this.DrawerSettings.DefineSetting($"{name}-opacity", 1f, () => "Opacity", () => "The opacity of the drawer.");
            opacity.SetRange(0f, 1f);
            var backgroundColor = this.DrawerSettings.DefineSetting($"{name}-backgroundColor", this.DefaultGW2Color, () => "Background Color", () => "The background color of the drawer.");
            var fontSize = this.DrawerSettings.DefineSetting($"{name}-fontSize", ContentService.FontSize.Size16, () => "Font Size", () => "The font size of the drawer.");

            DrawerConfiguration configuration = new DrawerConfiguration()
            {
                Name = name,
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
                FontSize = fontSize
            };

            return configuration;
        }

        public void CheckDrawerSizeAndPosition(DrawerConfiguration configuration, int currentWidth, int currentHeight)
        {
            bool buildFromBottom = configuration.BuildDirection.Value == BuildDirection.Bottom;
            int maxResX = (int)(GameService.Graphics.Resolution.X / GameService.Graphics.UIScaleMultiplier);
            int maxResY = (int)(GameService.Graphics.Resolution.Y / GameService.Graphics.UIScaleMultiplier);

            int minLocationX = 0;
            int maxLocationX = maxResX - currentWidth;
            int minLocationY = buildFromBottom ? currentHeight : 0;
            int maxLocationY = buildFromBottom ? maxResY : maxResY - currentHeight;
            int minWidth = 0;
            int maxWidth = maxResX - configuration.Location.X.Value;
            int minHeight = 0;
            int maxHeight = maxResY - configuration.Location.Y.Value;

            configuration.Location.X.SetRange(minLocationX, maxLocationX);
            configuration.Location.Y.SetRange(minLocationY, maxLocationY);
            configuration.Size.X.SetRange(minWidth, maxWidth);
            configuration.Size.Y.SetRange(minHeight, maxHeight);
        }

        public virtual void Unload()
        {
            // Global Settings
            this.GlobalDrawerVisible.SettingChanged -= this.SettingChanged;
            this.GlobalDrawerVisibleHotkey.SettingChanged -= this.SettingChanged;
            this.GlobalDrawerVisibleHotkey.Value.Enabled = false;
            this.GlobalDrawerVisibleHotkey.Value.Activated -= this.GlobalEnabledHotkey_Activated;
            this.RegisterCornerIcon.SettingChanged -= this.SettingChanged;
            this.HideOnOpenMap.SettingChanged -= this.SettingChanged;
            this.HideOnMissingMumbleTicks.SettingChanged -= this.SettingChanged;
            this.HideInCombat.SettingChanged -= this.SettingChanged;
            this.HideInPvP.SettingChanged -= this.SettingChanged;
            //this.BackgroundColor.SettingChanged -= this.SettingChanged;
            //this.BackgroundColorOpacity.SettingChanged -= this.SettingChanged;
            this.DebugEnabled.SettingChanged -= this.SettingChanged;
            //this.BuildDirection.SettingChanged -= this.SettingChanged;
            //this.Opacity.SettingChanged -= this.SettingChanged;
            this.FontSize.SettingChanged -= this.SettingChanged;

            for (int i = this.GlobalSettings.Entries.Count - 1; i >= 0; i--)
            {
                this.GlobalSettings.UndefineSetting(this.GlobalSettings.Entries[i].EntryKey);
            }
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
