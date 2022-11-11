namespace Estreya.BlishHUD.ScrollingCombatText.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.ScrollingCombatText.Models;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Utils;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class ScrollingTextAreaEvent : RenderTargetControl
{
    private static readonly Logger Logger = Logger.GetLogger<ScrollingTextAreaEvent>();

    private const int IMAGE_SIZE = 32;
    private static Regex _colorRegex = new Regex("<c=(.*?)>", RegexOptions.Compiled);
    private static Regex _colorSplitRegex = new Regex("(<c=.*?>.*?<\\/c>)", RegexOptions.Compiled);
    private static Regex _colorRemoveRegex = new Regex("(<c=.*?>).*?(<\\/c>)", RegexOptions.Compiled);

    private Shared.Models.ArcDPS.CombatEvent _combatEvent;
    private CombatEventFormatRule _formatRule;

    private readonly int _textWidth = 0;

    private Rectangle _imageRectangle;
    private Rectangle _textRectangle;
    private List<ScrollingTextAreaText> _scrollingTexts;

    private BitmapFont _font { get; set; }

    public double Time { get; set; } = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;

    public Color BaseTextColor { get; set; } = Color.White;

    public ScrollingTextAreaEvent(Shared.Models.ArcDPS.CombatEvent combatEvent, CombatEventFormatRule formatRule, BitmapFont font)
    {
        this._combatEvent = combatEvent;
        this._formatRule = formatRule;
        this._font = font;

        this._textWidth = (int)(this._font?.MeasureString(this.ToString()).Width ?? 0);
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (this._imageRectangle == null || this._textRectangle == null || this._scrollingTexts == null)
        {
            this.CalculateLayout();
            this.CalculateScrollingTexts();
        }

        if (this._combatEvent?.Skill?.IconTexture != null)
        {
            spriteBatch.Draw(this._combatEvent.Skill?.IconTexture, this._imageRectangle, Color.White);
        }

        if (this._font != null && this._scrollingTexts != null && this._scrollingTexts.Count > 0)
        {
            foreach (var scrollingText in this._scrollingTexts)
            {
                spriteBatch.DrawString(scrollingText.Text, this._font, scrollingText.Rectangle, scrollingText.Color, verticalAlignment: VerticalAlignment.Middle);
            }
        }
    }

    public void CalculateLayout()
    {
        this._imageRectangle = new Rectangle(0, 0, this.Height, this.Height);

        int textWidth = this._textWidth;
        textWidth = MathHelper.Clamp(textWidth, 0, (this.Parent?.Width ?? int.MaxValue) - this.Location.X);

        int x = this._imageRectangle.Right + 10;
        this._textRectangle = new Rectangle(x, 0, textWidth, this.Height);

        this.Width = this._textRectangle.Right;
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.None;
    }

    protected override void InternalDispose()
    {
        this._font = null;
        //this._combatEvent?.Dispose();
        this._combatEvent = null;
        this._formatRule = null;
    }

    public void CalculateScrollingTexts()
    {
        this._scrollingTexts = new List<ScrollingTextAreaText>();

        if (this._combatEvent == null)
        {
            this._scrollingTexts.Add(new ScrollingTextAreaText()
            {
                Text = "Unknown combat event",
                Color = Color.Red,
                Rectangle = this._textRectangle
            });

            return;
        }

        if (this._formatRule == null)
        {
            this._scrollingTexts.Add(new ScrollingTextAreaText()
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
            string formattedTemplate = this._formatRule.FormatEvent(_combatEvent);
            string[] formattedTemplateParts = _colorSplitRegex.Split(formattedTemplate).Where(split => !string.IsNullOrEmpty(split)).ToArray();

            foreach (var formattedTemplatePart in formattedTemplateParts)
            {
                var changedPart = formattedTemplatePart;

                Point lastPoint = this._scrollingTexts.Count > 0 ? new Point(this._scrollingTexts.Last().Rectangle.Right, 0) : new Point(this._textRectangle.X, 0);
                var maxWidth = MathHelper.Clamp(this._textRectangle.Width - lastPoint.X, 0, this._textRectangle.Width);

                bool added = false;

                Match colorMatch = _colorRemoveRegex.Match(changedPart);
                if (colorMatch.Success)
                {
                    changedPart = changedPart.Replace(colorMatch.Groups[1].Value, string.Empty); // Remove <c=..>
                    changedPart = changedPart.Replace(colorMatch.Groups[2].Value, string.Empty); // Remove </c>

                    Match hexColorMatch = _colorRegex.Match(colorMatch.Groups[1].Value);

                    if (hexColorMatch.Success)
                    {
                        System.Drawing.Color hexColor = System.Drawing.ColorTranslator.FromHtml(hexColorMatch.Groups[1].Value);

                        this._scrollingTexts.Add(new ScrollingTextAreaText()
                        {
                            Text = changedPart,
                            Color = new Color(hexColor.R, hexColor.G, hexColor.B),
                            Rectangle = new Rectangle(lastPoint.X, lastPoint.Y, MathHelper.Clamp((int)this._font.MeasureString(changedPart).Width, 0, maxWidth), this._textRectangle.Height)
                        });

                        added = true;
                    }
                }

                if (!added)
                {
                    this._scrollingTexts.Add(new ScrollingTextAreaText()
                    {
                        Text = changedPart,
                        Color = this.BaseTextColor,
                        Rectangle = new Rectangle(lastPoint.X, lastPoint.Y, MathHelper.Clamp((int)this._font.MeasureString(changedPart).Width, 0, maxWidth), this._textRectangle.Height)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed parsing event:");

            this._scrollingTexts.Add(new ScrollingTextAreaText()
            {
                Text = "Unparsable event",
                Color = Color.Red,
                Rectangle = this._textRectangle
            });

            return;
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
            string formattedTemplate = this._formatRule.FormatEvent(_combatEvent);

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
        public Rectangle Rectangle;
        public Color Color;
    }
}
