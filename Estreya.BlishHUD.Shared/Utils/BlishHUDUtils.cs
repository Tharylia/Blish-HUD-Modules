namespace Estreya.BlishHUD.Shared.Utils
{
    using Blish_HUD;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class BlishHUDUtils
    {
        public static string GetLocaleAsISO639_1()
        {
            return GameService.Overlay.UserLocale.Value switch
            {
                Gw2Sharp.WebApi.Locale.German => "de",
                Gw2Sharp.WebApi.Locale.Spanish => "es",
                Gw2Sharp.WebApi.Locale.French => "fr",
                _ => "en",
            };
        }
    }
}
