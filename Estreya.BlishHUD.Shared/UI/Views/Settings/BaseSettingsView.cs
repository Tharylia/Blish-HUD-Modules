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
        private readonly SettingEventState _settingEventState;

        protected BaseSettingsView(Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, SettingEventState settingEventState, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
        {
            base.LABEL_WIDTH = 250;
            this.CONTROL_WIDTH = 250;

            this.CONTROL_LOCATION = new Point(base.LABEL_WIDTH + 20, 0);
            this._settingEventState = settingEventState;
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

        protected abstract void BuildView(FlowPanel parent);

        protected (Panel Panel, Label label, ColorBox colorBox) RenderColorSetting(Panel parent, SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> settingEntry)
        {
            Panel panel = this.GetPanel(parent);
            var label = base.RenderLabel(panel, settingEntry.DisplayName);

            var colorBox = base.RenderColorBox(panel, this.CONTROL_LOCATION, settingEntry.Value, color => settingEntry.Value = color, this.MainPanel);

            colorBox.BasicTooltipText = settingEntry.Description;

            this.SetControlEnabledState(colorBox, settingEntry);
            this.AddControlForDisabledCheck(colorBox, settingEntry);

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

            this.SetControlEnabledState(textBox, settingEntry);
            this.AddControlForDisabledCheck(textBox, settingEntry);

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

            _settingEventState.AddForRangeCheck(settingEntry);
            _settingEventState.RangeUpdated += (s, e) =>
            {
                if (e.SettingEntry.EntryKey == settingEntry.EntryKey)
                {
                    var range = (IntRangeRangeComplianceRequisite)e.NewCompliance;
                    trackbar.MinValue = range.MinValue;
                    trackbar.MaxValue = range.MaxValue;
                }
            };

            trackbar.Disposed += (s, e) =>
            {
                _settingEventState.RemoveFromRangeCheck(settingEntry);
            };

            this.SetControlEnabledState(trackbar, settingEntry);
            this.AddControlForDisabledCheck(trackbar, settingEntry);

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

            _settingEventState.AddForRangeCheck(settingEntry);
            _settingEventState.RangeUpdated += (s, e) =>
            {
                if (e.SettingEntry.EntryKey == settingEntry.EntryKey)
                {
                    var range = (FloatRangeRangeComplianceRequisite)e.NewCompliance;
                    trackbar.MinValue = range.MinValue;
                    trackbar.MaxValue = range.MaxValue;
                }
            };

            trackbar.Disposed += (s, e) =>
            {
                _settingEventState.RemoveFromRangeCheck(settingEntry);
            };

            this.SetControlEnabledState(trackbar, settingEntry);
            this.AddControlForDisabledCheck(trackbar, settingEntry);

            return (panel, label.TitleLabel, trackbar);
        }

        protected (Panel Panel, Label label, Checkbox checkbox) RenderBoolSetting(Panel parent, SettingEntry<bool> settingEntry)
        {
            Panel panel = this.GetPanel(parent);

            var label = base.RenderLabel(panel, settingEntry.DisplayName);

            var checkbox = base.RenderCheckbox(panel, this.CONTROL_LOCATION, settingEntry.Value, onChangeAction: newValue =>
            {
                settingEntry.Value = newValue;
            });

            checkbox.BasicTooltipText = settingEntry.Description;

            this.SetControlEnabledState(checkbox, settingEntry);
            this.AddControlForDisabledCheck(checkbox, settingEntry);

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

            this.SetControlEnabledState(keybindingAssigner, settingEntry);
            this.AddControlForDisabledCheck(keybindingAssigner, settingEntry);

            return (panel, label.TitleLabel, keybindingAssigner);
        }

        protected (Panel Panel, Label label, Dropdown dropdown) RenderEnumSetting<T>(Panel parent, SettingEntry<T> settingEntry) where T : Enum
        {
            Panel panel = this.GetPanel(parent);

            var label = base.RenderLabel(panel, settingEntry.DisplayName);

            var casing = LetterCasing.Title;

            var values = ((T[])Enum.GetValues(settingEntry.SettingType)).ToList();
            var formattedValues = values.Select(value => value.Humanize(casing)).ToArray();

            var dropdown = base.RenderDropdown(panel, this.CONTROL_LOCATION, CONTROL_WIDTH, formattedValues, settingEntry.Value.Humanize(casing), onChangeAction: newValue =>
            {
                settingEntry.Value = values[formattedValues.ToList().IndexOf(newValue)];
            });

            dropdown.BasicTooltipText = settingEntry.Description;

            this.SetControlEnabledState(dropdown, settingEntry);
            this.AddControlForDisabledCheck(dropdown, settingEntry);

            return (panel, label.TitleLabel, dropdown);
        }

        private void AddControlForDisabledCheck(Control control, SettingEntry settingEntry)
        {
            _settingEventState.AddForDisabledCheck(settingEntry);
            _settingEventState.DisabledUpdated += (s, e) =>
            {
                if (e.SettingEntry.EntryKey == settingEntry.EntryKey)
                {
                    var disabled = (SettingDisabledComplianceRequisite)e.NewCompliance;
                    control.Enabled = !disabled.Disabled;
                }
            };

            control.Disposed += (s, e) =>
            {
                _settingEventState.RemoveFromDisabledCheck(settingEntry);
            };
        }

        private void SetControlEnabledState(Control control, SettingEntry settingEntry)
        {
            var compliances = settingEntry.GetComplianceRequisite().Where(c => c is SettingDisabledComplianceRequisite).Select(c => (SettingDisabledComplianceRequisite)c);
            if (compliances.Any())
            {
                control.Enabled = !compliances.First().Disabled;
            }
        }

        protected override void Unload()
        {
            base.Unload();
        }
    }
}
