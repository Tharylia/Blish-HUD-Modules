namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Resources;
    using Estreya.BlishHUD.EventTable.UI.Views.Controls;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public abstract class BaseSettingsView : BaseView
    {
        private const int LEFT_PADDING = 20;
        private const int CONTROL_X_SPACING = 20;
        private const int LABEL_WIDTH = 250;
        private const int BINDING_WIDTH = 170;

        private static readonly Logger Logger = Logger.GetLogger<BaseSettingsView>();
        protected ModuleSettings ModuleSettings { get; set; }

        private static IEnumerable<Gw2Sharp.WebApi.V2.Models.Color> Colors { get; set; }

        private static Panel ColorPickerPanel { get; set; }

        private static string SelectedColorSetting { get; set; }

        private static ColorPicker ColorPicker { get; set; }

        public BaseSettingsView(ModuleSettings settings)
        {
            this.ModuleSettings = settings;
        }

        protected override async Task<bool> Load(IProgress<string> progress)
        {
            if (Colors == null)
            {
                progress.Report(Strings.BaseSettingsView_LoadingColors);

                try
                {
                    Colors = await EventTableModule.ModuleInstance.Gw2ApiManager.Gw2ApiClient.V2.Colors.AllAsync();
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Could not load gw2 colors: {ex.Message}");
                    if (this.ModuleSettings.DefaultGW2Color != null)
                    {
                        Logger.Debug($"Adding default color: {this.ModuleSettings.DefaultGW2Color.Name}");
                        Colors = new List<Gw2Sharp.WebApi.V2.Models.Color>() { this.ModuleSettings.DefaultGW2Color };
                    }
                }
            }

            if (ColorPicker == null)
            {
                progress.Report(Strings.BaseSettingsView_LoadingColorPicker);
                // build initial colorpicker

                ColorPickerPanel = new Panel()
                {
                    Location = new Point(10, 10),
                    WidthSizingMode = SizingMode.AutoSize,
                    HeightSizingMode = SizingMode.AutoSize,
                    Visible = false,
                    ZIndex = int.MaxValue,
                    BackgroundColor = Color.Black,
                    ShowBorder = false,
                };

                ColorPicker = new ColorPicker()
                {
                    Location = new Point(10, 10),
                    Parent = ColorPickerPanel,
                    Visible = true
                };

                progress.Report(Strings.BaseSettingsView_AddingColorsToColorPicker);
                if (Colors != null)
                {
                    foreach (Gw2Sharp.WebApi.V2.Models.Color color in Colors.OrderBy(color => color.Categories.FirstOrDefault()))
                    {
                        ColorPicker.Colors.Add(color);
                    }
                }
            }

            progress.Report(string.Empty);

            return await this.InternalLoad(progress);
        }

        protected sealed override void InternalBuild(Panel parent)
        {
            Rectangle bounds = parent.ContentRegion;

            FlowPanel parentPanel = new FlowPanel()
            {
                Size = bounds.Size,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                ControlPadding = new Vector2(5, 2),
                OuterControlPadding = new Vector2(LEFT_PADDING, 15),
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.Fill,
                AutoSizePadding = new Point(0, 15),
                Parent = parent
            };

            this.BuildView(parentPanel);
        }

        protected abstract void BuildView(Panel parent);

        protected Panel RenderChangedTypeSetting<T, TOverride>(Panel parent, SettingEntry<T> setting, Func<TOverride, T> convertFunction)
        {
            Panel panel = this.GetPanel(parent);

            Label label = this.GetLabel(panel, setting.DisplayName);

            try
            {
                Control ctrl = ControlHandler.CreateFromChangedTypeSetting<T, TOverride>(setting, (settingEntry, val) =>
                {
                    T converted = convertFunction(val);
                    return this.HandleValidation(settingEntry, converted);
                }, BINDING_WIDTH, -1, label.Right + CONTROL_X_SPACING, 0);
                ctrl.Parent = panel;
                ctrl.BasicTooltipText = setting.Description;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Type \"{setting.SettingType.FullName}\" could not be found in internal type lookup:");
            }

            return panel;
        }

        protected Panel RenderSetting<T>(Panel parent, SettingEntry<T> setting)
        {
            return this.RenderChangedTypeSetting(parent, setting, (T val) => val);
        }

        protected Panel RenderSetting<T>(Panel parent, SettingEntry<T> setting, Action<T> onChangeAction)
        {
            Panel panel = this.RenderSetting(parent, setting);

            setting.SettingChanged += (s, e) =>
            {
                onChangeAction?.Invoke(e.NewValue);
            };

            return panel;
        }

        protected void RenderColorSetting(Panel parent, SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> setting)
        {
            Panel panel = this.GetPanel(parent);
            Label label = this.GetLabel(panel, setting.DisplayName);

            ColorBox colorBox = new ColorBox()
            {
                Location = new Point(label.Right + CONTROL_X_SPACING, 0),
                Parent = panel,
                Color = setting.Value
            };

            colorBox.LeftMouseButtonPressed += (s, e) =>
            {
                ColorPickerPanel.Parent = parent.Parent;
                ColorPickerPanel.Size = new Point(parent.Width - 30, 850);
                ColorPicker.Size = new Point(ColorPickerPanel.Size.X - 20, ColorPickerPanel.Size.Y - 20);

                // Hack to get lineup right
                Gw2Sharp.WebApi.V2.Models.Color tempColor = new Gw2Sharp.WebApi.V2.Models.Color()
                {
                    Id = int.MaxValue,
                    Name = "temp"
                };

                ColorPicker.RecalculateLayout();
                ColorPicker.Colors.Add(tempColor);
                ColorPicker.Colors.Remove(tempColor);

                ColorPickerPanel.Visible = !ColorPickerPanel.Visible;
                SelectedColorSetting = setting.EntryKey;
            };

            ColorPicker.SelectedColorChanged += (sender, eArgs) =>
            {
                if (SelectedColorSetting != setting.EntryKey)
                {
                    return;
                }

                Gw2Sharp.WebApi.V2.Models.Color selectedColor = ColorPicker.SelectedColor;

                if (!this.HandleValidation(setting, selectedColor))
                {
                    selectedColor = setting.Value;
                }

                setting.Value = selectedColor;
                ColorPickerPanel.Visible = false;
                colorBox.Color = selectedColor;
            };
        }

        private bool HandleValidation<T>(SettingEntry<T> settingEntry, T value)
        {
            SettingValidationResult result = settingEntry.CheckValidation(value);

            if (!result.Valid)
            {
                this.ShowError(result.InvalidMessage);
                return false;
            }

            return true;
        }

        protected override void Unload()
        {
            base.Unload();
        }
    }
}
