namespace Estreya.BlishHUD.Shared.Services;

using Flurl.Http;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class TranslationService : ManagedService
{
    private static readonly List<string> _locales = new List<string>
    {
        "en",
        "de",
        "es",
        "fr"
    };

    private readonly string _rootUrl;
    private IFlurlClient _flurlClient;
    private ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _translations;

    public TranslationService(ServiceConfiguration configuration, IFlurlClient flurlClient, string rootUrl) : base(configuration)
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
        this._translations?.Clear();
        this._translations = null;

        this._flurlClient = null;
    }

    protected override void InternalUpdate(GameTime gameTime) { }

    protected override async Task Load()
    {
        var tasks = _locales.Select(this.LoadLocale);

        await Task.WhenAll(tasks);
    }

    private async Task LoadLocale(string locale)
    {
        try
        {
            string translations = await this._flurlClient.Request(this._rootUrl, $"translation.{locale}.properties").WithTimeout(TimeSpan.FromSeconds(5)).GetStringAsync();

            ConcurrentDictionary<string, string> localeTranslations = new ConcurrentDictionary<string, string>();

            string[] lines = translations.Split(new[]
            {
                '\n'
            }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string[] lineParts = line.Trim('\n', '\r').Split('=');
                if (lineParts.Length < 2)
                {
                    // Incomplete
                    continue;
                }

                string key = lineParts[0];
                string value = string.Join("=", lineParts.Skip(1));

                bool added = localeTranslations.TryAdd(key, value);
                if (!added)
                {
                    this.Logger.Warn($"{key} for locale {locale} already added.");
                }
            }

            this._translations.TryAdd(locale, localeTranslations);

            this.Logger.Debug($"Loaded {localeTranslations.Count} translations for locale {locale}");
        }
        catch (Exception ex)
        {
            this.Logger.Debug(ex, $"Failed to load translations for locale {locale}:");
        }
    }

    public string GetTranslation(string key, string defaultValue = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            return defaultValue;
        }

        ConcurrentDictionary<string, string> translations = this.GetTranslationsForLocale(Thread.CurrentThread.CurrentUICulture);

        return translations?.TryGetValue(key, out string result) ?? false ? result : defaultValue;
    }

    private ConcurrentDictionary<string, string> GetTranslationsForLocale(CultureInfo locale)
    {
        CultureInfo tempLocale = locale;
        while (tempLocale != null && tempLocale.LCID != 127)
        {
            if (this._translations.TryGetValue(tempLocale.Name, out ConcurrentDictionary<string, string> translations))
            {
                return translations;
            }

            tempLocale = tempLocale.Parent;
        }

        return null;
    }
}