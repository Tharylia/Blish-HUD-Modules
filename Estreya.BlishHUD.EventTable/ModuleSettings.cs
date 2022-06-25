namespace Estreya.BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.Resources;
    using Estreya.BlishHUD.EventTable.Utils;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class ModuleSettings
    {
        private static readonly Logger Logger = Logger.GetLogger<ModuleSettings>();
        private Gw2Sharp.WebApi.V2.Models.Color _defaultColor;

        private AsyncLock _eventSettingsLock = new AsyncLock();

        public Gw2Sharp.WebApi.V2.Models.Color DefaultGW2Color { get => this._defaultColor; private set => this._defaultColor = value; }

        public event EventHandler<ModuleSettingsChangedEventArgs> ModuleSettingsChanged;
        public event EventHandler<EventSettingsChangedEventArgs> EventSettingChanged;

        private SettingCollection Settings { get; set; }

        #region Global Settings
        private const string GLOBAL_SETTINGS = "event-table-global-settings";
        public SettingCollection GlobalSettings { get; private set; }
        public SettingEntry<bool> GlobalEnabled { get; private set; }
        public SettingEntry<KeyBinding> GlobalEnabledHotkey { get; private set; }
        public SettingEntry<bool> RegisterCornerIcon { get; private set; }
        public SettingEntry<int> RefreshRateDelay { get; private set; }
        public SettingEntry<bool> AutomaticallyUpdateEventFile { get; private set; }
        public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> BackgroundColor { get; private set; }
        public SettingEntry<float> BackgroundColorOpacity { get; private set; }
        public SettingEntry<bool> HideOnMissingMumbleTicks { get; private set; }
        public SettingEntry<bool> HideInCombat { get; private set; }
        public SettingEntry<bool> HideOnOpenMap { get; private set; }
        public SettingEntry<bool> HideInWvW { get; private set; }
        public SettingEntry<bool> HideInPvP { get; private set; }
        public SettingEntry<bool> DebugEnabled { get; private set; }
        public SettingEntry<bool> ShowTooltips { get; private set; }
        public SettingEntry<bool> HandleLeftClick { get; private set; }
        public SettingEntry<LeftClickAction> LeftClickAction { get; private set; }
        public SettingEntry<bool> ShowContextMenuOnClick { get; private set; }
        public SettingEntry<BuildDirection> BuildDirection { get; private set; }
        public SettingEntry<float> Opacity { get; private set; }
        public SettingEntry<bool> DirectlyTeleportToWaypoint { get; private set; }
        public SettingEntry<KeyBinding> MapKeybinding { get; private set; }
        #endregion

        #region Location
        private const string LOCATION_SETTINGS = "event-table-location-settings";
        public SettingCollection LocationSettings { get; private set; }
        public SettingEntry<int> LocationX { get; private set; }
        public SettingEntry<int> LocationY { get; private set; }
        //public SettingEntry<int> Height { get; private set; }
        //public SettingEntry<bool> SnapHeight { get; private set; }
        public SettingEntry<int> Width { get; private set; }
        #endregion

        #region Events
        private const string EVENT_SETTINGS = "event-table-event-settings";
        private const string EVENT_LIST_SETTINGS = "event-table-event-list-settings";
        public SettingCollection EventSettings { get; private set; }
        public SettingEntry<int> EventTimeSpan { get; private set; } // Is listed in global
        public SettingEntry<int> EventHistorySplit { get; private set; } // Is listed in global
        public SettingEntry<int> EventHeight { get; private set; } // Is listed in global
        public SettingEntry<bool> DrawEventBorder { get; private set; } // Is listed in global
        public SettingEntry<ContentService.FontSize> EventFontSize { get; private set; } // Is listed in global
        public SettingEntry<bool> UseFiller { get; private set; } // Is listed in global
        public SettingEntry<bool> UseFillerEventNames { get; private set; } // Is listed in global
        public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> TextColor { get; private set; } // Is listed in global
        public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> FillerTextColor { get; private set; } // Is listed in global
        public SettingEntry<EventCompletedAction> EventCompletedAcion { get; private set; }
        public SettingEntry<bool> UseEventTranslation { get; private set; }

        private ReadOnlyCollection<SettingEntry<bool>> _allEvents;
        public ReadOnlyCollection<SettingEntry<bool>> AllEvents
        {
            get
            {
                using (_eventSettingsLock.Lock()) return _allEvents;
            }
        }
        #endregion

        public ModuleSettings(SettingCollection settings)
        {
            this.Settings = settings;

            this.BuildDefaultColor();

            this.InitializeGlobalSettings(settings);
            this.InitializeLocationSettings(settings);

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

        public async Task LoadAsync()
        {
            try
            {
                this.DefaultGW2Color = await EventTableModule.ModuleInstance.Gw2ApiManager.Gw2ApiClient.V2.Colors.GetAsync(1);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not load default gw2 color: {ex.Message}");
            }
        }

        private void InitializeGlobalSettings(SettingCollection settings)
        {
            this.GlobalSettings = settings.AddSubCollection(GLOBAL_SETTINGS);

            this.GlobalEnabled = this.GlobalSettings.DefineSetting(nameof(this.GlobalEnabled), true, () => Strings.Setting_GlobalEnabled_Name, () => Strings.Setting_GlobalEnabled_Description);
            this.GlobalEnabled.SettingChanged += this.SettingChanged;

            this.GlobalEnabledHotkey = this.GlobalSettings.DefineSetting(nameof(this.GlobalEnabledHotkey), new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.E), () => Strings.Setting_GlobalEnabledHotkey_Name, () => Strings.Setting_GlobalEnabledHotkey_Description);
            this.GlobalEnabledHotkey.SettingChanged += this.SettingChanged;
            this.GlobalEnabledHotkey.Value.Enabled = true;
            this.GlobalEnabledHotkey.Value.Activated += (s, e) => this.GlobalEnabled.Value = !this.GlobalEnabled.Value;
            this.GlobalEnabledHotkey.Value.BlockSequenceFromGw2 = true;

            this.RegisterCornerIcon = this.GlobalSettings.DefineSetting(nameof(this.RegisterCornerIcon), true, () => Strings.Setting_RegisterCornerIcon_Name, () => Strings.Setting_RegisterCornerIcon_Description);
            this.RegisterCornerIcon.SettingChanged += this.SettingChanged;

            this.RefreshRateDelay = this.GlobalSettings.DefineSetting(nameof(this.RefreshRateDelay), 900, () => Strings.Setting_RefreshRateDelay_Title, () => string.Format(Strings.Setting_RefreshRateDelay_Description, this.RefreshRateDelay.GetRange().Value.Min, this.RefreshRateDelay.GetRange().Value.Max));
            this.RefreshRateDelay.SettingChanged += this.SettingChanged;
            this.RefreshRateDelay.SetRange(0, 900);

            this.AutomaticallyUpdateEventFile = this.GlobalSettings.DefineSetting(nameof(this.AutomaticallyUpdateEventFile), true, () => Strings.Setting_AutomaticallyUpdateEventFile_Name, () => Strings.Setting_AutomaticallyUpdateEventFile_Description);
            this.AutomaticallyUpdateEventFile.SettingChanged += this.SettingChanged;

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

            this.BackgroundColor = this.GlobalSettings.DefineSetting(nameof(this.BackgroundColor), this.DefaultGW2Color, () => Strings.Setting_BackgroundColor_Name, () => Strings.Setting_BackgroundColor_Description);
            this.BackgroundColor.SettingChanged += this.SettingChanged;

            this.BackgroundColorOpacity = this.GlobalSettings.DefineSetting(nameof(this.BackgroundColorOpacity), 0.0f, () => Strings.Setting_BackgroundColorOpacity_Name, () => Strings.Setting_BackgroundColorOpacity_Description);
            this.BackgroundColorOpacity.SetRange(0.0f, 1f);
            this.BackgroundColorOpacity.SettingChanged += this.SettingChanged;

            this.EventTimeSpan = this.GlobalSettings.DefineSetting(nameof(this.EventTimeSpan), 120, () => Strings.Setting_EventTimeSpan_Name, () => Strings.Setting_EventTimeSpan_Description);
            this.EventTimeSpan.SettingChanged += this.SettingChanged;
            this.EventTimeSpan.SetValidation(val =>
            {
                bool isValid = true;
                string message = null;
                double limit = 1440;

                if (val > limit)
                {
                    isValid = false;
                    message = string.Format(Strings.Setting_EventTimeSpan_Validation_OverLimit, limit);
                }

                return new SettingValidationResult(isValid, message);
            });

            this.EventHistorySplit = this.GlobalSettings.DefineSetting(nameof(this.EventHistorySplit), 50, () => Strings.Setting_EventHistorySplit_Name, () => Strings.Setting_EventHistorySplit_Description);
            this.EventHistorySplit.SetRange(0, 75);
            this.EventHistorySplit.SettingChanged += this.SettingChanged;

            this.EventHeight = this.GlobalSettings.DefineSetting(nameof(this.EventHeight), 20, () => Strings.Setting_EventHeight_Name, () => Strings.Setting_EventHeight_Description);
            this.EventHeight.SetRange(5, 50);
            this.EventHeight.SettingChanged += this.SettingChanged;

            this.EventFontSize = this.GlobalSettings.DefineSetting(nameof(this.EventFontSize), ContentService.FontSize.Size16, () => Strings.Setting_EventFontSize_Name, () => Strings.Setting_EventFontSize_Description);
            this.EventFontSize.SettingChanged += this.SettingChanged;

            this.DrawEventBorder = this.GlobalSettings.DefineSetting(nameof(this.DrawEventBorder), true, () => Strings.Setting_DrawEventBorder_Name, () => Strings.Setting_DrawEventBorder_Description);
            this.DrawEventBorder.SettingChanged += this.SettingChanged;

            this.DebugEnabled = this.GlobalSettings.DefineSetting(nameof(this.DebugEnabled), false, () => Strings.Setting_DebugEnabled_Name, () => Strings.Setting_DebugEnabled_Description);
            this.DebugEnabled.SettingChanged += this.SettingChanged;

            this.ShowTooltips = this.GlobalSettings.DefineSetting(nameof(this.ShowTooltips), true, () => Strings.Setting_ShowTooltips_Name, () => Strings.Setting_ShowTooltips_Description);
            this.ShowTooltips.SettingChanged += this.SettingChanged;

            this.HandleLeftClick = this.GlobalSettings.DefineSetting(nameof(this.HandleLeftClick), true, () => Strings.Setting_HandleLeftClick_Name, () => Strings.Setting_HandleLeftClick_Description);
            this.HandleLeftClick.SettingChanged += this.SettingChanged;

            this.LeftClickAction = this.GlobalSettings.DefineSetting(nameof(this.LeftClickAction), Models.LeftClickAction.CopyWaypoint, () => Strings.Setting_LeftClickAction_Title, () => Strings.Setting_LeftClickAction_Description);
            this.LeftClickAction.SettingChanged += this.SettingChanged;

            this.DirectlyTeleportToWaypoint = this.GlobalSettings.DefineSetting(nameof(this.DirectlyTeleportToWaypoint), false, () => Strings.Setting_DirectlyTeleportToWaypoint_Title, () => Strings.Setting_DirectlyTeleportToWaypoint_Description);
            this.DirectlyTeleportToWaypoint.SettingChanged += this.SettingChanged;

            this.ShowContextMenuOnClick = this.GlobalSettings.DefineSetting(nameof(this.ShowContextMenuOnClick), true, () => Strings.Setting_ShowContextMenuOnClick_Name, () => Strings.Setting_ShowContextMenuOnClick_Description);
            this.ShowContextMenuOnClick.SettingChanged += this.SettingChanged;

            this.BuildDirection = this.GlobalSettings.DefineSetting(nameof(this.BuildDirection), Models.BuildDirection.Top, () => Strings.Setting_BuildDirection_Name, () => Strings.Setting_BuildDirection_Description);
            this.BuildDirection.SettingChanged += this.SettingChanged;

            this.Opacity = this.GlobalSettings.DefineSetting(nameof(this.Opacity), 1f, () => Strings.Setting_Opacity_Name, () => Strings.Setting_Opacity_Description);
            this.Opacity.SetRange(0.1f, 1f);
            this.Opacity.SettingChanged += this.SettingChanged;

            this.UseFiller = this.GlobalSettings.DefineSetting(nameof(this.UseFiller), false, () => Strings.Setting_UseFiller_Name, () => Strings.Setting_UseFiller_Description);
            this.UseFiller.SettingChanged += this.SettingChanged;

            this.UseFillerEventNames = this.GlobalSettings.DefineSetting(nameof(this.UseFillerEventNames), false, () => Strings.Setting_UseFillerEventNames_Name, () => Strings.Setting_UseFillerEventNames_Description);
            this.UseFillerEventNames.SettingChanged += this.SettingChanged;

            this.TextColor = this.GlobalSettings.DefineSetting(nameof(this.TextColor), this.DefaultGW2Color, () => Strings.Setting_TextColor_Name, () => Strings.Setting_TextColor_Description);
            this.TextColor.SettingChanged += this.SettingChanged;

            this.FillerTextColor = this.GlobalSettings.DefineSetting(nameof(this.FillerTextColor), this.DefaultGW2Color, () => Strings.Setting_FillerTextColor_Name, () => Strings.Setting_FillerTextColor_Description);
            this.FillerTextColor.SettingChanged += this.SettingChanged;

            this.EventCompletedAcion = this.GlobalSettings.DefineSetting(nameof(this.EventCompletedAcion), EventCompletedAction.Crossout, () => Strings.Setting_EventCompletedAction_Name, () => Strings.Setting_EventCompletedAction_Description);
            this.EventCompletedAcion.SettingChanged += this.SettingChanged;

            this.UseEventTranslation = this.GlobalSettings.DefineSetting(nameof(this.UseEventTranslation), true, () => Strings.Setting_UseEventTranslation_Name, () => Strings.Setting_UseEventTranslation_Description);
            this.UseEventTranslation.SettingChanged += this.SettingChanged;

            this.MapKeybinding = this.GlobalSettings.DefineSetting(nameof(this.MapKeybinding), new KeyBinding( Microsoft.Xna.Framework.Input.Keys.M), () => "Open Map Hotkey", () => "Defines the key used to open the fullscreen map.");
            this.MapKeybinding.SettingChanged += this.SettingChanged;
            this.MapKeybinding.Value.Enabled = true;
            this.MapKeybinding.Value.BlockSequenceFromGw2 = false;
        }

        private void InitializeLocationSettings(SettingCollection settings)
        {
            this.LocationSettings = settings.AddSubCollection(LOCATION_SETTINGS);

            int height = 1080;
            int width = 1920;

            this.LocationX = this.LocationSettings.DefineSetting(nameof(this.LocationX), (int)(width * 0.1), () => Strings.Setting_LocationX_Name, () => Strings.Setting_LocationX_Description);
            this.LocationX.SetRange(0, width);
            this.LocationX.SettingChanged += this.SettingChanged;

            this.LocationY = this.LocationSettings.DefineSetting(nameof(this.LocationY), (int)(height * 0.1), () => Strings.Setting_LocationY_Name, () => Strings.Setting_LocationY_Description);
            this.LocationY.SetRange(0, height);
            this.LocationY.SettingChanged += this.SettingChanged;

            this.Width = this.LocationSettings.DefineSetting(nameof(this.Width), (int)(width * 0.5), () => Strings.Setting_Width_Name, () => Strings.Setting_Width_Description);
            this.Width.SetRange(0, width);
            this.Width.SettingChanged += this.SettingChanged;
        }

        public void InitializeEventSettings(IEnumerable<EventCategory> eventCategories)
        {
            using (_eventSettingsLock.Lock())
            {
                this.EventSettings = this.Settings.AddSubCollection(EVENT_SETTINGS);

                SettingCollection eventList = this.EventSettings.AddSubCollection(EVENT_LIST_SETTINGS);

                var eventSettingList = new List<SettingEntry<bool>>();

                foreach (EventCategory category in eventCategories)
                {
                    IEnumerable<Event> events = category.ShowCombined ? category.Events.GroupBy(e => e.Key).Select(eg => eg.First()) : category.Events;
                    foreach (Event e in events)
                    {
                        SettingEntry<bool> setting = eventList.DefineSetting<bool>(e.SettingKey, true);
                        setting.SettingChanged += (s, e) =>
                        {
                            SettingEntry<bool> settingEntry = (SettingEntry<bool>)s;
                            this.EventSettingChanged?.Invoke(s, new EventSettingsChangedEventArgs()
                            {
                                Name = settingEntry.EntryKey,
                                Enabled = e.NewValue
                            });

                            this.SettingChanged(s, e);
                        };

                        eventSettingList.Add(setting);
                    }
                }

                this._allEvents = eventSettingList.AsReadOnly();
            }
        }

        private void SettingChanged<T>(object sender, ValueChangedEventArgs<T> e)
        {
            SettingEntry<T> settingEntry = (SettingEntry<T>)sender;
            string prevValue = e.PreviousValue.GetType() == typeof(string) ? e.PreviousValue.ToString() : JsonConvert.SerializeObject(e.PreviousValue);
            string newValue = e.NewValue.GetType() == typeof(string) ? e.NewValue.ToString() : JsonConvert.SerializeObject(e.NewValue);
            Logger.Debug($"Changed setting \"{settingEntry.EntryKey}\" from \"{prevValue}\" to \"{newValue}\"");

            ModuleSettingsChanged?.Invoke(this, new ModuleSettingsChangedEventArgs() { Name = settingEntry.EntryKey, Value = e.NewValue });
        }

        public class ModuleSettingsChangedEventArgs
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }

        public class EventSettingsChangedEventArgs
        {
            public string Name { get; set; }
            public bool Enabled { get; set; }
        }
    }
}
