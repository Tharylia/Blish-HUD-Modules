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
using Estreya.BlishHUD.WebhookUpdater.Models;
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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

[Export(typeof(Blish_HUD.Modules.Module))]
public class WebhookUpdaterModule : BaseModule<WebhookUpdaterModule, ModuleSettings>
{
    private IHandlebars _handleBarsContext;

    private List<Webhook> _webhooks = new List<Webhook>();
    private AsyncLock _webhookLock = new AsyncLock();

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

    private void UpdateWebhooks(GameTime gameTime)
    {
        try
        {
            if (this._webhookLock.IsFree())
            {
                using (this._webhookLock.Lock())
                {
                    foreach (var webhook in this._webhooks)
                    {
                        try
                        {
                            webhook.Update(gameTime);
                        }
                        catch (Exception ex)
                        {
                            this.Logger.Warn(ex, $"Failed updating webhook {webhook.Configuration.Name}");
                        }
                    }
                }
            }
            else
            {
                Logger.Debug("WebhookLock is busy.");
            }
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed to update webhooks:");
        }
    }

    protected override async Task LoadAsync()
    {
        await base.LoadAsync();

        this.BuildHandlebarContext();

        foreach (var name in this.ModuleSettings.WebhookNames.Value)
        {
            this.AddWebhook(name);
        }
    }

    private void BuildHandlebarContext()
    {
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
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        this.UpdateWebhooks(gameTime);
    }

    protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
    {
        var webhookView = new UI.Views.WebhookSettingsView(() => this._webhooks.Select(w => w.Configuration), this.Gw2ApiManager, this.IconState, this.TranslationState, this.SettingEventState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color };
        webhookView.AddWebhook += this.WebhookView_AddWebhook;
        webhookView.RemoveWebhook += this.WebhookView_RemoveWebhook;

        settingWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156736.png"), () => webhookView, "Webhooks"));
    }

    private Webhook AddWebhook(string name)
    {
        var configuration = this.ModuleSettings.AddWebhook(name);

        var webhook = new Webhook(configuration, this._handleBarsContext, this.GetFlurlClient());

        if (!this.ModuleSettings.WebhookNames.Value.Contains(name))
        {
            this.ModuleSettings.WebhookNames.Value = new List<string>(this.ModuleSettings.WebhookNames.Value) { name };
        }

        using (this._webhookLock.Lock())
        {
            this._webhooks.Add(webhook);
        }

        return webhook;
    }

    private void RemoveWebhook(string name)
    {
        using (this._webhookLock.Lock())
        {
            var webhook = this._webhooks.Find(w => w.Configuration.Name == name);

            if (webhook == null) return;

            this.ModuleSettings.WebhookNames.Value = new List<string>(this.ModuleSettings.WebhookNames.Value.Where(n => n != name));
            this._webhooks.Remove(webhook);
            this.ModuleSettings.RemoveWebhook(webhook.Configuration);
        }
    }

    private void WebhookView_RemoveWebhook(object sender, WebhookConfiguration e)
    {
        this.RemoveWebhook(e.Name);
    }

    private void WebhookView_AddWebhook(object sender, UI.Views.WebhookSettingsView.AddWebhookEventArgs e)
    {
        var webhook = this.AddWebhook(e.Name);
        e.Configuration = webhook.Configuration;
    }

    protected override void Unload()
    {
        base.Unload();

        this._handleBarsContext = null;
        using (this._webhookLock.Lock())
        {
            this._webhooks?.Clear();
        }
    }
}
