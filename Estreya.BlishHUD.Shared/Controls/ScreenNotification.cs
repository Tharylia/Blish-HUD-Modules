namespace Estreya.BlishHUD.Shared.Controls
{
    using Microsoft.Xna.Framework.Graphics;
    using static Blish_HUD.Controls.ScreenNotification;

    public static class ScreenNotification
    {
        public static void ShowNotification(string message, NotificationType type = NotificationType.Info, Texture2D icon = null, int duration = 4)
        {
            ShowNotification(new[] { message }, type, icon, duration);
        }

        public static void ShowNotification(string[] messages, NotificationType type = NotificationType.Info, Texture2D icon = null, int duration = 4)
        {
            for (int i = messages.Length - 1; i >= 0; i--)
            {
                Blish_HUD.Controls.ScreenNotification.ShowNotification(messages[i], type, icon, duration);
            }
        }
    }
}
