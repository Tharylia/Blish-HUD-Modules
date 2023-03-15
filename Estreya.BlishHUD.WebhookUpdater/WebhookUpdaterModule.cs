namespace Estreya.BlishHUD.WebhookUpdater;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Modules;
using Estreya.BlishHUD.Shared.Settings;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using Humanizer;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

[Export(typeof(Blish_HUD.Modules.Module))]
public class WebhookUpdaterModule : BaseModule<WebhookUpdaterModule, ModuleSettings>
{
    private TimeSpan _updateInterval = Timeout.InfiniteTimeSpan;
    private AsyncRef<double> _lastUpdate = new AsyncRef<double>(0);

    private string _lastUrl = null;
    private string _lastContent = null;

    private IHandlebars _handleBarsContext;

    [ImportingConstructor]
    public WebhookUpdaterModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    public override string WebsiteModuleName => "webhook-updater";

    protected override string API_VERSION_NO => "1";

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        return new ModuleSettings(settings);
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconState.GetIcon("textures/webhook.png");
    }

    protected override string GetDirectoryName() => null;

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconState.GetIcon("textures/webhook.png");
    }

    private async Task UpdateWebhook()
    {
        try
        {
            var url = this.BuildUrl();

            if (string.IsNullOrEmpty(url)) return;

            var data = this.BuildData();

            if (this.ModuleSettings.UpdateOnlyOnUrlOrDataChange.Value)
            {
                if (url == _lastUrl && data == _lastContent) return;
            }

            await this.GetFlurlClient().Request(url).PostAsync(new StringContent(data));

            _lastUrl = url;
            _lastContent = data;
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed to update webhook:");
        }
    }

    private string BuildUrl()
    {
        var url = this.ModuleSettings.WebhookUrl.Value;
        var template = this._handleBarsContext.Compile(url);

        return template.Invoke(new
        {
            mumble = GameService.Gw2Mumble
        });
    }

    private string BuildData()
    {
        var data = this.ModuleSettings.WebhookStringContent.Value;
        var template = this._handleBarsContext.Compile(data);

        return template.Invoke(new
        {
            mumble = GameService.Gw2Mumble
        });
    }

    protected override async Task LoadAsync()
    {
        await base.LoadAsync();

        this._handleBarsContext = HandlebarsDotNet.Handlebars.Create();
        HandlebarsHelpers.Register(this._handleBarsContext, HandlebarsDotNet.Helpers.Enums.Category.Math);

        this._handleBarsContext.RegisterHelper("toJson", (writer, context, parameters) =>
        {
            if (parameters.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters), "toJson: Minimum one parameter required.");
            }
            else if (parameters.Length > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters), "toJson: Only one parameter supported.");
            }

            var element = parameters[0];
            writer.Write(JsonConvert.SerializeObject(element, new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>()
                {
                    new StringEnumConverter()
                }
            }), false);
        });

        GameService.Gw2Mumble.CurrentMap.MapChanged += this.CurrentMap_MapChanged;
        this.ModuleSettings.UpdateInterval.SettingChanged += this.UpdateInterval_SettingChanged;
        this.ModuleSettings.UpdateIntervalUnit.SettingChanged += this.UpdateIntervalUnit_SettingChanged;

        this.UpdateInterval(false);
    }

    private void UpdateIntervalUnit_SettingChanged(object sender, ValueChangedEventArgs<Humanizer.Localisation.TimeUnit> e)
    {
        this.UpdateInterval(true);
    }

    private void UpdateInterval_SettingChanged(object sender, ValueChangedEventArgs<string> e)
    {
        this.UpdateInterval(true);
    }

    protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
    {
        settingWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156736.png"), () => new UI.Views.GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconState, this.TranslationState, this.SettingEventState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));
    }

    private void UpdateInterval(bool throwException)
    {
        if (!double.TryParse(this.ModuleSettings.UpdateInterval.Value, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            var message = $"New update interval \"{this.ModuleSettings.UpdateInterval.Value}\" is invalid.";
            this.Logger.Warn(message);
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
            switch (this.ModuleSettings.UpdateIntervalUnit.Value)
            {
                case Humanizer.Localisation.TimeUnit.Millisecond:
                    this._updateInterval = TimeSpan.FromMilliseconds(value);
                    break;
                case Humanizer.Localisation.TimeUnit.Second:
                    this._updateInterval = TimeSpan.FromSeconds(value);
                    break;
                case Humanizer.Localisation.TimeUnit.Minute:
                    this._updateInterval = TimeSpan.FromMinutes(value);
                    break;
                case Humanizer.Localisation.TimeUnit.Hour:
                    this._updateInterval = TimeSpan.FromHours(value);
                    break;
                default:
                    this.Logger.Error($"Invalid timeunit selected: {this.ModuleSettings.UpdateIntervalUnit.Value.Humanize()}");
                    break;
            }

            this.Logger.Info($"Updated interval to: {this._updateInterval.Humanize(2)}");
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, $"Failed to update interval:");
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

    private void CurrentMap_MapChanged(object sender, ValueEventArgs<int> e)
    {
        if (this.ModuleSettings.UpdateMode.Value == Models.UpdateMode.MapChange)
        {
            _ = Task.Run(this.UpdateWebhook);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (this.ModuleSettings.UpdateMode.Value == Models.UpdateMode.Interval && this._updateInterval != Timeout.InfiniteTimeSpan)
        {
            _ = UpdateUtil.UpdateAsync(this.UpdateWebhook, gameTime, _updateInterval.TotalMilliseconds, _lastUpdate, false);
        }
    }

    protected override void Unload()
    {
        GameService.Gw2Mumble.CurrentMap.MapChanged -= this.CurrentMap_MapChanged;
        this.ModuleSettings.UpdateInterval.SettingChanged -= this.UpdateInterval_SettingChanged;
        this.ModuleSettings.UpdateIntervalUnit.SettingChanged -= this.UpdateIntervalUnit_SettingChanged;

        base.Unload();
    }
}
