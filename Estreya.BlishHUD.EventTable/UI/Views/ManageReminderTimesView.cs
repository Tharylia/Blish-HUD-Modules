﻿namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Controls;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;

public class ManageReminderTimesView : BaseView
{
    private Models.Event _ev;
    private List<TimeSpan> _reminderTimes = new List<TimeSpan>();

    public event EventHandler<(Models.Event Event, List<TimeSpan> ReminderTimes)> SaveClicked;
    public event EventHandler CancelClicked;

    public ManageReminderTimesView(Models.Event ev, Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
    {
        this._ev = ev;

        if (this._ev.ReminderTimes != null)
        {
            this._reminderTimes.AddRange(this._ev.ReminderTimes);
        }
    }

    protected override void InternalBuild(Panel parent)
    {
        var listPanel = new FlowPanel()
        {
            Parent = parent,
            Location = new Microsoft.Xna.Framework.Point(20,20),
            Width = parent.ContentRegion.Width - 20 * 2,
            Height = parent.ContentRegion.Height - 20 * 3,
            OuterControlPadding = new Vector2(20,20),
            ControlPadding = new Vector2(0,5),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true,
            ShowBorder = true,
        };

        this.RenderTimes(listPanel);

        var buttonGroup = new FlowPanel()
        {
            Parent = parent,
            Top = listPanel.Bottom + 5,
            Left = listPanel.Left,
            Width = listPanel.Width,
            FlowDirection = ControlFlowDirection.SingleRightToLeft
        };

        this.RenderButton(buttonGroup, "Cancel", () =>
        {
            this.CancelClicked?.Invoke(this, EventArgs.Empty);
        });

        this.RenderButton(buttonGroup, "Save", () =>
        {
            this.SaveClicked?.Invoke(this,(this._ev, this._reminderTimes));
        });
    }

    private void RenderTimes(Panel parent)
    {
        parent.ClearChildren();

        Panel lastTimeSection = null;
        foreach (var reminderTime in this._reminderTimes)
        {
            lastTimeSection = this.AddTimeSection(parent, reminderTime, this._reminderTimes.Count == 1);
        }

        var x = lastTimeSection?.Children.LastOrDefault()?.Left ?? 0;

        var addButtonPanel = new Panel()
        {
            Parent = parent,
            Width = x + 100,
            HeightSizingMode = SizingMode.AutoSize
        };

        var addButton = this.RenderButton(addButtonPanel, "Add", () =>
        {
            this._reminderTimes.Add(TimeSpan.Zero);
            this.RenderTimes(parent);
        });
        addButton.Left = x;
        addButton.Width = 100;
        addButton.Icon = this.IconState.GetIcon("1444520.png");
        addButton.ResizeIcon = false;
    }

    private Panel AddTimeSection(Panel parent, TimeSpan time, bool disableRemove)
    {
        var timeSectionPanel = new Panel()
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
        };

        var hours = new Controls.Dropdown()
        {
            Parent = timeSectionPanel,
            Location = new Point(0, 0),
            Width = 75,
            BasicTooltipText = "Hours",
            PanelHeight = 400
        };

        foreach (var valueToAdd in Enumerable.Range(0, 24).Select(h => h.ToString()))
        {
            hours.Items.Add(valueToAdd);
        }

        hours.SelectedItem = time.Hours.ToString();
        hours.ValueChanged += (s, e) =>
        {
            try
            {
                var newTime = new TimeSpan(int.Parse(e.CurrentValue), time.Minutes, time.Seconds);
                var index = this._reminderTimes.IndexOf(time);
                this._reminderTimes[index] = newTime;
                time = newTime;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        };

        var minutes = new Controls.Dropdown()
        {
            Parent = timeSectionPanel,
            Location = new Point(hours.Right + 5, 0),
            Width = 75,
            BasicTooltipText = "Minutes",
            PanelHeight = 400
        };

        foreach (var valueToAdd in Enumerable.Range(0, 60).Select(h => h.ToString()))
        {
            minutes.Items.Add(valueToAdd);
        }

        minutes.SelectedItem = time.Minutes.ToString();
        minutes.ValueChanged += (s, e) =>
        {
            try
            {
                var newTime = new TimeSpan(time.Hours, int.Parse(e.CurrentValue), time.Seconds);
                var index = this._reminderTimes.IndexOf(time);
                this._reminderTimes[index] = newTime;
                time = newTime;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        };

        var seconds = new Controls.Dropdown()
        {
            Parent = timeSectionPanel,
            Location = new Point(minutes.Right + 5, 0),
            Width = 75,
            BasicTooltipText = "Seconds",
            PanelHeight = 400
        };

        foreach (var valueToAdd in Enumerable.Range(0, 60).Select(h => h.ToString()))
        {
            seconds.Items.Add(valueToAdd);
        }

        seconds.SelectedItem = time.Seconds.ToString();
        seconds.ValueChanged += (s, e) =>
        {
            try
            {
                var newTime = new TimeSpan(time.Hours, time.Minutes , int.Parse(e.CurrentValue));
                var index = this._reminderTimes.IndexOf(time);
                this._reminderTimes[index] = newTime;
                time = newTime;
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        };

        var removeButton = this.RenderButton(timeSectionPanel, "Remove", () =>
        {
            this._reminderTimes.Remove(time);
            this.RenderTimes(parent);
        }, () => disableRemove);

        removeButton.Left = seconds.Right + 10;
        removeButton.Width = 100;
        removeButton.Icon = this.IconState.GetIcon("1444524.png");
        removeButton.ResizeIcon = false;

        return timeSectionPanel;
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);

    protected override void Unload()
    {
        base.Unload();

        this._ev = null;
    }
}