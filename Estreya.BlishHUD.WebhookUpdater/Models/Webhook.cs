namespace Estreya.BlishHUD.WebhookUpdater.Models;

using Blish_HUD;
using Blish_HUD.Controls;
using Flurl.Http;
using HandlebarsDotNet;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Xna.Framework;
using Shared.Threading;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class Webhook : IUpdatable
{
    private static readonly Logger Logger = Logger.GetLogger<Webhook>();
    private readonly IFlurlClient _flurlClient;

    private IHandlebars _handlebarsContext;
    private HandlebarsDataContext _handlebarsDataContext;
    private TimeSpan _interval = Timeout.InfiniteTimeSpan;
    private string _lastContent;

    private string _lastUrl;
    private readonly AsyncRef<double> _timeSinceIntervalTick = new AsyncRef<double>(0);

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

    public WebhookConfiguration Configuration { get; set; }

    public void Update(GameTime gameTime)
    {
        if (!this.Configuration.Enabled.Value)
        {
            return;
        }

        if (this.Configuration.Mode.Value == UpdateMode.Interval && this._interval != Timeout.InfiniteTimeSpan)
        {
            _ = UpdateUtil.UpdateAsync(this.Send, gameTime, this._interval.TotalMilliseconds, this._timeSinceIntervalTick, false);
        }
    }

    private void CurrentMap_MapChanged(object sender, ValueEventArgs<int> e)
    {
        if (this.Configuration.Mode.Value == UpdateMode.MapChange)
        {
            this.Trigger();
        }
    }

    private void IntervalUnit_SettingChanged(object sender, ValueChangedEventArgs<TimeUnit> e)
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
        if (!double.TryParse(this.Configuration.Interval.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            string message = $"New update interval \"{this.Configuration.Interval.Value}\" of {this.Configuration.Name} is invalid.";
            Logger.Warn(message);
            if (!throwException)
            {
                GameService.Graphics.QueueMainThreadRender(device =>
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

            throw new FormatException(message); // Bubble up in view catch
        }

        try
        {
            switch (this.Configuration.IntervalUnit.Value)
            {
                case TimeUnit.Millisecond:
                    this._interval = TimeSpan.FromMilliseconds(value);
                    break;
                case TimeUnit.Second:
                    this._interval = TimeSpan.FromSeconds(value);
                    break;
                case TimeUnit.Minute:
                    this._interval = TimeSpan.FromMinutes(value);
                    break;
                case TimeUnit.Hour:
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
            Logger.Warn(ex, "Failed to update interval:");
            if (!throwException)
            {
                GameService.Graphics.QueueMainThreadRender(device =>
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
            string url = this.BuildUrl();

            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            string data = this.BuildData();

            if (this.Configuration.OnlyOnUrlOrDataChange.Value)
            {
                if (url == this._lastUrl && data == this._lastContent)
                {
                    return;
                }
            }

            string contentType = this.GetContentType();

            WebhookProtocol protocol = new WebhookProtocol
            {
                Url = url,
                Method = this.Configuration.HTTPMethod.Value,
                Payload = data,
                ContentType = contentType
            };

            try
            {
                IFlurlRequest request = this._flurlClient.Request(url);
                if (!string.IsNullOrWhiteSpace(contentType))
                {
                    request.WithHeader("Content-Type", contentType);
                }

                HttpMethod method = this.GetMethod();

                HttpResponseMessage response = await request
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
                protocol.StatusCode = fex.Call.Response?.StatusCode ?? HttpStatusCode.InternalServerError;
                protocol.Message = await fex.GetResponseStringAsync();
                protocol.Exception = new WebhookProtocol.ProtocolException(fex);

                throw;
            }
            catch (Exception ex)
            {
                protocol.StatusCode = HttpStatusCode.InternalServerError;
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

            this._lastUrl = url;
            this._lastContent = data;
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

    private string BuildUrl()
    {
        string url = this.Configuration.Url.Value;
        HandlebarsTemplate<object, object> template = this._handlebarsContext.Compile(url);

        return template.Invoke(this._handlebarsDataContext);
    }

    private string BuildData()
    {
        string data = this.Configuration.Content.Value;
        HandlebarsTemplate<object, object> template = this._handlebarsContext.Compile(data);

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