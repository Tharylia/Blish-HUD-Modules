﻿namespace Estreya.BlishHUD.Shared.Controls
{
    using Blish_HUD.Common.UI.Views;
    using Blish_HUD.Controls;
    using Blish_HUD;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Blish_HUD.Input;
    using Estreya.BlishHUD.Shared.Services;
    using Microsoft.Xna.Framework.Graphics;
    using Blish_HUD.Content;

    public class SmallInteract : Control
    {
        private const int DRAW_WIDTH = 64;
        private const int DRAW_HEIGHT = 64;

        private const float LEFT_OFFSET = 0.62f;
        private const float TOP_OFFSET = 0.58f;

        private const double SUBTLE_DELAY = 0.103d;
        private const double SUBTLE_DAMPER = 0.05d;

        private readonly AsyncTexture2D _interact1 = AsyncTexture2D.FromAssetId(102390);

        private Vector3 _lastPlayerPosition = Vector3.Zero;
        private double _subtleTimer = 0;

        private double _showStart = 0;

        private Color _tint = Color.White;

        public event EventHandler Interacted;

        public SmallInteract()
        {
            this.Visible = false;
            this.Size = new Point(DRAW_WIDTH, DRAW_HEIGHT);

            GameService.Input.Keyboard.KeyPressed += this.Keyboard_KeyPressed;
        }

        private void Keyboard_KeyPressed(object sender, KeyboardEventArgs e)
        {
            if (this.Visible && e.Key == Microsoft.Xna.Framework.Input.Keys.F)
            {
                this.Interacted?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.DoNotBlock | CaptureType.Mouse;
        }

        public void ShowInteract(string interactMessage)
        {
            ShowInteract(new BasicTooltipView(string.Format(interactMessage, $"[{Blish_HUD.Common.Gw2.KeyBindings.Interact.GetBindingDisplayText()}]")));
        }

        public void ShowInteract(string interactMessage, Color tint)
        {
            ShowInteract(new BasicTooltipView(string.Format(interactMessage, $"[{Blish_HUD.Common.Gw2.KeyBindings.Interact.GetBindingDisplayText()}]")), tint);
        }

        public void ShowInteract(ITooltipView tooltipView, Color tint)
        {
            _tint = tint;

            _showStart = GameService.Overlay.CurrentGameTime.TotalGameTime.TotalSeconds;

            this.Tooltip = new Tooltip(tooltipView);
            this.Visible = true;
        }

        public void ShowInteract(ITooltipView tooltipView)
        {
            ShowInteract(tooltipView, Color.FromNonPremultiplied(255, 142, 50, 255));
        }

        protected override void OnClick(MouseEventArgs e)
        {
            base.OnClick(e);

            this.Interacted?.Invoke(this, EventArgs.Empty);
        }

        public override void DoUpdate(GameTime gameTime)
        {
            base.DoUpdate(gameTime);

            if (GameService.Gw2Mumble.PlayerCharacter.Position != _lastPlayerPosition || GameService.Gw2Mumble.PlayerCharacter.IsInCombat)
            {
                _lastPlayerPosition = GameService.Gw2Mumble.PlayerCharacter.Position;

                _subtleTimer = gameTime.TotalGameTime.TotalSeconds;
            }

            if (this.Parent != null)
            {
                this.Location = new Point((int)(this.Parent.Width * 0.5f /*_packState.UserResourceStates.Advanced.InteractGearXOffset*/), (int)(this.Parent.Height * 0.5f/*_packState.UserResourceStates.Advanced.InteractGearYOffset*/));
            }
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (!GameService.GameIntegration.Gw2Instance.IsInGame) return;

            float baseOpacity = 0.4f;
            double tCTS = Math.Max(GameService.Overlay.CurrentGameTime.TotalGameTime.TotalSeconds - _subtleTimer, SUBTLE_DAMPER);
            float opacity = (this.MouseOver ? baseOpacity : 0.3f) + (float)Math.Min((tCTS / SUBTLE_DELAY - SUBTLE_DAMPER) * 0.6f, 0.6f);

            spriteBatch.DrawOnCtrl(this, _interact1, bounds.OffsetBy(DRAW_WIDTH / 2, DRAW_HEIGHT / 2), null, _tint * opacity, Math.Min((float)(GameService.Overlay.CurrentGameTime.TotalGameTime.TotalSeconds - _showStart) * 20f, MathHelper.TwoPi), new Vector2(DRAW_WIDTH / 2, DRAW_HEIGHT / 2));
        }
    }
}
