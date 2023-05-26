namespace Estreya.BlishHUD.ScrollingCombatText.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Models;
using MonoGame.Extended.BitmapFonts;
using Shared.Models.ArcDPS;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Color = Microsoft.Xna.Framework.Color;
using RectangleF = MonoGame.Extended.RectangleF;

public class ScrollingTextAreaEvent : IDisposable
{
    private const int IMAGE_SIZE = 32;
    private static readonly Logger Logger = Logger.GetLogger<ScrollingTextAreaEvent>();
    private static readonly Regex _colorRegex = new Regex("<c=(.*?)>", RegexOptions.Compiled);
    private static readonly Regex _colorSplitRegex = new Regex("(<c=.*?>.*?<\\/c>)", RegexOptions.Compiled);
    private static readonly Regex _colorRemoveRegex = new Regex("(<c=.*?>).*?(<\\/c>)", RegexOptions.Compiled);

    private readonly int _textWidth;

    private CombatEvent _combatEvent;
    private CombatEventFormatRule _formatRule;

    private RectangleF _imageRectangle;
    private List<ScrollingTextAreaText> _scrollingTexts;
    private RectangleF _textRectangle;

    public ScrollingTextAreaEvent(CombatEvent combatEvent, CombatEventFormatRule formatRule, BitmapFont font, float maxWidth, float height)
    {
        this._combatEvent = combatEvent;
        this._formatRule = formatRule;
        this._font = font;
        this.Width = maxWidth;
        this.Height = height;

        this._textWidth = (int)(this._font?.MeasureString(this.ToString()).Width ?? 0);
        this.CalculateLayout();
        this.CalculateScrollingTexts();
        this.Width = this._textRectangle.Right;
    }

    private BitmapFont _font { get; set; }

    public double Time { get; set; } = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;

    public Color BaseTextColor { get; set; } = Color.White;

    public float Width { get; }

    public float Height { get; }

    public void Dispose()
    {
        this._font = null;
        //this._combatEvent?.Dispose();
        this._combatEvent = null;
        this._formatRule = null;

        this.Disposed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler Disposed;

    public void Render(SpriteBatch spriteBatch, RectangleF bounds, float opacity)
    {
        if (this._combatEvent?.Skill?.IconTexture != null)
        {
            RectangleF rect = new RectangleF(this._imageRectangle.Position, this._imageRectangle.Size);
            rect.Offset(bounds.X, bounds.Y);
            spriteBatch.Draw(this._combatEvent.Skill?.IconTexture, rect, Color.White * opacity);
        }

        if (this._font != null && this._scrollingTexts != null && this._scrollingTexts.Count > 0)
        {
            foreach (ScrollingTextAreaText scrollingText in this._scrollingTexts)
            {
                RectangleF rect = new RectangleF(scrollingText.Rectangle.Position, scrollingText.Rectangle.Size);
                rect.Offset(bounds.X, bounds.Y);
                spriteBatch.DrawString(scrollingText.Text, this._font, rect, scrollingText.Color * opacity, verticalAlignment: VerticalAlignment.Middle);
            }
        }
    }

    public void CalculateLayout()
    {
        this._imageRectangle = new RectangleF(0, 0, this.Height, this.Height);

        int textWidth = this._textWidth;
        textWidth = MathHelper.Clamp(textWidth, 0, (int)Math.Floor(this.Width - this._imageRectangle.Right));

        float x = this._imageRectangle.Right + 10;
        this._textRectangle = new RectangleF(x, this._imageRectangle.Y, textWidth, this.Height);
    }

    public void CalculateScrollingTexts()
    {
        this._scrollingTexts = new List<ScrollingTextAreaText>();

        if (this._combatEvent == null)
        {
            this._scrollingTexts.Add(new ScrollingTextAreaText
            {
                Text = "Unknown combat event",
                Color = Color.Red,
                Rectangle = this._textRectangle
            });

            return;
        }

        if (this._formatRule == null)
        {
            this._scrollingTexts.Add(new ScrollingTextAreaText
            {
                Text = "Unknown format rule",
                Color = Color.Red,
                Rectangle = this._textRectangle
            });

            return;
        }

        try
        {
            // Can't call ToString() as it will remove color formatting
            string formattedTemplate = this._formatRule.FormatEvent(this._combatEvent);
            string[] formattedTemplateParts = _colorSplitRegex.Split(formattedTemplate).Where(split => !string.IsNullOrEmpty(split)).ToArray();

            foreach (string formattedTemplatePart in formattedTemplateParts)
            {
                string changedPart = formattedTemplatePart;

                Vector2 lastPoint = this._scrollingTexts.Count > 0 ? new Vector2(this._scrollingTexts.Last().Rectangle.Right, 0) : new Vector2(this._textRectangle.X, 0);
                float maxWidth = MathHelper.Clamp(this._textRectangle.Width - lastPoint.X, 0, this._textRectangle.Width);

                bool added = false;

                Match colorMatch = _colorRemoveRegex.Match(changedPart);
                if (colorMatch.Success)
                {
                    changedPart = changedPart.Replace(colorMatch.Groups[1].Value, string.Empty); // Remove <c=..>
                    changedPart = changedPart.Replace(colorMatch.Groups[2].Value, string.Empty); // Remove </c>

                    Match hexColorMatch = _colorRegex.Match(colorMatch.Groups[1].Value);

                    if (hexColorMatch.Success)
                    {
                        System.Drawing.Color hexColor = ColorTranslator.FromHtml(hexColorMatch.Groups[1].Value);

                        this._scrollingTexts.Add(new ScrollingTextAreaText
                        {
                            Text = changedPart,
                            Color = new Color(hexColor.R, hexColor.G, hexColor.B),
                            Rectangle = new RectangleF(lastPoint.X, lastPoint.Y, MathHelper.Clamp((int)this._font.MeasureString(changedPart).Width, 0, maxWidth), this._textRectangle.Height)
                        });

                        added = true;
                    }
                }

                if (!added)
                {
                    this._scrollingTexts.Add(new ScrollingTextAreaText
                    {
                        Text = changedPart,
                        Color = this.BaseTextColor,
                        Rectangle = new RectangleF(lastPoint.X, lastPoint.Y, MathHelper.Clamp((int)this._font.MeasureString(changedPart).Width, 0, maxWidth), this._textRectangle.Height)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed parsing event:");

            this._scrollingTexts.Add(new ScrollingTextAreaText
            {
                Text = "Unparsable event",
                Color = Color.Red,
                Rectangle = this._textRectangle
            });
        }
    }

    public override string ToString()
    {
        if (this._combatEvent == null)
        {
            return "Unknown combat event";
        }

        if (this._formatRule == null)
        {
            return "Unknown format rule";
        }

        try
        {
            string formattedTemplate = this._formatRule.FormatEvent(this._combatEvent);

            foreach (Match match in _colorRemoveRegex.Matches(formattedTemplate))
            {
                formattedTemplate = formattedTemplate.Replace(match.Groups[1].Value, string.Empty); // Remove <c=..>
                formattedTemplate = formattedTemplate.Replace(match.Groups[2].Value, string.Empty); // Remove </c>
            }

            return formattedTemplate;
        }
        catch (Exception)
        {
            return "Unparsable event";
        }
    }

    private struct ScrollingTextAreaText
    {
        public string Text;
        public RectangleF Rectangle;
        public Color Color;
    }
}