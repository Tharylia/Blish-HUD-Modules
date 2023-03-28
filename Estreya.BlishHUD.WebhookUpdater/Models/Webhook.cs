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
using Octokit;
using SharpDX.Direct3D9;
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
    private HandlebarsDataContext _handlebarsDataContext;
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
        GameService.Gw2Mumble.CurrentMap.MapChanged += this.CurrentMap_MapChanged;

        this.UpdateInterval(false);
    }

    private void CurrentMap_MapChanged(object sender, ValueEventArgs<int> e)
    {
        if (this.Configuration.Mode.Value == UpdateMode.MapChange)
        {
            this.Trigger();
        }
    }

    private void IntervalUnit_SettingChanged(object sender, ValueChangedEventArgs<Humanizer.Localisation.TimeUnit> e)
    {
        this.UpdateInterval(true);
    }

    private void Interval_SettingChanged(object sender, ValueChangedEventArgs<string> e)
    {
        this.UpdateInterval(true);
    }

    public void UpdateDataContext(HandlebarsDataContext ctx)
    {
        this._handlebarsDataContext = ctx;
    }

    private void UpdateInterval(bool throwException)
    {
        if (!double.TryParse(this.Configuration.Interval.Value, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            var message = $"New update interval \"{this.Configuration.Interval.Value}\" of {this.Configuration.Name} is invalid.";
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
                    Logger.Error($"Invalid timeunit selected of {this.Configuration.Name}: {this.Configuration.IntervalUnit.Value.Humanize()}");
                    break;
            }

            Logger.Info($"Updated interval of {this.Configuration.Name} to: {this._interval.Humanize(2)}");
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
                        $"Failed to update interval of {this.Configuration.Name}: {ex.Message}",
                        900),
                    ScreenNotification.NotificationType.Error);
                });
            }
            else
            {
                throw new FormatException($"Failed to update interval of {this.Configuration.Name}: {ex.Message}"); // Bubble up in view catch
            }
        }
    }

    private async Task Send()
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

            var contentType = this.GetContentType();

            WebhookProtocol protocol = new WebhookProtocol
            {
                Url = url,
                Method = this.Configuration.HTTPMethod.Value,
                Payload = data,
                ContentType = contentType
            };

            try
            {
                var request = this._flurlClient.Request(url);
                if (!string.IsNullOrWhiteSpace(contentType))
                {
                    request.WithHeader("Content-Type", contentType);
                }

                var method = this.GetMethod();

                var response = await request
                    .SendAsync(
                        method,
                        !string.IsNullOrWhiteSpace(data)
                            ? new StringContent(data)
                            : null
                    );

                protocol.StatusCode = response.StatusCode;
                protocol.Message = await response.Content.ReadAsStringAsync();
            }
            catch (FlurlHttpException fex)
            {
                protocol.StatusCode = fex.Call.Response?.StatusCode ?? System.Net.HttpStatusCode.InternalServerError;
                protocol.Message = await fex.GetResponseStringAsync();
                protocol.Exception = new WebhookProtocol.ProtocolException(fex);

                throw;
            }
            catch (Exception ex)
            {
                protocol.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                protocol.Exception = new WebhookProtocol.ProtocolException(ex);

                throw;
            }
            finally
            {
                if (this.Configuration.CollectProtocols.Value)
                {
                    this.Configuration.Protocol.Value = new List<WebhookProtocol>(this.Configuration.Protocol.Value) { protocol };
                }
            }

            _lastUrl = url;
            _lastContent = data;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"Failed to send webhook request for {this.Configuration.Name}:");
        }
    }

    public async Task TriggerAsync(bool resetInterval = true)
    {
        await this.Send();

        if (resetInterval)
        {
            this._timeSinceIntervalTick.Value = 0;
        }
    }


    public void Trigger(bool resetInterval = true)
    {
        _ = Task.Run(async () => await this.TriggerAsync(resetInterval));
    }

    private string GetContentType()
    {
        return this.Configuration.HTTPMethod.Value switch
        {
            HTTPMethod.GET or HTTPMethod.DELETE or HTTPMethod.OPTIONS or HTTPMethod.TRACE or HTTPMethod.HEAD => null,
            _ => this.Configuration.ContentType.Value
        };
    }

    private HttpMethod GetMethod()
    {
        return this.Configuration.HTTPMethod.Value switch
        {
            HTTPMethod.GET => HttpMethod.Get,
            HTTPMethod.POST => HttpMethod.Post,
            HTTPMethod.PUT => HttpMethod.Put,
            HTTPMethod.PATCH => new HttpMethod("patch"),
            HTTPMethod.DELETE => HttpMethod.Delete,
            HTTPMethod.OPTIONS => HttpMethod.Options,
            HTTPMethod.TRACE => HttpMethod.Trace,
            HTTPMethod.HEAD => HttpMethod.Head,
            _ => throw new ArgumentOutOfRangeException("Http Method")
        };
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

        return template.Invoke(this._handlebarsDataContext);
    }

    private string BuildData()
    {
        var data = this.Configuration.Content.Value;
        var template = this._handlebarsContext.Compile(data);

        return template.Invoke(this._handlebarsDataContext);
    }

    public void Unload()
    {
        this._handlebarsContext = null;

        this.Configuration.Interval.SettingChanged -= this.Interval_SettingChanged;
        this.Configuration.IntervalUnit.SettingChanged -= this.IntervalUnit_SettingChanged;
        GameService.Gw2Mumble.CurrentMap.MapChanged -= this.CurrentMap_MapChanged;
    }
}
