namespace Estreya.BlishHUD.Shared.Extensions
{
    using Blish_HUD;
    using Blish_HUD.Settings;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;

    public static class SettingCollectionExtensions
    {
        public static void AddLoggingEvents(this SettingCollection settings)
        {
            foreach (var setting in settings)
            {
                var method = typeof(SettingEntryExtensions).GetMethod(nameof(SettingEntryExtensions.AddLoggingEvent)).MakeGenericMethod(setting.SettingType);
                method.Invoke(setting, new object[] { setting });
            }
        }

        public static void RemoveLoggingEvents(this SettingCollection settings)
        {
            foreach (var setting in settings)
            {
                var method = typeof(SettingEntryExtensions).GetMethod(nameof(SettingEntryExtensions.RemoveLoggingEvent)).MakeGenericMethod(setting.SettingType);
                method.Invoke(setting, new object[] { setting });
            }
        }
    }
}
