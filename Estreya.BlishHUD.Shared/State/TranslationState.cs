namespace Estreya.BlishHUD.Shared.State;

using Estreya.BlishHUD.Shared.Extensions;
using Flurl.Http;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.TextFormatting;

public class TranslationState : ManagedState
{
    ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _translations;
    private IFlurlClient _flurlClient;
    private readonly string _rootUrl;
    private static List<string> _locales = new List<string>()
    {
        "en",
        "de",
        "es",
        "fr"
    };

    public TranslationState(StateConfiguration configuration, IFlurlClient flurlClient, string rootUrl) : base(configuration)
    {
        this._flurlClient = flurlClient;
        this._rootUrl = rootUrl;
    }

    protected override Task Initialize()
    {
        this._translations = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

        return Task.CompletedTask;
    }

    protected override Task Clear()
    {
        this._translations?.Clear();
        return Task.CompletedTask;
    }

    protected override void InternalUnload()
    {
        _translations?.Clear();
        _translations = null;

        _flurlClient = null;
    }

    protected override void InternalUpdate(GameTime gameTime) { }

    protected override async Task Load()
    {
        foreach (var locale in _locales)
        {
            await this.LoadLocale(locale);
        }
    }

    private async Task LoadLocale(string locale)
    {
        try
        {
            var translations = await this._flurlClient.Request(this._rootUrl, $"translation.{locale}.properties").GetStringAsync();

            ConcurrentDictionary<string, string> localeTranslations = new ConcurrentDictionary<string, string>();

            var lines = translations.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var lineParts = line.Trim('\n', '\r').Split('=');
                if (lineParts.Length < 2)
                {
                    // Incomplete
                    continue;
                }

                string key = lineParts[0];
                string value = string.Join("=", lineParts.Skip(1));

                var added = localeTranslations.TryAdd(key, value);
                if (!added)
                {
                    Logger.Warn($"{key} for locale {locale} already added.");
                }
            }

            this._translations.TryAdd(locale, localeTranslations);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, $"Failed to load translations for locale {locale}:");
        }
    }

    public string GetTranslation(string key, string defaultValue = null)
    {
        if (string.IsNullOrEmpty(key)) return defaultValue;

        var translations = this.GetTranslationsForLocale(Thread.CurrentThread.CurrentUICulture);

        return translations?.TryGetValue(key, out var result) ?? false ? result : defaultValue;
    }

    private ConcurrentDictionary<string,string> GetTranslationsForLocale(CultureInfo locale)
    {
        var tempLocale = locale;
        while (tempLocale != null && tempLocale.LCID != 127)
        {
            if (_translations.TryGetValue(tempLocale.Name, out var translations))
            {
                return translations;
            }

            tempLocale = tempLocale.Parent;
        }

        return null;
    }
}
