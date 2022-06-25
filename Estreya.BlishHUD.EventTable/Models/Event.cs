namespace Estreya.BlishHUD.EventTable.Models
{
    using Blish_HUD;
    using Blish_HUD._Extensions;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Resources;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.EventTable.UI.Views.Edit;
    using Estreya.BlishHUD.EventTable.Utils;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using MonoGame.Extended.BitmapFonts;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    [Serializable]
    public class Event
    {
        private static readonly Logger Logger = Logger.GetLogger<Event>();

        private readonly TimeSpan updateInterval = TimeSpan.FromMinutes(15);
        private double timeSinceUpdate = 0;
        private bool _editing;

        public event EventHandler Edited;

        [Description("Specifies the key of the event. Should be unique for a event category. Avoid changing it, as it resets saved settings and states.")]
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// The name of the event.
        /// <br/>
        /// Will get overridden with the localized event name if available.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("offset"), JsonConverter(typeof(Json.TimeSpanJsonConverter), "dd\\.hh\\:mm", new string[] { "dd\\.hh\\:mm", "hh\\:mm" })]
        public TimeSpan Offset { get; set; }

        [JsonProperty("repeat"), JsonConverter(typeof(Json.TimeSpanJsonConverter), "dd\\.hh\\:mm", new string[] { "dd\\.hh\\:mm", "hh\\:mm" })]
        public TimeSpan Repeat { get; set; }

        [JsonProperty("startingDate"), JsonConverter(typeof(Json.DateJsonConverter))]
        public DateTime? StartingDate { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("waypoint")]
        public string Waypoint { get; set; }

        [JsonProperty("wiki")]
        public string Wiki { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonIgnore]
        private string _backgroundColorCode;

        [JsonProperty("color")]
        public string BackgroundColorCode
        {
            get => this._backgroundColorCode;
            set
            {
                this._backgroundColorCode = value;
                this._backgroundColor = null;
            }
        }

        [JsonProperty("apiType")]
        public APICodeType APICodeType { get; set; }

        [JsonProperty("api")]
        public string APICode { get; set; }

        [JsonIgnore]
        internal bool Filler { get; set; }

        [JsonIgnore]
        internal EventCategory EventCategory { get; set; }

        [JsonIgnore]
        private Tooltip _tooltip;

        [JsonIgnore]
        private ContextMenuStrip _contextMenuStrip;

        [JsonIgnore]
        private Tooltip Tooltip
        {
            get
            {
                if (this._tooltip == null)
                {
                    this._tooltip = new Tooltip(new UI.Views.TooltipView(this.Name, $"{this.Location}", this.Icon));
                }

                return this._tooltip;
            }
        }

        [JsonIgnore]
        private string _settingKey;

        [JsonIgnore]
        public string SettingKey
        {
            get
            {
                if (this._settingKey == null)
                {
                    this._settingKey = $"{this.EventCategory.Key}-{this.Key ?? this.Name}";
                }

                return this._settingKey;
            }
        }

        [JsonIgnore]
        private ContextMenuStrip ContextMenuStrip
        {
            get
            {
                if (this._contextMenuStrip == null)
                {
                    this._contextMenuStrip = new ContextMenuStrip(this.GetContextMenu);
                }

                return this._contextMenuStrip;
            }
        }

        private IEnumerable<ContextMenuStripItem> GetContextMenu()
        {
            ContextMenuStripItem copyWaypoint = new ContextMenuStripItem
            {
                Text = Strings.Event_CopyWaypoint,
                BasicTooltipText = "Copies the waypoint to the clipboard."
            };
            copyWaypoint.Click += (s, e) => this.CopyWaypoint();
            yield return copyWaypoint;

            ContextMenuStripItem openWiki = new ContextMenuStripItem
            {
                Text = Strings.Event_OpenWiki,
                BasicTooltipText = "Open the wiki in the default browser."
            };
            openWiki.Click += (s, e) => this.OpenWiki();
            yield return openWiki;

            #region Hide
            Blish_HUD.Controls.ContextMenuStrip hideMenuStrip = new ContextMenuStrip(() =>
            {
                List<ContextMenuStripItem> items = new List<ContextMenuStripItem>();

                ContextMenuStripItem hideCategory = new ContextMenuStripItem
                {
                    Text = Strings.Event_HideCategory,
                    BasicTooltipText = "Hides the event category until reset."
                };
                hideCategory.Click += (s, e) => this.HideCategory();
                items.Add(hideCategory);

                ContextMenuStripItem hideEvent = new ContextMenuStripItem
                {
                    Text = Strings.Event_HideEvent,
                    BasicTooltipText = "Hides the event until reset."
                };
                hideEvent.Click += (s, e) => this.Hide();
                items.Add(hideEvent);

                return items;
            });

            ContextMenuStripItem hideItem = new ContextMenuStripItem
            {
                Text = "Hide",
                Submenu = hideMenuStrip,
                BasicTooltipText = "Adds options for hiding events."
            };
            yield return hideItem;
            #endregion

            #region Finish
            Blish_HUD.Controls.ContextMenuStrip finishMenuStrip = new ContextMenuStrip(() =>
            {
                List<ContextMenuStripItem> items = new List<ContextMenuStripItem>();

                ContextMenuStripItem finishCategory = new ContextMenuStripItem
                {
                    Text = "Finish Category",
                    BasicTooltipText = "Completes the event category until reset. Click again to reset."
                };
                finishCategory.Click += (s, e) =>
                {
                    if (!this.EventCategory?.IsFinished() ?? false)
                    {
                        this.EventCategory?.Finish();
                    }
                    else
                    {
                        this.EventCategory?.Unfinish();
                    }
                };
                items.Add(finishCategory);

                ContextMenuStripItem finishEvent = new ContextMenuStripItem
                {
                    Text = "Finish Event",
                    BasicTooltipText = "Completes the event until reset. Click again to reset."
                };
                finishEvent.LeftMouseButtonPressed += (s, e) =>
                {
                    if (!this.IsFinished())
                    {
                        this.Finish();
                    }
                    else
                    {
                        this.Unfinish();
                    }
                };
                items.Add(finishEvent);

                return items;
            });
            ContextMenuStripItem finishItem = new ContextMenuStripItem
            {
                Text = "Finish",
                Submenu = finishMenuStrip,
                BasicTooltipText = "Adds options for finishing events."
            };

            yield return finishItem;
            #endregion

            ContextMenuStripItem edit = new ContextMenuStripItem
            {
                Text = "Edit",
                BasicTooltipText = "Opens the edit window."
            };
            edit.Click += (s, e) => this.Edit();

            yield return edit;

            ContextMenuStripItem disable = new ContextMenuStripItem
            {
                Text = Strings.Event_Disable,
                BasicTooltipText = "Disables the event completely."
            };
            disable.Click += (s, e) => this.Disable();

            yield return disable;
        }

        [JsonIgnore]
        private Color? _backgroundColor;

        [JsonIgnore]
        public Color BackgroundColor
        {
            get
            {
                if (this._backgroundColor == null)
                {
                    if (!this.Filler)
                    {
                        try
                        {
                            System.Drawing.Color colorFromEvent = string.IsNullOrWhiteSpace(this.BackgroundColorCode) ? System.Drawing.Color.White : System.Drawing.ColorTranslator.FromHtml(this.BackgroundColorCode);
                            this._backgroundColor = new Color(colorFromEvent.R, colorFromEvent.G, colorFromEvent.B);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Failed generating background color:");
                            this._backgroundColor = Color.Transparent;
                        }
                    }
                }

                return this._backgroundColor ?? Color.Transparent;
            }
        }

        [JsonIgnore]
        private bool? _isDisabled;

        [JsonIgnore]
        public bool IsDisabled
        {
            get
            {
                if (this._isDisabled == null)
                {
                    if (this.Filler)
                    {
                        this._isDisabled = false;
                    }

                    IEnumerable<SettingEntry<bool>> eventSetting = EventTableModule.ModuleInstance.ModuleSettings.AllEvents.Where(e => e.EntryKey.ToLowerInvariant() == this.SettingKey.ToLowerInvariant());
                    if (eventSetting.Any())
                    {
                        bool enabled = eventSetting.First().Value && !EventTableModule.ModuleInstance.EventState.Contains(this.SettingKey, EventState.EventStates.Hidden);

                        this._isDisabled = !enabled;
                    }

                    if (!this._isDisabled.HasValue)
                    {
                        this._isDisabled = false;
                    }
                }

                return this._isDisabled.Value;
            }
        }

        [JsonIgnore]
        private int _lastYPosition = 0;

        [JsonIgnore]
        public List<DateTime> Occurences { get; private set; } = new List<DateTime>();

        [JsonProperty("markers")]
        public List<EventPhaseMarker> EventPhaseMarkers { get; set; } = new List<EventPhaseMarker>();

        public Event()
        {
            this.timeSinceUpdate = this.updateInterval.TotalMilliseconds;
        }

        public bool Draw(SpriteBatch spriteBatch, Rectangle bounds, Texture2D baseTexture, int y, double pixelPerMinute, DateTime now, DateTime min, DateTime max, BitmapFont font)
        {
            List<DateTime> occurences = new List<DateTime>();
            lock (this.Occurences)
            {
                occurences.AddRange(this.Occurences.Where(oc => (oc >= min || oc.AddMinutes(this.Duration) >= min) && oc <= max));
            }

            this._lastYPosition = y;

            foreach (DateTime eventStart in occurences)
            {
                float width = (float)this.GetWidth(eventStart, min, bounds, pixelPerMinute);
                if (width <= 0)
                {
                    // Why would it be negativ anyway?
                    continue;
                }

                float originalX = (float)this.GetXPosition(eventStart, min, pixelPerMinute);
                float x = Math.Max(originalX, 0);

                bool running = eventStart <= now && eventStart.AddMinutes(this.Duration) > now;

                #region Draw Event Rectangle

                RectangleF eventTexturePosition = new RectangleF(x, y, width, EventTableModule.ModuleInstance.EventHeight);
                bool drawBorder = !this.Filler && EventTableModule.ModuleInstance.ModuleSettings.DrawEventBorder.Value;

                spriteBatch.DrawRectangle(baseTexture, eventTexturePosition, this.BackgroundColor * EventTableModule.ModuleInstance.ModuleSettings.Opacity.Value, drawBorder ? 1 : 0, Color.Black);

                #endregion

                Color textColor = Color.Black;
                if (this.Filler)
                {
                    if (EventTableModule.ModuleInstance.ModuleSettings.FillerTextColor.Value != null && EventTableModule.ModuleInstance.ModuleSettings.FillerTextColor.Value?.Id != 1)
                    {
                        textColor = EventTableModule.ModuleInstance.ModuleSettings.FillerTextColor.Value.Cloth.ToXnaColor();
                    }
                }
                else
                {
                    if (EventTableModule.ModuleInstance.ModuleSettings.TextColor.Value != null && EventTableModule.ModuleInstance.ModuleSettings.TextColor.Value?.Id != 1)
                    {
                        textColor = EventTableModule.ModuleInstance.ModuleSettings.TextColor.Value.Cloth.ToXnaColor();
                    }
                }

                #region Draw Event Name

                RectangleF eventTextPosition = Rectangle.Empty;
                if (!string.IsNullOrWhiteSpace(this.Name) && (!this.Filler || (this.Filler && EventTableModule.ModuleInstance.ModuleSettings.UseFillerEventNames.Value)))
                {
                    string eventName = this.GetLongestEventName(eventTexturePosition.Width, font);
                    float eventTextWidth = this.MeasureStringWidth(eventName, font);
                    eventTextPosition = new RectangleF(eventTexturePosition.X + 5, eventTexturePosition.Y + 5, eventTextWidth, eventTexturePosition.Height - 10);

                    spriteBatch.DrawString(eventName, font, eventTextPosition, textColor);
                }

                #endregion

                #region Draw Event Remaining Time
                if (running)
                {
                    DateTime end = eventStart.AddMinutes(this.Duration);
                    TimeSpan timeRemaining = end.Subtract(now);
                    string timeRemainingString = this.FormatTime(timeRemaining);// timeRemaining.Hours > 0 ? timeRemaining.ToString("hh\\:mm\\:ss") : timeRemaining.ToString("mm\\:ss");
                    float timeRemainingWidth = this.MeasureStringWidth(timeRemainingString, font);
                    float timeRemainingX = eventTexturePosition.X + ((eventTexturePosition.Width / 2) - (timeRemainingWidth / 2));
                    if (timeRemainingX < eventTextPosition.X + eventTextPosition.Width)
                    {
                        timeRemainingX = eventTextPosition.X + eventTextPosition.Width + 10;
                    }

                    RectangleF eventTimeRemainingPosition = new RectangleF(timeRemainingX, eventTexturePosition.Y + 5, timeRemainingWidth, eventTexturePosition.Height - 10);

                    if (eventTimeRemainingPosition.X + eventTimeRemainingPosition.Width <= eventTexturePosition.X + eventTexturePosition.Width)
                    {
                        // Only draw if it fits in event bounds
                        spriteBatch.DrawString(timeRemainingString, font, eventTimeRemainingPosition, textColor);
                    }
                }
                #endregion

                #region Draw Phase Markers

                if (running && this.EventPhaseMarkers != null)
                {
                    int markerWidth = 2;

                    foreach (EventPhaseMarker marker in this.EventPhaseMarkers)
                    {
                        float xPosition = originalX + (marker.Time * (float)pixelPerMinute);

                        xPosition = Math.Min(xPosition, eventTexturePosition.Right - markerWidth);

                        if (xPosition < 0)
                        {
                            continue; // Marker not visible
                        }

                        RectangleF markerRectangle = new RectangleF(xPosition, eventTexturePosition.Y, markerWidth, eventTexturePosition.Height);

                        spriteBatch.DrawRectangle(baseTexture, markerRectangle, marker.Color);
                    }
                }

                #endregion

                #region Draw Cross out

                if ((this.EventCategory?.IsFinished() ?? false) || this.IsFinished())
                {
                    spriteBatch.DrawCrossOut(baseTexture, eventTexturePosition, Color.Red);
                }
                #endregion

            }

            return occurences.Any();
        }

        private void UpdateTooltip(string description)
        {
            this._tooltip = new Tooltip(new UI.Views.TooltipView(this.Name, description, this.Icon));
        }

        private string FormatTime(TimeSpan ts)
        {

            if (ts.Days > 0)
            {
                return ts.ToString("dd\\.hh\\:mm\\:ss");
            }
            else if (ts.Hours > 0)
            {
                return ts.ToString("hh\\:mm\\:ss");
            }
            else
            {
                return ts.ToString("mm\\:ss");
            }
        }

        private string FormatTime(DateTime dateTime)
        {
            return this.FormatTime(dateTime.TimeOfDay);
        }

        private string GetLongestEventName(float maxSize, BitmapFont font)
        {
            float size = this.MeasureStringWidth(this.Name, font);

            if (size <= maxSize)
            {
                return this.Name;
            }

            for (int i = 0; i < this.Name.Length; i++)
            {
                string name = this.Name.Substring(0, this.Name.Length - i);
                size = this.MeasureStringWidth(name, font);

                if (size <= maxSize)
                {
                    return name;
                }
            }

            return "...";
        }

        private float MeasureStringWidth(string text, BitmapFont font)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            return font.MeasureString(text).Width + 10; // TODO: Why is +10 needed?
        }

        public void CopyWaypoint()
        {
            if (!string.IsNullOrWhiteSpace(this.Waypoint))
            {
                _ = ClipboardUtil.WindowsClipboardService.SetTextAsync(this.Waypoint);
                Controls.ScreenNotification.ShowNotification(new[] { $"{this.Name}", Strings.Event_WaypointCopied });
                //ScreenNotification.ShowNotification($"Waypoint copied to clipboard!");
                //ScreenNotification.ShowNotification($"{this.Name}");
            }
            else
            {
                Controls.ScreenNotification.ShowNotification(new[] { $"{this.Name}", Strings.Event_NoWaypointFound });
                //ScreenNotification.ShowNotification($"No Waypoint found!");
                //ScreenNotification.ShowNotification($"{this.Name}");
            }
        }

        public void OpenWiki()
        {
            if (!string.IsNullOrWhiteSpace(this.Wiki))
            {
                _ = Process.Start(this.Wiki);
            }
        }

        private List<DateTime> GetStartOccurences(DateTime now, DateTime max, DateTime min)
        {
            List<DateTime> startOccurences = new List<DateTime>();

            DateTime zero = this.StartingDate ?? new DateTime(min.Year, min.Month, min.Day, 0, 0, 0).AddDays(this.Repeat.TotalMinutes == 0 ? 0 : -1);

            TimeSpan offset = this.Offset.Add(TimeZone.CurrentTimeZone.GetUtcOffset(now));

            DateTime eventStart = zero.Add(offset);

            while (eventStart < max)
            {
                bool startAfterMin = eventStart > min;
                bool startBeforeMax = eventStart < max;
                bool endAfterMin = eventStart.AddMinutes(this.Duration) > min;

                bool inRange = (startAfterMin || endAfterMin) && startBeforeMax;

                if (inRange)
                {
                    startOccurences.Add(eventStart);
                }

                eventStart = this.Repeat.TotalMinutes == 0 ? eventStart.Add(TimeSpan.FromDays(1)) : eventStart.Add(this.Repeat);
            }

            return startOccurences;
        }

        private double GetXPosition(DateTime start, DateTime min, double pixelPerMinute)
        {
            double minutesSinceMin = start.Subtract(min).TotalMinutes;
            return minutesSinceMin * pixelPerMinute;
        }

        private double GetWidth(DateTime eventOccurence, DateTime min, Rectangle bounds, double pixelPerMinute)
        {
            double eventWidth = this.Duration * pixelPerMinute;

            double x = this.GetXPosition(eventOccurence, min, pixelPerMinute);

            if (x < 0)
            {
                eventWidth -= Math.Abs(x);
            }

            // Only draw event until end of form
            if ((x > 0 ? x : 0) + eventWidth > bounds.Width)
            {
                eventWidth = bounds.Width - (x > 0 ? x : 0);
            }

            //eventWidth = Math.Min(eventWidth, bounds.Width/* - x*/);

            return eventWidth;
        }

        public bool IsHovered(DateTime min, Rectangle bounds, Point relativeMousePosition, double pixelPerMinute)
        {
            if (this.IsDisabled)
            {
                return false;
            }

            List<DateTime> occurences = this.Occurences;

            foreach (DateTime occurence in occurences)
            {
                double x = this.GetXPosition(occurence, min, pixelPerMinute);
                double width = this.GetWidth(occurence, min, bounds, pixelPerMinute);

                x = Math.Max(x, 0);

                bool hovered = (relativeMousePosition.X >= x && relativeMousePosition.X < x + width) && (relativeMousePosition.Y >= this._lastYPosition && relativeMousePosition.Y < this._lastYPosition + EventTableModule.ModuleInstance.EventHeight);

                if (hovered)
                {
                    return true;
                }
            }

            return false;
        }

        public void HandleClick(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (this.Filler)
            {
                return; // Currently don't do anything when filler
            }

            if (e.EventType == Blish_HUD.Input.MouseEventType.LeftMouseButtonPressed && EventTableModule.ModuleInstance.ModuleSettings.HandleLeftClick.Value)
            {
                if (EventTableModule.ModuleInstance.ModuleSettings.LeftClickAction.Value == LeftClickAction.CopyWaypoint)
                {
                    this.CopyWaypoint();
                }
                else if (EventTableModule.ModuleInstance.ModuleSettings.LeftClickAction.Value == LeftClickAction.NavigateToWaypoint)
                {
                    _ = Task.Run(async () =>
                    {
                        if (EventTableModule.ModuleInstance.PointOfInterestState.Loading)
                        {
                            Controls.ScreenNotification.ShowNotification("Point of Interest State is currently loading...", ScreenNotification.NotificationType.Error);
                        }
                        else
                        {
                            PointOfInterest pointOfInterest = EventTableModule.ModuleInstance.PointOfInterestState.GetPointOfInterest(this.Waypoint);

                            if (pointOfInterest != null)
                            {
                                bool navigationSuccess = await EventTableModule.ModuleInstance.MapNavigationUtil.NavigateToPosition(pointOfInterest, EventTableModule.ModuleInstance.ModuleSettings.DirectlyTeleportToWaypoint.Value);
                                if (!navigationSuccess)
                                {
                                    Controls.ScreenNotification.ShowNotification("Navigation failed.", ScreenNotification.NotificationType.Error);
                                }
                            }
                            else
                            {
                                Controls.ScreenNotification.ShowNotification(Strings.Event_NoWaypointFound, ScreenNotification.NotificationType.Error);
                            }
                        }
                    });
                }
            }
            else if (e.EventType == Blish_HUD.Input.MouseEventType.RightMouseButtonPressed && EventTableModule.ModuleInstance.ModuleSettings.ShowContextMenuOnClick.Value)
            {
                int topPos = e.MousePosition.Y + this.ContextMenuStrip.Height > GameService.Graphics.SpriteScreen.Height
                                ? -this.ContextMenuStrip.Height
                                : 0;

                int leftPos = e.MousePosition.X + this.ContextMenuStrip.Width < GameService.Graphics.SpriteScreen.Width
                                  ? 0
                                  : -this.ContextMenuStrip.Width;

                Point menuPosition = e.MousePosition + new Point(leftPos, topPos);
                this.ContextMenuStrip.Show(menuPosition);
            }
        }

        public void HandleHover(object _, Input.MouseEventArgs e, double pixelPerMinute)
        {
            if (this.Filler)
            {
                return; // Currently don't do anything when filler
            }

            List<DateTime> occurences = this.Occurences;
            IEnumerable<DateTime> hoveredOccurences = occurences.Where(eo =>
            {
                double xStart = this.GetXPosition(eo, EventTableModule.ModuleInstance.EventTimeMin, pixelPerMinute);
                double xEnd = xStart + this.Duration * pixelPerMinute;
                return e.Position.X > xStart && e.Position.X < xEnd;
            });

            if (!this.Tooltip.Visible)
            {
                Debug.WriteLine($"Show Tooltip for Event: {this.Name} | {e.Position}");

                string description = $"{this.Location}";

                if (hoveredOccurences.Any())
                {
                    DateTime hoveredOccurence = hoveredOccurences.First();

                    // Relative
                    bool isPrev = hoveredOccurence.AddMinutes(this.Duration) < EventTableModule.ModuleInstance.DateTimeNow;
                    bool isNext = !isPrev && hoveredOccurence > EventTableModule.ModuleInstance.DateTimeNow;
                    bool isCurrent = !isPrev && !isNext;

                    description = $"{this.Location}{(!string.IsNullOrWhiteSpace(this.Location) ? "\n" : string.Empty)}\n";

                    if (isPrev)
                    {
                        description += $"{Strings.Event_Tooltip_FinishedSince}: {this.FormatTime(EventTableModule.ModuleInstance.DateTimeNow - hoveredOccurence.AddMinutes(this.Duration))}";
                    }
                    else if (isNext)
                    {
                        description += $"{Strings.Event_Tooltip_StartsIn}: {this.FormatTime(hoveredOccurence - EventTableModule.ModuleInstance.DateTimeNow)}";
                    }
                    else if (isCurrent)
                    {
                        description += $"{Strings.Event_Tooltip_Remaining}: {this.FormatTime(hoveredOccurence.AddMinutes(this.Duration) - EventTableModule.ModuleInstance.DateTimeNow)}";
                    }

                    // Absolute
                    description += $" ({Strings.Event_Tooltip_StartsAt}: {this.FormatTime(hoveredOccurence)})";
                }
                else
                {
                    Logger.Warn($"Can't find hovered event: {this.Name} - {string.Join(", ", occurences.Select(o => o.ToString()))}");
                }

                this.UpdateTooltip(description);
                this.Tooltip.Show(0, 0);
            }
        }

        public void HandleNonHover(object _, Input.MouseEventArgs e)
        {
            if (this.Tooltip.Visible)
            {
                Debug.WriteLine($"Hide Tooltip for Event: {this.Name} | {e.Position}");
                this.Tooltip.Hide();
            }
        }

        public void Hide()
        {
            DateTime now = EventTableModule.ModuleInstance.DateTimeNow.ToUniversalTime();
            DateTime until = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, System.DateTimeKind.Utc).AddDays(1);
            EventTableModule.ModuleInstance.EventState.Add(this.SettingKey, until, EventState.EventStates.Hidden);
        }

        private void HideCategory()
        {
            this.EventCategory?.Hide();
        }

        public void Disable()
        {
            // Check with .ToLower() because settings define is case insensitive
            IEnumerable<SettingEntry<bool>> eventSetting = EventTableModule.ModuleInstance.ModuleSettings.AllEvents.Where(e => e.EntryKey.ToLowerInvariant() == this.SettingKey.ToLowerInvariant());
            if (eventSetting.Any())
            {
                eventSetting.First().Value = false;
            }
        }

        public void Finish()
        {
            DateTime now = EventTableModule.ModuleInstance.DateTimeNow.ToUniversalTime();
            DateTime until = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, System.DateTimeKind.Utc).AddDays(1);
            EventTableModule.ModuleInstance.EventState.Add(this.SettingKey, until, EventState.EventStates.Completed);
        }

        public void Unfinish()
        {
            EventTableModule.ModuleInstance.EventState.Remove(this.SettingKey);
        }

        public bool IsFinished()
        {
            if (this.Filler)
            {
                return false;
            }

            bool finished = EventTableModule.ModuleInstance.EventState.Contains(this.SettingKey, EventState.EventStates.Completed);

            return finished;
        }

        private void UpdateEventOccurences(GameTime gameTime)
        {
            if (this.Filler)
            {
                return;
            }

            lock (this.Occurences)
            {
                this.Occurences.Clear();
            }

            DateTime now = EventTableModule.ModuleInstance.DateTimeNow;
            DateTime min = now.AddDays(-2);
            DateTime max = now.AddDays(2);

            List<DateTime> occurences = this.GetStartOccurences(now, max, min);

            lock (this.Occurences)
            {
                this.Occurences.AddRange(occurences);
            }
        }

        public Task LoadAsync()
        {
            EventTableModule.ModuleInstance.EventState.StateAdded += this.EventState_StateAdded;
            EventTableModule.ModuleInstance.EventState.StateRemoved += this.EventState_StateRemoved;

            // Prevent crash on older events.json files
            if (string.IsNullOrWhiteSpace(this.Key))
            {
                this.Key = this.Name;
            }

            if (EventTableModule.ModuleInstance.ModuleSettings.UseEventTranslation.Value)
            {
                this.Name = Strings.ResourceManager.GetString($"event-{this.SettingKey}") ?? this.Name;
            }

            if (string.IsNullOrWhiteSpace(this.Icon))
            {
                this.Icon = this.EventCategory.Icon;
            }

            return Task.CompletedTask;
        }

        private void EventState_StateRemoved(object sender, ValueEventArgs<string> e)
        {
            if (this.SettingKey == e.Value)
            {
                this._isDisabled = null;
            }
        }

        private void EventState_StateAdded(object sender, ValueEventArgs<EventState.VisibleStateInfo> e)
        {
            if (this.SettingKey == e.Value.Key && e.Value.State == EventState.EventStates.Hidden)
            {
                this._isDisabled = null;
            }
        }

        public void ResetCachedStates()
        {
            this._isDisabled = null;
        }

        public void Unload()
        {
            Logger.Debug("Unload event: {0}", this.Key);

            EventTableModule.ModuleInstance.EventState.StateAdded -= this.EventState_StateAdded;
            EventTableModule.ModuleInstance.EventState.StateRemoved -= this.EventState_StateRemoved;

            this._tooltip?.Dispose();
            this._tooltip = null;

            this._contextMenuStrip?.Dispose();
            this._contextMenuStrip = null;

            Logger.Debug("Unloaded event: {0}", this.Key);
        }

        public void Update(GameTime gameTime)
        {
            UpdateUtil.Update(this.UpdateEventOccurences, gameTime, this.updateInterval.TotalMilliseconds, ref this.timeSinceUpdate);
        }

        public void Edit()
        {
            lock (this)
            {
                if (this._editing)
                {
                    return;
                }

                this._editing = true;
            }

            _ = Task.Run(async () =>
            {
                Texture2D backgroundTexture = await EventTableModule.ModuleInstance.IconState.GetIconAsync("controls/window/502049", false);

                Rectangle settingsWindowSize = new Rectangle(35, 50, 913, 691);
                int contentRegionPaddingY = settingsWindowSize.Y - 15;
                int contentRegionPaddingX = settingsWindowSize.X + 46;
                Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 52, settingsWindowSize.Height - contentRegionPaddingY);

                StandardWindow window = new StandardWindow(backgroundTexture, settingsWindowSize, contentRegion)
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    Title = "Edit",
                    Emblem = await EventTableModule.ModuleInstance.IconState.GetIconAsync("156684", false),
                    Subtitle = this.Name,
                    SavesPosition = true,
                    CanClose = false,
                    Id = $"{nameof(EventTableModule)}_f925849b-44bd-4c9f-aaac-76826d93ba6f"
                };

                Event clonedEvent;

                lock (this)
                {
                    clonedEvent = this.Clone();
                }

                EditEventView editView = new EditEventView(clonedEvent);
                editView.SavePressed += (s, e) =>
                {
                    window.Hide();
                    window.Dispose();
                    // Save edited event

                    lock (this)
                    {
                        this.Name = e.Value.Name;
                        this.Offset = e.Value.Offset;
                        this.Repeat = e.Value.Repeat;
                        this.Location = e.Value.Location;
                        this.Waypoint = e.Value.Waypoint;
                        this.Wiki = e.Value.Wiki;
                        this.Duration = e.Value.Duration;
                        this.Icon = e.Value.Icon;
                        this.BackgroundColorCode = e.Value.BackgroundColorCode;
                        this.APICodeType = e.Value.APICodeType;
                        this.APICode = e.Value.APICode;

                        this.EventPhaseMarkers = e.Value.EventPhaseMarkers;

                        // Force update next tick. Don't need to lock since this is locked already.
                        this.timeSinceUpdate = this.updateInterval.TotalMilliseconds;

                        this._editing = false;
                    }

                    this.Edited?.Invoke(this, EventArgs.Empty);
                };
                editView.CancelPressed += (s, e) =>
                {
                    window.Hide();
                    window.Dispose();

                    lock (this)
                    {
                        this._editing = false;
                    }
                };

                window.Show(editView);
            });
        }

        public Event Clone()
        {
            return this.Copy();
        }
    }
}
