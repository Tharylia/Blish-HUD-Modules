namespace Estreya.BlishHUD.ScrollingCombatText.Controls;

using Blish_HUD.Controls;
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
    private BitmapFont _font;
    private Shared.Models.ArcDPS.CombatEvent _combatEvent;

    public double Time { get; set; } = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;

    public Color TextColor { get; set; } = Color.White;

    public ScrollingTextAreaEvent(Shared.Models.ArcDPS.CombatEvent combatEvent, BitmapFont font)
    {
        this._combatEvent = combatEvent;
        this._font = font;
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        Rectangle imageRectangle = new Rectangle(0, 0, Microsoft.Xna.Framework.MathHelper.Clamp(IMAGE_SIZE, 0, this.Width), Microsoft.Xna.Framework.MathHelper.Clamp(IMAGE_SIZE, 0, this.Height));
        if (this._combatEvent?.Skill?.IconTexture != null)
        {
            spriteBatch.Draw(this._combatEvent.Skill?.IconTexture, imageRectangle, Color.White);
        }

        if (this._font != null)
        {
            string text = this.ToString();

            int x = imageRectangle.Right + 10;
            int maxWidth = this.Width - x;

            RectangleF textRectangle = new RectangleF(x, 0,
                maxWidth,
                this.Height);

            spriteBatch.DrawString(text, this._font, textRectangle, this.TextColor, verticalAlignment: VerticalAlignment.Middle);
        }
    }

    protected override void DisposeControl()
    {
        this._font = null;
        this._combatEvent?.Dispose();
        this._combatEvent = null;
    }

    public override string ToString()
    {
        if (this._combatEvent == null)
        {
            return string.Empty;
        }

        int value = 0;

        if (this._combatEvent?.Ev != null)
        {
            value = this._combatEvent.Ev.Buff ? this._combatEvent.Ev.BuffDmg : this._combatEvent.Ev.Value;
        }

        string categoryText = this._combatEvent?.Category.Humanize() ?? "Unknown";
        string typeText = this._combatEvent?.Type.Humanize() ?? "Unknown";

        return $"{categoryText}: {this._combatEvent?.Skill?.Name ?? typeText} ({typeText}): {value}";
    }
}
