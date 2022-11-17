namespace Estreya.BlishHUD.Shared.State;

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

public class TranslationState : ManagedState
{
    ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _translations;
    private readonly string _baseUrl;
    private WebClient _webclient;
    private static List<string> _locales = new List<string>()
    {
        "en", // en
        "de"
    };

    public TranslationState(StateConfiguration configuration, WebClient webclient, string baseUrl) : base(configuration)
    {
        this._baseUrl = baseUrl.TrimEnd('/');
        this._webclient = webclient;
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

        _webclient = null;
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
            var translationData = await this._webclient.DownloadDataTaskAsync($"{this._baseUrl}/translation.{locale}.properties");

            var translations = Encoding.UTF8.GetString(translationData);

            ConcurrentDictionary<string, string> localeTranslations = new ConcurrentDictionary<string, string>();

            var lines = translations.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var lineParts = line.Split('=');
                if (lineParts.Length < 2)
                {
                    // Incomplete
                    continue;
                }

                string key = lineParts[0];
                string value = lineParts[1];

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
            var fail = ex.Message;
        }
    }

    public string GetTranslation(string key, string defaultValue = null)
    {
        if (string.IsNullOrEmpty(key)) return defaultValue;

        var translations = this.GetTranslationForLocale(Thread.CurrentThread.CurrentUICulture);

        return translations?.TryGetValue(key, out var result) ?? false ? result : defaultValue;
    }

    private ConcurrentDictionary<string,string> GetTranslationForLocale(CultureInfo locale)
    {
        var tempLocale = locale;
        while (tempLocale != null)
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
