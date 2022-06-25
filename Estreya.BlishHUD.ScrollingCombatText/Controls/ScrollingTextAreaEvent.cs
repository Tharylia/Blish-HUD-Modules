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
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
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

    public double Time { get; } = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;

    public ScrollingTextAreaEvent(Shared.Models.ArcDPS.CombatEvent combatEvent, BitmapFont font)
    {
        this._combatEvent = combatEvent;
        this._font = font;
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var imageRectangle = new Rectangle(0, 0, Microsoft.Xna.Framework.MathHelper.Clamp(IMAGE_SIZE, 0, this.Width), Microsoft.Xna.Framework.MathHelper.Clamp(IMAGE_SIZE, 0, this.Height));
        if (_combatEvent.SkillTexture != null)
        {
            spriteBatch.Draw(_combatEvent.SkillTexture, imageRectangle, Color.White);
        }

        var text = $"{_combatEvent.Skill?.Name ?? "Unknown"} ({_combatEvent.Type.Humanize()}): {this._combatEvent.Ev?.Value ?? 0}";

        var textRectangle = new Rectangle(imageRectangle.Right + 10, 0, this.Width - (imageRectangle.Right + 10), this.Height);
        spriteBatch.DrawString(text, this._font, textRectangle, Color.Black);
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
    }
}
