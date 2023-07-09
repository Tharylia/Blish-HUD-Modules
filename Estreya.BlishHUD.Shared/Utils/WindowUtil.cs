namespace Estreya.BlishHUD.Shared.Utils;

using Blish_HUD;
using Blish_HUD.Content;
using Controls;
using Microsoft.Xna.Framework;
using Services;
using Settings;
using System;

public static class WindowUtil
{
    private static AsyncTexture2D GetWindowBackgroundTexture(IconService iconService)
    {
        return iconService?.GetIcon("502049.png");
    }

    private static Rectangle GetDefaultWindowSize()
    {
        return new Rectangle(35, 26, 930, 710);
    }

    public static StandardWindow CreateStandardWindow(BaseModuleSettings moduleSettings, string title, Type callingType, Guid guid, IconService iconService, AsyncTexture2D emblem = null)
    {
        AsyncTexture2D windowBackground = GetWindowBackgroundTexture(iconService);

        Rectangle settingsWindowSize = GetDefaultWindowSize();
        int contentRegionPaddingY = settingsWindowSize.Y - 15;
        int contentRegionPaddingX = settingsWindowSize.X;
        Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 6, settingsWindowSize.Height - contentRegionPaddingY);

        StandardWindow window = new StandardWindow(moduleSettings, windowBackground, settingsWindowSize, contentRegion)
        {
            Parent = GameService.Graphics.SpriteScreen,
            Title = title,
            SavesPosition = true,
            Id = $"{callingType.Name}_{guid}"
        };

        QueueEmblemChange(window, emblem);

        return window;
    }

    public static TabbedWindow CreateTabbedWindow(BaseModuleSettings moduleSettings, string title, Type callingType, Guid guid, IconService iconService, AsyncTexture2D emblem = null)
    {
        AsyncTexture2D windowBackground = GetWindowBackgroundTexture(iconService);

        Rectangle settingsWindowSize = GetDefaultWindowSize();
        int contentRegionPaddingY = settingsWindowSize.Y - 15;
        int contentRegionPaddingX = settingsWindowSize.X + 46;
        Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 46, settingsWindowSize.Height);

        TabbedWindow window = new TabbedWindow(moduleSettings, windowBackground, settingsWindowSize, contentRegion)
        {
            Parent = GameService.Graphics.SpriteScreen,
            Title = title,
            SavesPosition = true,
            Id = $"{callingType.Name}_{guid}"
        };

        QueueEmblemChange(window, emblem);

        return window;
    }

    private static void QueueEmblemChange(Window window, AsyncTexture2D emblem)
    {
        if (emblem == null)
        {
            return;
        }

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
}