namespace Estreya.BlishHUD.WebhookUpdater.Models;

using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using Flurl.Util;
using HandlebarsDotNet;
using Humanizer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Webhook : IUpdatable
{
    private static readonly Logger Logger = Logger.GetLogger<Webhook>();

    private IHandlebars _handlebarsContext;
    private IFlurlClient _flurlClient;
    private TimeSpan _interval = Timeout.InfiniteTimeSpan;
    private AsyncRef<double> _timeSinceIntervalTick = new AsyncRef<double>(0);

    private string _lastUrl = null;
    private string _lastContent = null;

    public WebhookConfiguration Configuration { get; set; }

    public Webhook(WebhookConfiguration configuration, IHandlebars handlebarsContext, IFlurlClient flurlClient)
    {
        this.Configuration = configuration;
        this._handlebarsContext = handlebarsContext;
        this._flurlClient = flurlClient;

        this.Configuration.Interval.SettingChanged += this.Interval_SettingChanged;
        this.Configuration.IntervalUnit.SettingChanged += this.IntervalUnit_SettingChanged;

        this.UpdateInterval(false);
    }

    private void IntervalUnit_SettingChanged(object sender, ValueChangedEventArgs<Humanizer.Localisation.TimeUnit> e)
    {
        this.UpdateInterval(true);
    }

    private void Interval_SettingChanged(object sender, ValueChangedEventArgs<string> e)
    {
        this.UpdateInterval(true);
    }

    private void UpdateInterval(bool throwException)
    {
        if (!double.TryParse(this.Configuration.Interval.Value, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            var message = $"New update interval \"{this.Configuration.Interval.Value}\" is invalid.";
            Logger.Warn(message);
            if (!throwException)
            {
                GameService.Graphics.QueueMainThreadRender((device) =>
                {
                    ScreenNotification.ShowNotification(
                        DrawUtil.WrapText(
                            GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular),
                            message,
                            900),
                        ScreenNotification.NotificationType.Error);
                });
                return;
            }
            else
            {
                throw new FormatException(message); // Bubble up in view catch
            }
        }

        try
        {
            switch (this.Configuration.IntervalUnit.Value)
            {
                case Humanizer.Localisation.TimeUnit.Millisecond:
                    this._interval = TimeSpan.FromMilliseconds(value);
                    break;
                case Humanizer.Localisation.TimeUnit.Second:
                    this._interval = TimeSpan.FromSeconds(value);
                    break;
                case Humanizer.Localisation.TimeUnit.Minute:
                    this._interval = TimeSpan.FromMinutes(value);
                    break;
                case Humanizer.Localisation.TimeUnit.Hour:
                    this._interval = TimeSpan.FromHours(value);
                    break;
                default:
                    Logger.Error($"Invalid timeunit selected: {this.Configuration.IntervalUnit.Value.Humanize()}");
                    break;
            }

            Logger.Info($"Updated interval to: {this._interval.Humanize(2)}");
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"Failed to update interval:");
            if (!throwException)
            {
                GameService.Graphics.QueueMainThreadRender((device) =>
                {
                    ScreenNotification.ShowNotification(
                    DrawUtil.WrapText(
                        GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular),
                        $"Failed to update interval: {ex.Message}",
                        900),
                    ScreenNotification.NotificationType.Error);
                });
            }
            else
            {
                throw new FormatException($"Failed to update interval: {ex.Message}"); // Bubble up in view catch
            }
        }
    }

    public async Task Send()
    {
        try
        {
            var url = this.BuildUrl();

            if (string.IsNullOrEmpty(url)) return;

            var data = this.BuildData();

            if (this.Configuration.OnlyOnUrlOrDataChange.Value)
            {
                if (url == _lastUrl && data == _lastContent) return;
            }

            await this._flurlClient.Request(url).WithHeader("Content-Type", this.Configuration.ContentType.Value).PostAsync(new StringContent(data));

            _lastUrl = url;
            _lastContent = data;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"Failed to send webhook request for {this.Configuration.Name}:");
        }
    }

    public void Update(GameTime gameTime)
    {
        if (!this.Configuration.Enabled.Value) return;

        if (this.Configuration.Mode.Value == Models.UpdateMode.Interval && this._interval != Timeout.InfiniteTimeSpan)
        {
            _ = UpdateUtil.UpdateAsync(this.Send, gameTime, this._interval.TotalMilliseconds, this._timeSinceIntervalTick, false);
        }
    }

    private string BuildUrl()
    {
        var url = this.Configuration.Url.Value;
        var template = this._handlebarsContext.Compile(url);

        return template.Invoke(new
        {
            mumble = GameService.Gw2Mumble
        });
    }

    private string BuildData()
    {
        var data = this.Configuration.Content.Value;
        var template = this._handlebarsContext.Compile(data);

        return template.Invoke(new
        {
            mumble = GameService.Gw2Mumble
        });
    }

    public void Unload()
    {
        this._handlebarsContext = null;
    }
}
