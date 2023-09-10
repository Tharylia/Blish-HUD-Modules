namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using Models;
using MonoGame.Extended.BitmapFonts;
using Shared.Controls;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ManageReminderTimesView : BaseView
{
    private Event _ev;
    private readonly bool _showKeepCustomizedQuestion;
    private readonly List<TimeSpan> _reminderTimes = new List<TimeSpan>();

    public ManageReminderTimesView(Event ev, bool showKeepCustomizedQuestion, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService)
    {
        this._ev = ev;
        this._showKeepCustomizedQuestion = showKeepCustomizedQuestion;
        if (this._ev.ReminderTimes != null)
        {
            this._reminderTimes.AddRange(this._ev.ReminderTimes);
        }
    }

    public event EventHandler<(Event Event, List<TimeSpan> ReminderTimes, bool KeepCustomized)> SaveClicked;
    public event EventHandler CancelClicked;

    protected override void InternalBuild(Panel parent)
    {
        FlowPanel listPanel = new FlowPanel
        {
            Parent = parent,
            Location = new Point(20, 20),
            Width = parent.ContentRegion.Width - (20 * 2),
            Height = parent.ContentRegion.Height - (20 * 4),
            OuterControlPadding = new Vector2(20, 20),
            ControlPadding = new Vector2(0, 5),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true,
            ShowBorder = true
        };

        this.RenderTimes(listPanel);

        Panel keepCustomizedGroup = null;
        Checkbox keepCustomizedCheckBox = null;
        if (this._showKeepCustomizedQuestion)
        {
            keepCustomizedGroup = new Panel()
            {
                Parent = parent,
                Top = listPanel.Bottom + 5,
                Left = listPanel.Left,
                Width = listPanel.Width,
            };
            var keepCustomizedLabel = this.RenderLabel(keepCustomizedGroup, "Keep Customized:").TitleLabel;
            keepCustomizedLabel.AutoSizeWidth = true;
            keepCustomizedCheckBox = this.RenderCheckbox(keepCustomizedGroup, new Point(keepCustomizedLabel.Right + 5, keepCustomizedLabel.Top), false);
        }

        FlowPanel buttonGroup = new FlowPanel
        {
            Parent = parent,
            Top = (keepCustomizedGroup ?? listPanel).Bottom + 5,
            Left = listPanel.Left,
            Width = listPanel.Width,
            FlowDirection = ControlFlowDirection.SingleRightToLeft
        };

        this.RenderButton(buttonGroup, this.TranslationService.GetTranslation("manageReminderTimesView-btn-cancel", "Cancel"), () =>
        {
            this.CancelClicked?.Invoke(this, EventArgs.Empty);
        });

        this.RenderButton(buttonGroup, this.TranslationService.GetTranslation("manageReminderTimesView-btn-save", "Save"), () =>
        {
            this.SaveClicked?.Invoke(this, (this._ev, this._reminderTimes, keepCustomizedCheckBox?.Checked ?? false));
        });
    }

    private void RenderTimes(Panel parent)
    {
        parent.ClearChildren();

        Panel lastTimeSection = null;
        foreach (TimeSpan reminderTime in this._reminderTimes)
        {
            lastTimeSection = this.AddTimeSection(parent, reminderTime, this._reminderTimes.Count == 1);
        }

        int x = lastTimeSection?.Children.LastOrDefault()?.Left ?? 0;

        Panel addButtonPanel = new Panel
        {
            Parent = parent,
            Width = x + 120,
            HeightSizingMode = SizingMode.AutoSize
        };

        Button addButton = this.RenderButton(addButtonPanel, this.TranslationService.GetTranslation("manageReminderTimesView-btn-add", "Add"), () =>
        {
            this._reminderTimes.Add(TimeSpan.Zero);
            this.RenderTimes(parent);
        });
        addButton.Left = x;
        addButton.Width = 120;
        addButton.Icon = this.IconService.GetIcon("1444520.png");
        addButton.ResizeIcon = false;
    }

    private Panel AddTimeSection(Panel parent, TimeSpan time, bool disableRemove)
    {
        Panel timeSectionPanel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize
        };

        Dropdown<string> hours = new Dropdown<string>
        {
            Parent = timeSectionPanel,
            Location = new Point(0, 0),
            Width = 75,
            BasicTooltipText = "Hours",
            PanelHeight = 400
        };

        foreach (string valueToAdd in Enumerable.Range(0, 24).Select(h => h.ToString()))
        {
            hours.Items.Add(valueToAdd);
        }

        hours.SelectedItem = time.Hours.ToString();
        hours.ValueChanged += (s, e) =>
        {
            try
            {
                TimeSpan newTime = new TimeSpan(int.Parse(e.NewValue), time.Minutes, time.Seconds);
                int index = this._reminderTimes.IndexOf(time);
                this._reminderTimes[index] = newTime;
                time = newTime;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        };

        Dropdown<string> minutes = new Dropdown<string>
        {
            Parent = timeSectionPanel,
            Location = new Point(hours.Right + 5, 0),
            Width = 75,
            BasicTooltipText = "Minutes",
            PanelHeight = 400
        };

        foreach (string valueToAdd in Enumerable.Range(0, 60).Select(h => h.ToString()))
        {
            minutes.Items.Add(valueToAdd);
        }

        minutes.SelectedItem = time.Minutes.ToString();
        minutes.ValueChanged += (s, e) =>
        {
            try
            {
                TimeSpan newTime = new TimeSpan(time.Hours, int.Parse(e.NewValue), time.Seconds);
                int index = this._reminderTimes.IndexOf(time);
                this._reminderTimes[index] = newTime;
                time = newTime;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        };

        Dropdown<string> seconds = new Dropdown<string>
        {
            Parent = timeSectionPanel,
            Location = new Point(minutes.Right + 5, 0),
            Width = 75,
            BasicTooltipText = "Seconds",
            PanelHeight = 400
        };

        foreach (string valueToAdd in Enumerable.Range(0, 60).Select(h => h.ToString()))
        {
            seconds.Items.Add(valueToAdd);
        }

        seconds.SelectedItem = time.Seconds.ToString();
        seconds.ValueChanged += (s, e) =>
        {
            try
            {
                TimeSpan newTime = new TimeSpan(time.Hours, time.Minutes, int.Parse(e.NewValue));
                int index = this._reminderTimes.IndexOf(time);
                this._reminderTimes[index] = newTime;
                time = newTime;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        };

        Button removeButton = this.RenderButton(timeSectionPanel, this.TranslationService.GetTranslation("manageReminderTimesView-btn-remove", "Remove"), () =>
        {
            this._reminderTimes.Remove(time);
            this.RenderTimes(parent);
        }, () => disableRemove);

        removeButton.Left = seconds.Right + 10;
        removeButton.Width = 120;
        removeButton.Icon = this.IconService.GetIcon("1444524.png");
        removeButton.ResizeIcon = false;

        return timeSectionPanel;
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }

    protected override void Unload()
    {
        base.Unload();

        this._ev = null;
    }
}