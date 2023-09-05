namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Extensions;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Color = Gw2Sharp.WebApi.V2.Models.Color;
using KeybindingAssigner = Controls.KeybindingAssigner;

public abstract class BaseSettingsView : BaseView
{
    private static readonly Logger Logger = Logger.GetLogger<BaseSettingsView>();
    private readonly SettingEventService _settingEventService;
    private readonly Point CONTROL_LOCATION;
    private readonly int CONTROL_WIDTH;

    protected BaseSettingsView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService) : base(apiManager, iconService, translationService)
    {
        this.LABEL_WIDTH = 250;
        this.CONTROL_WIDTH = 250;

        this.CONTROL_LOCATION = new Point(this.LABEL_WIDTH + 20, 0);
        this._settingEventService = settingEventService;
    }

    protected sealed override void InternalBuild(Panel parent)
    {
        Rectangle bounds = parent.ContentRegion;

        FlowPanel parentPanel = new FlowPanel
        {
            Size = bounds.Size,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            ControlPadding = new Vector2(5, 2),
            OuterControlPadding = new Vector2(20, 15),
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.Fill,
            //AutoSizePadding = new Point(0, 15),
            Parent = parent
        };

        this.BuildView(parentPanel);
    }

    protected abstract void BuildView(FlowPanel parent);

    protected (Panel Panel, Label label, ColorBox colorBox) RenderColorSetting(Panel parent, SettingEntry<Color> settingEntry)
    {
        Panel panel = this.GetPanel(parent);
        (Label TitleLabel, Label ValueLabel) label = this.RenderLabel(panel, settingEntry.DisplayName);

        ColorBox colorBox = this.RenderColorBox(panel, this.CONTROL_LOCATION, settingEntry.Value, color =>
        {
            try
            {
                settingEntry.Value = color;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        }, this.MainPanel);

        colorBox.BasicTooltipText = settingEntry.Description;

        this.SetControlEnabledState(colorBox, settingEntry);
        this.AddControlForDisabledCheck(colorBox, settingEntry);

        return (panel, label.TitleLabel, colorBox);
    }

    protected (Panel Panel, Label label, TextBox textBox) RenderTextSetting(Panel parent, SettingEntry<string> settingEntry, Func<string, string, Task<bool>> onBeforeChangeAction = null)
    {
        Panel panel = this.GetPanel(parent);

        (Label TitleLabel, Label ValueLabel) label = this.RenderLabel(panel, settingEntry.DisplayName);

        TextBox textBox = this.RenderTextbox(panel, this.CONTROL_LOCATION, this.CONTROL_WIDTH, settingEntry.Value, settingEntry.Description, newValue =>
        {
            try
            {
                settingEntry.Value = newValue;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        }, onBeforeChangeAction: onBeforeChangeAction);

        textBox.BasicTooltipText = settingEntry.Description;

        this.SetControlEnabledState(textBox, settingEntry);
        this.AddControlForDisabledCheck(textBox, settingEntry);

        return (panel, label.TitleLabel, textBox);
    }

    protected (Panel Panel, Label label, TrackBar trackBar) RenderIntSetting(Panel parent, SettingEntry<int> settingEntry)
    {
        Panel panel = this.GetPanel(parent);

        (Label TitleLabel, Label ValueLabel) label = this.RenderLabel(panel, settingEntry.DisplayName);
        (float Min, float Max)? range = settingEntry.GetRange();

        TrackBar trackbar = this.RenderTrackBar(panel, this.CONTROL_LOCATION, this.CONTROL_WIDTH, settingEntry.Value, ((int)(range?.Min ?? 0), (int)(range?.Max ?? 100)), newValue =>
        {
            try
            {
                settingEntry.Value = newValue;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });

        trackbar.BasicTooltipText = settingEntry.Description;

        this._settingEventService.AddForRangeCheck(settingEntry);
        this._settingEventService.RangeUpdated += (s, e) =>
        {
            if (e.SettingEntry.EntryKey == settingEntry.EntryKey)
            {
                IntRangeRangeComplianceRequisite range = (IntRangeRangeComplianceRequisite)e.NewCompliance;
                trackbar.MinValue = range.MinValue;
                trackbar.MaxValue = range.MaxValue;
            }
        };

        trackbar.Disposed += (s, e) =>
        {
            this._settingEventService.RemoveFromRangeCheck(settingEntry);
        };

        this.SetControlEnabledState(trackbar, settingEntry);
        this.AddControlForDisabledCheck(trackbar, settingEntry);

        return (panel, label.TitleLabel, trackbar);
    }

    protected (Panel Panel, Label label, TrackBar trackBar) RenderFloatSetting(Panel parent, SettingEntry<float> settingEntry)
    {
        Panel panel = this.GetPanel(parent);

        (Label TitleLabel, Label ValueLabel) label = this.RenderLabel(panel, settingEntry.DisplayName);
        (float Min, float Max)? range = settingEntry.GetRange();

        TrackBar trackbar = this.RenderTrackBar(panel, this.CONTROL_LOCATION, this.CONTROL_WIDTH, settingEntry.Value, range, newValue =>
        {
            try
            {
                settingEntry.Value = newValue;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });

        trackbar.BasicTooltipText = settingEntry.Description;

        this._settingEventService.AddForRangeCheck(settingEntry);
        this._settingEventService.RangeUpdated += (s, e) =>
        {
            if (e.SettingEntry.EntryKey == settingEntry.EntryKey)
            {
                FloatRangeRangeComplianceRequisite range = (FloatRangeRangeComplianceRequisite)e.NewCompliance;
                trackbar.MinValue = range.MinValue;
                trackbar.MaxValue = range.MaxValue;
            }
        };

        trackbar.Disposed += (s, e) =>
        {
            this._settingEventService.RemoveFromRangeCheck(settingEntry);
        };

        this.SetControlEnabledState(trackbar, settingEntry);
        this.AddControlForDisabledCheck(trackbar, settingEntry);

        return (panel, label.TitleLabel, trackbar);
    }

    protected (Panel Panel, Label label, Checkbox checkbox) RenderBoolSetting(Panel parent, SettingEntry<bool> settingEntry, Func<bool, bool, Task<bool>> onBeforeChangeAction = null)
    {
        Panel panel = this.GetPanel(parent);

        (Label TitleLabel, Label ValueLabel) label = this.RenderLabel(panel, settingEntry.DisplayName);

        Checkbox checkbox = this.RenderCheckbox(panel, this.CONTROL_LOCATION, settingEntry.Value, newValue =>
        {
            try
            {
                settingEntry.Value = newValue;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        }, onBeforeChangeAction);

        checkbox.BasicTooltipText = settingEntry.Description;

        this.SetControlEnabledState(checkbox, settingEntry);
        this.AddControlForDisabledCheck(checkbox, settingEntry);

        return (panel, label.TitleLabel, checkbox);
    }

    protected (Panel Panel, Label label, KeybindingAssigner keybindingAssigner) RenderKeybindingSetting(Panel parent, SettingEntry<KeyBinding> settingEntry)
    {
        Panel panel = this.GetPanel(parent);

        (Label TitleLabel, Label ValueLabel) label = this.RenderLabel(panel, settingEntry.DisplayName);

        KeybindingAssigner keybindingAssigner = this.RenderKeybinding(panel, this.CONTROL_LOCATION, this.CONTROL_WIDTH, settingEntry.Value, newValue =>
        {
            try
            {
                settingEntry.Value = newValue;
                GameService.Settings.Save(); // Force save as it is not a new object
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });

        keybindingAssigner.BasicTooltipText = settingEntry.Description;

        this.SetControlEnabledState(keybindingAssigner, settingEntry);
        this.AddControlForDisabledCheck(keybindingAssigner, settingEntry);

        return (panel, label.TitleLabel, keybindingAssigner);
    }

    protected (Panel Panel, Label label, Controls.Dropdown<string> dropdown) RenderEnumSetting<T>(Panel parent, SettingEntry<T> settingEntry) where T : struct, Enum
    {
        Panel panel = this.GetPanel(parent);

        (Label TitleLabel, Label ValueLabel) label = this.RenderLabel(panel, settingEntry.DisplayName);

        List<T> values = new List<T>();

        IEnumerable<EnumInclusionComplianceRequisite<T>> requisite = settingEntry.GetComplianceRequisite().Where(cr => cr is EnumInclusionComplianceRequisite<T>).Select(cr => (EnumInclusionComplianceRequisite<T>)cr);
        if (requisite.Any())
        {
            values.AddRange(requisite.First().IncludedValues);
        }
        else
        {
            values.AddRange((T[])Enum.GetValues(settingEntry.SettingType));
        }

        Controls.Dropdown<string> dropdown = this.RenderDropdown(panel, this.CONTROL_LOCATION, this.CONTROL_WIDTH, settingEntry.Value, values.ToArray(), newValue =>
        {
            try
            {
                settingEntry.Value = newValue;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });

        dropdown.BasicTooltipText = settingEntry.Description;

        this.SetControlEnabledState(dropdown, settingEntry);
        this.AddControlForDisabledCheck(dropdown, settingEntry);

        return (panel, label.TitleLabel, dropdown);
    }

    private void AddControlForDisabledCheck(Control control, SettingEntry settingEntry)
    {
        this._settingEventService.AddForDisabledCheck(settingEntry);
        this._settingEventService.DisabledUpdated += (s, e) =>
        {
            if (e.SettingEntry.EntryKey == settingEntry.EntryKey)
            {
                SettingDisabledComplianceRequisite disabled = (SettingDisabledComplianceRequisite)e.NewCompliance;
                control.Enabled = !disabled.Disabled;
            }
        };

        control.Disposed += (s, e) =>
        {
            this._settingEventService.RemoveFromDisabledCheck(settingEntry);
        };
    }

    private void SetControlEnabledState(Control control, SettingEntry settingEntry)
    {
        IEnumerable<SettingDisabledComplianceRequisite> compliances = settingEntry.GetComplianceRequisite().Where(c => c is SettingDisabledComplianceRequisite).Select(c => (SettingDisabledComplianceRequisite)c);
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