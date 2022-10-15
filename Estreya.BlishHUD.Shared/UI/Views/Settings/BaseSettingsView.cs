namespace Estreya.BlishHUD.Shared.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Input;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Controls;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.State;
    using Humanizer;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Linq;
    using System.Reflection;
    using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;

    public abstract class BaseSettingsView : BaseView
    {
        private readonly int CONTROL_WIDTH;
        private readonly Point CONTROL_LOCATION;

        private static readonly Logger Logger = Logger.GetLogger<BaseSettingsView>();

        protected BaseSettingsView(Gw2ApiManager apiManager, IconState iconState, BitmapFont font = null) : base(apiManager, iconState, font) {
            base.LABEL_WIDTH = 250;
            this.CONTROL_WIDTH = 250;

            this.CONTROL_LOCATION = new Point(base.LABEL_WIDTH + 20, 0);
        }

        protected sealed override void InternalBuild(Panel parent)
        {
            Rectangle bounds = parent.ContentRegion;

            FlowPanel parentPanel = new FlowPanel()
            {
                Size = bounds.Size,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                ControlPadding = new Vector2(5, 2),
                OuterControlPadding = new Vector2(20, 15),
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.Fill,
                AutoSizePadding = new Point(0, 15),
                Parent = parent
            };

            this.BuildView(parentPanel);
        }

        protected abstract void BuildView(Panel parent);

        protected (Panel Panel, Label label, ColorBox colorBox) RenderColorSetting(Panel parent, SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> settingEntry)
        {
            Panel panel = this.GetPanel(parent);
            var label = base.RenderLabel(panel, settingEntry.DisplayName);

            var colorBox = base.RenderColorBox(panel, this.CONTROL_LOCATION, settingEntry.Value, color => settingEntry.Value = color, this.MainPanel);

            colorBox.BasicTooltipText = settingEntry.Description;

            return (panel, label.TitleLabel, colorBox);
        }

        protected (Panel Panel, Label label, TextBox textBox) RenderTextSetting(Panel parent, SettingEntry<string> settingEntry)
        {
            Panel panel = this.GetPanel(parent);

            var label = base.RenderLabel(panel, settingEntry.DisplayName);

            var textBox = base.RenderTextbox(panel, this.CONTROL_LOCATION, CONTROL_WIDTH, settingEntry.Value, settingEntry.Description, onChangeAction: newValue =>
            {
                settingEntry.Value = newValue;
            });

            textBox.BasicTooltipText = settingEntry.Description;

            return (panel, label.TitleLabel, textBox);
        }

        protected (Panel Panel, Label label, TrackBar trackBar) RenderIntSetting(Panel parent, SettingEntry<int> settingEntry)
        {
            Panel panel = this.GetPanel(parent);

            var label = base.RenderLabel(panel, settingEntry.DisplayName);
            var range = settingEntry.GetRange();

            var trackbar = base.RenderTrackBar(panel, this.CONTROL_LOCATION, CONTROL_WIDTH, settingEntry.Value, ((int)(range?.Min ?? 0), (int)(range?.Max ?? 100)), onChangeAction: newValue =>
            {
                settingEntry.Value = newValue;
            });

            trackbar.BasicTooltipText = settingEntry.Description;

            return (panel, label.TitleLabel, trackbar);
        }

        protected (Panel Panel, Label label, TrackBar trackBar) RenderFloatSetting(Panel parent, SettingEntry<float> settingEntry)
        {
            Panel panel = this.GetPanel(parent);

            var label = base.RenderLabel(panel, settingEntry.DisplayName);
            var range = settingEntry.GetRange();

            var trackbar = base.RenderTrackBar(panel, this.CONTROL_LOCATION, CONTROL_WIDTH, settingEntry.Value, range, onChangeAction: newValue =>
            {
                settingEntry.Value = newValue;
            });

            trackbar.BasicTooltipText = settingEntry.Description;

            return (panel, label.TitleLabel, trackbar);
        }

        protected (Panel Panel, Label label, Checkbox checkbox) RenderBoolSetting(Panel parent, SettingEntry<bool> settingEntry)
        {
            Panel panel = this.GetPanel(parent);

            var label = base.RenderLabel(panel, settingEntry.DisplayName);

            var checkbox = base.RenderCheckbox(panel, this.CONTROL_LOCATION, settingEntry.Value , onChangeAction: newValue =>
            {
                settingEntry.Value = newValue;
            });

            checkbox.BasicTooltipText = settingEntry.Description;

            return (panel, label.TitleLabel, checkbox);
        }

        protected (Panel Panel, Label label, Shared.Controls.KeybindingAssigner keybindingAssigner) RenderKeybindingSetting(Panel parent, SettingEntry<KeyBinding> settingEntry)
        {
            Panel panel = this.GetPanel(parent);

            var label = base.RenderLabel(panel, settingEntry.DisplayName);

            var keybindingAssigner = base.RenderKeybinding(panel, this.CONTROL_LOCATION, CONTROL_WIDTH, settingEntry.Value, onChangeAction: newValue =>
            {
                settingEntry.Value = newValue;
                GameService.Settings.Save(); // Force save as it is not a new object
            });

            keybindingAssigner.BasicTooltipText = settingEntry.Description;

            return (panel, label.TitleLabel, keybindingAssigner);
        }

        protected (Panel Panel, Label label, Dropdown dropdown) RenderEnumSetting<T>(Panel parent, SettingEntry<T> settingEntry) where T: Enum
        {
            Panel panel = this.GetPanel(parent);

            var label = base.RenderLabel(panel, settingEntry.DisplayName);

            var casing = LetterCasing.Title;

            var values = ((T[])Enum.GetValues(settingEntry.SettingType)).ToList();
            var formattedValues = values.Select(value => value.Humanize(casing)).ToArray();

            var dropdown = base.RenderDropdown(panel, this.CONTROL_LOCATION, CONTROL_WIDTH, formattedValues,  settingEntry.Value.Humanize(casing), onChangeAction: newValue =>
            {
                settingEntry.Value = values[formattedValues.ToList().IndexOf(newValue)];
            });

            dropdown.BasicTooltipText = settingEntry.Description;

            return (panel, label.TitleLabel, dropdown);
        }

        protected override void Unload()
        {
            base.Unload();
        }
    }
}
