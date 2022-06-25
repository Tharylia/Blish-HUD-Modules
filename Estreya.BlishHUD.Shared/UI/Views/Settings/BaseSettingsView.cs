namespace Estreya.BlishHUD.Shared.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.UI.Views.Controls;
    using Microsoft.Xna.Framework;
    using System;

    public abstract class BaseSettingsView : BaseView
    {
        private const int LEFT_PADDING = 20;
        private const int CONTROL_X_SPACING = 20;
        private const int LABEL_WIDTH = 250;
        private const int BINDING_WIDTH = 170;

        private static readonly Logger Logger = Logger.GetLogger<BaseSettingsView>();

        protected sealed override void DoBuild(Panel parent)
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
                this.SelectedColorSetting = setting.EntryKey;
            };

            ColorPicker.SelectedColorChanged += (sender, eArgs) =>
            {
                if (this.SelectedColorSetting != setting.EntryKey)
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
