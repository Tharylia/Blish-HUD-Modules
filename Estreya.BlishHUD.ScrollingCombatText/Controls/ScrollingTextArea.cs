namespace Estreya.BlishHUD.ScrollingCombatText.Controls;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.ScrollingCombatText.Models;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

public class ScrollingTextArea : Container
{
    private readonly Gw2ApiManager _apiManager;
    private readonly SkillState _skillState;
    private readonly BitmapFont _font;
    private readonly int _eventHeight;

    private readonly AsyncLock _eventLock = new AsyncLock();

    private static readonly Collection<ScrollingTextAreaEvent> _activeEvents = new Collection<ScrollingTextAreaEvent>();

    public ScrollingTextAreaConfiguration Configuration { get; }

    public ScrollingTextArea(ScrollingTextAreaConfiguration configuration, Gw2ApiManager apiManager, SkillState skillState, BitmapFont font)
    {
        this.Configuration = configuration;

        this.Size = this.Configuration.Size;
        this.Location = this.Configuration.Location;

        this._apiManager = apiManager;
        this._skillState = skillState;
        this._font = font;

        this._eventHeight = this.Configuration.EventHeight != -1 ? this.Configuration.EventHeight : this._font.LineHeight;
    }

    public void AddCombatEvent(Shared.Models.ArcDPS.CombatEvent combatEvent)
    {
        if (!this.CheckConfiguration(combatEvent))
        {
            return;
        }

        ScrollingTextAreaEvent scrollingTextAreaEvent = new ScrollingTextAreaEvent(combatEvent, this._font)
        {
            Parent = this,
            Width = this.Width,
            Height = this._eventHeight,
            BackgroundColor = Color.Red,
            Opacity = 0f
        };

        scrollingTextAreaEvent.Disposed += this.ScrollingTextAreaEvent_Disposed;

        using (_eventLock.Lock())
        {
            foreach (ScrollingTextAreaEvent activeEvent in _activeEvents)
            {
                if (activeEvent.Top < scrollingTextAreaEvent.Bottom)
                {
                    var targetTop = activeEvent.Top + this._eventHeight;
                    activeEvent.Top = targetTop;
                }
            }

            Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: Adding event to area '{this.Configuration.Name}'");

            _activeEvents.Add(scrollingTextAreaEvent);
            scrollingTextAreaEvent.Show();
        }
    }

    public override void UpdateContainer(GameTime gameTime)
    {
        List<ScrollingTextAreaEvent> activeEvents = new List<ScrollingTextAreaEvent>();

        using (_eventLock.Lock())
        {
            activeEvents.AddRange(_activeEvents);
        }

        var now = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;

        foreach (ScrollingTextAreaEvent activeEvent in activeEvents)
        {
            float animatedHeight = (float)(now - activeEvent.Time) * this.Configuration.ScrollSpeed;
            float percentage = animatedHeight / this.Height;
            float fadeLength = 0.2f;

            if (percentage > 1)
            {
                activeEvent.Dispose();
                continue;
            }
            //else if (percentage > 1f - fadeLength)
            //{
            //}

            var alpha = 1 - (percentage - 1f + fadeLength) / fadeLength;
            activeEvent.Opacity = alpha;

            Point2 pos = new Point2(0, 0);

            switch (this.Configuration.Curve)
            {
                case ScrollingTextAreaCurve.Left:
                    pos.X += this.Width * (2 * percentage - 1) * (2 * percentage - 1);
                    break;
                case ScrollingTextAreaCurve.Right:
                    pos.X += this.Width * (1 - (2 * percentage - 1) * (2 * percentage - 1));
                    break;
                case ScrollingTextAreaCurve.Straight:
                    break;
                default:
                    break;
            }

            pos.Y += animatedHeight;

            activeEvent.Location = new Point((int)pos.X, (int)pos.Y);

            //_ = activeEvent.Move(new Point((int)pos.X, (int)pos.Y)).OnComplete(() =>
            //{
            //    if (activeEvent.Top > this.Bottom)
            //    {
            //        activeEvent.Hide();
            //    }
            //});
        }

        //Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: Update events in area '{this.Configuration.Name}'");
    }

    private bool CheckConfiguration(Shared.Models.ArcDPS.CombatEvent combatEvent)
    {
        if (this.Configuration.Categories != null && !this.Configuration.Categories.Contains(combatEvent.Category))
        {
            return false;
        }

        if (this.Configuration.Types != null && !this.Configuration.Types.Contains(combatEvent.Type))
        {
            return false;
        }

        return true;
    }

    private void ScrollingTextAreaEvent_Disposed(object sender, EventArgs e)
    {
        ScrollingTextAreaEvent scrollingTextAreaEvent = sender as ScrollingTextAreaEvent;

        scrollingTextAreaEvent.Disposed -= this.ScrollingTextAreaEvent_Disposed;

        Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: Removing event of area '{this.Configuration.Name}'");
        using (_eventLock.Lock())
        {
            Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: Removed event of area '{this.Configuration.Name}'");
            _activeEvents.Remove(scrollingTextAreaEvent);
        }
    }
}
