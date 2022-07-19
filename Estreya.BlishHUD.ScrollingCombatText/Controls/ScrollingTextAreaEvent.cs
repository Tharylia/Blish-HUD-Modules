namespace Estreya.BlishHUD.ScrollingCombatText.Controls;

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

public class ScrollingTextAreaEvent : RenderTargetControl
{
    private const int IMAGE_SIZE = 32;

    private Shared.Models.ArcDPS.CombatEvent _combatEvent;
    private CombatEventFormatRule _formatRule;

    private readonly int _textWidth = 0;

    private Rectangle _imageRectangle;
    private Rectangle _textRectangle;

    private BitmapFont _font { get; set; }

    public double Time { get; set; } = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;

    public Color TextColor { get; set; } = Color.White;

    public ScrollingTextAreaEvent(Shared.Models.ArcDPS.CombatEvent combatEvent, CombatEventFormatRule formatRule, BitmapFont font)
    {
        this._combatEvent = combatEvent;
        this._formatRule = formatRule;
        this._font = font;

        this._textWidth = (int)(this._font?.MeasureString(this.ToString()).Width ?? 0);
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (this._imageRectangle == null || this._textRectangle == null)
        {
            this.CalculateLayout();
        }

        if (this._combatEvent?.Skill?.IconTexture != null)
        {
            spriteBatch.Draw(this._combatEvent.Skill?.IconTexture, this._imageRectangle, Color.White);
        }

        if (this._font != null)
        {
            spriteBatch.DrawString(this.ToString(), this._font, this._textRectangle, this.TextColor, verticalAlignment: VerticalAlignment.Middle);
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
        this._combatEvent?.Dispose();
        this._combatEvent = null;
        this._formatRule = null;
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
            return this._formatRule.FormatEvent(_combatEvent);
        }
        catch (Exception)
        {
            return "Unparsable event";
        }
    }
}
