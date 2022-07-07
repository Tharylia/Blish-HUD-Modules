namespace Estreya.BlishHUD.ScrollingCombatText.Controls;

using Blish_HUD;
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
using System.Threading;

public class ScrollingTextArea : Container
{
    private static readonly Logger Logger = Logger.GetLogger<ScrollingTextArea>();

    private readonly Gw2ApiManager _apiManager;
    private readonly SkillState _skillState;
    private readonly BitmapFont _font;
    private readonly int _eventHeight;

    private readonly SynchronizedCollection<ScrollingTextAreaEvent> _activeEvents = new SynchronizedCollection<ScrollingTextAreaEvent>();

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

        //var rnd = new Random();

        ScrollingTextAreaEvent scrollingTextAreaEvent = new ScrollingTextAreaEvent(combatEvent, this._font)
        {
            Parent = this,
            Width = this.Width,
            Height = this._eventHeight,
            TextColor = Color.Black,
            Opacity = 0f,
            //BackgroundColor = new Color(rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256))
        };

        scrollingTextAreaEvent.Disposed += this.ScrollingTextAreaEvent_Disposed;

        _activeEvents.Add(scrollingTextAreaEvent);

        scrollingTextAreaEvent.Show();
    }

    private float GetNow()
    {
        return (float)DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
    }

    private float GetActualScrollspeed()
    {
        return this.Configuration.ScrollSpeed / 3;
    }

    public override void UpdateContainer(GameTime gameTime)
    {
        ScrollingTextAreaEvent[] activeEvents = null;

        try
        {
            activeEvents = _activeEvents.ToArray();
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "SynchronizedCollection not being thread safe.");
        }

        if (activeEvents == null || activeEvents.Length == 0)
        {
            return;
        }

        var now = this.GetNow();
        var actualScrollspeed = this.GetActualScrollspeed();

        if (activeEvents.Length >= 2)
        {
            var activeEventsReversed = activeEvents.Reverse();
            var newestEvent = activeEventsReversed.ElementAt(0);

            ScrollingTextAreaEvent lastChecked = newestEvent;
            foreach (ScrollingTextAreaEvent activeEvent in activeEventsReversed.Skip(1)) // Skip newest
            {
                if (lastChecked.Bottom > activeEvent.Top)
                {
                    var distance = this.DistanceToTime(now, this.GetActualScrollspeed(), lastChecked.Bottom - activeEvent.Top);
                    //Logger.Debug($"LastChecked.Bottom > ActiveEvent.Top: {lastChecked.Bottom} > {activeEvent.Top} -> Distance: {distance}");
                    activeEvent.Time -= distance;
                }

                lastChecked = activeEvent;
            }
        }

        //foreach (ScrollingTextAreaEvent activeEvent in activeEventsReversed.Skip(1)) // Skip newest
        //{
        //    if (movedOne || activeEvent.Top < newestEvent.Bottom)
        //    {
        //        movedOne = true; // Move all following events as well.
        //        if (activeEvent.Top < lastChecked.Bottom)
        //        {
        //            activeEvent.Time -= this.DistanceToTime(now, this.GetActualScrollspeed(), lastChecked.Bottom - activeEvent.Top);
        //        }
        //    }

        //    lastChecked = activeEvent;
        //}

        foreach (ScrollingTextAreaEvent activeEvent in activeEvents)
        {
            float animatedHeight = (float)(now - activeEvent.Time) * actualScrollspeed;
            float percentage = animatedHeight / this.Height;
            float fadeLength = 0.2f;

            if (percentage > 1)
            {
                activeEvent.Dispose();
                continue;
            }

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
        }

        var activeEventsReversed2 = activeEvents.Reverse();

        if (activeEventsReversed2.Count() > 1)
        {
            ScrollingTextAreaEvent lastChecked = activeEventsReversed2.First();
            foreach (ScrollingTextAreaEvent activeEvent in activeEventsReversed2.Skip(1))
            {
                if (lastChecked.Bottom > activeEvent.Top)
                {
                    //Logger.Info($"Some event still stuck inside each other: {activeEvent.ToString()}");
                }

                lastChecked = activeEvent;
            }
        }
    }

    private float DistanceToTime(float timeNow, float actualScrollspeed, float distance)
    {
        var x = timeNow - (distance - (timeNow * actualScrollspeed)) / -actualScrollspeed;
        return x;
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

        _activeEvents.Remove(scrollingTextAreaEvent);
    }
}
