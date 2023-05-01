namespace Estreya.BlishHUD.Shared.Utils
{
    using Blish_HUD.Controls;
    using Blish_HUD;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework;
    using Estreya.BlishHUD.Shared.Services;
    using Blish_HUD.Content;
    using Estreya.BlishHUD.Shared.Controls;
    using Estreya.BlishHUD.Shared.Settings;

    public static class WindowUtil
    {
        public static Controls.StandardWindow CreateStandardWindow(BaseModuleSettings moduleSettings, string title, Type callingType, Guid guid, IconService iconService, AsyncTexture2D emblem = null)
        {
            var backgroundTexturePath = @"textures\setting_window_background.png";
            Texture2D windowBackground = iconService?.GetIcon(backgroundTexturePath);
            if (windowBackground == null || windowBackground == ContentService.Textures.Error)
            {
                throw new ArgumentNullException(nameof(windowBackground), $"Module does not include texture \"{backgroundTexturePath}\".");
            }

            Rectangle settingsWindowSize = new Rectangle(35, 26, 1100, 714);
            int contentRegionPaddingY = settingsWindowSize.Y - 15;
            int contentRegionPaddingX = settingsWindowSize.X;
            Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 6, settingsWindowSize.Height - contentRegionPaddingY);

            var window = new Controls.StandardWindow(moduleSettings, windowBackground, settingsWindowSize, contentRegion)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = title,
                SavesPosition = true,
                Id = $"{callingType.Name}_{guid}"
            };

            if (emblem != null)
            {
                if (emblem.HasSwapped)
                {
                    window.Emblem = emblem;
                }
                else
                {
                    emblem.TextureSwapped += (s, e) =>
                    {
                        window.Emblem = e.NewValue;
                    };
                }
            }

            return window;
        }

        public static TabbedWindow2 CreateTabbedWindow(string title, Type callingType, Guid guid, IconService iconService, AsyncTexture2D emblem = null)
        {
            var backgroundTexturePath = @"textures\setting_window_background.png";
            Texture2D windowBackground = iconService?.GetIcon(backgroundTexturePath);
            if (windowBackground == null || windowBackground == ContentService.Textures.Error)
            {
                throw new ArgumentNullException(nameof(windowBackground), $"Module does not include texture \"{backgroundTexturePath}\".");
            }

            Rectangle settingsWindowSize = new Rectangle(35, 26, 1100, 714);
            int contentRegionPaddingY = settingsWindowSize.Y - 15;
            int contentRegionPaddingX = settingsWindowSize.X + 46;
            Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 52, settingsWindowSize.Height - contentRegionPaddingY);

            var window = new TabbedWindow2(windowBackground, settingsWindowSize, contentRegion)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = title,
                SavesPosition = true,
                Id = $"{callingType.Name}_{guid}"
            };

            if (emblem != null)
            {
                if (emblem.HasSwapped)
                {
                    window.Emblem = emblem;
                }
                else
                {
                    emblem.TextureSwapped += (s, e) =>
                    {
                        window.Emblem = e.NewValue;
                    };
                }
            }

            return window;
        }
    }
}
