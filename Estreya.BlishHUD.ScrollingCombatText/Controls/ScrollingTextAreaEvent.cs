namespace Estreya.BlishHUD.ScrollingCombatText.Controls;

using Blish_HUD.ArcDps.Models;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Utils;
using Glide;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ScrollingTextAreaEvent : RenderTargetControl
{
    private const int IMAGE_SIZE = 32;
    private readonly BitmapFont _font;
    private readonly Shared.Models.ArcDPS.CombatEvent _combatEvent;

    private Tween _animationFadeInOut;
    private Tween _animationMove;

    public double Time { get; set; } = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;

    public Color TextColor { get; set; } = Color.White;

    public ScrollingTextAreaEvent(Shared.Models.ArcDPS.CombatEvent combatEvent, BitmapFont font)
    {
        this._combatEvent = combatEvent;
        this._font = font;
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var imageRectangle = new Rectangle(0, 0, Microsoft.Xna.Framework.MathHelper.Clamp(IMAGE_SIZE, 0, this.Width), Microsoft.Xna.Framework.MathHelper.Clamp(IMAGE_SIZE, 0, this.Height));
        if (_combatEvent.Skill?.IconTexture != null)
        {
            spriteBatch.Draw(_combatEvent.Skill?.IconTexture, imageRectangle, Color.White);
        }


        var text = this.ToString();

        var textSize = this._font.MeasureString(text);

        int maxWidth = this.Width - (imageRectangle.Right + 10);
        var textRectangle = new RectangleF(imageRectangle.Right + 10, 0,
            (int)Microsoft.Xna.Framework.MathHelper.Clamp(textSize.Width, 0, maxWidth),
            this.Height);// (int)Microsoft.Xna.Framework.MathHelper.Clamp(textSize.Height, 0, this.Height));
        spriteBatch.DrawString(text, this._font, textRectangle, this.TextColor, verticalAlignment: VerticalAlignment.Middle);
    }

    protected override void DisposeControl()
    {
        if (this._animationMove != null)
        {
            this._animationMove.Cancel();
        }

        if (this._animationFadeInOut != null)
        {
            this._animationFadeInOut.Cancel();
        }

        if (this._combatEvent != null)
        {
            this._combatEvent.Dispose();
        }
    }

    public override string ToString()
    {
        var value = 0;

        if (this._combatEvent?.Ev != null)
        {
            value = this._combatEvent.Ev.Buff ? this._combatEvent.Ev.BuffDmg : this._combatEvent.Ev.Value;
        }

        return $"{_combatEvent.Skill?.Name ?? _combatEvent.Type.Humanize()} ({_combatEvent.Type.Humanize()}): {value}";
    }
}
