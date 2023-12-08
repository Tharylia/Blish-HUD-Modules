namespace Estreya.BlishHUD.WebhookUpdater;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using HandlebarsDotNet.Helpers.Enums;
using Microsoft.Xna.Framework;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Modules;
using Shared.Services;
using Shared.Settings;
using Shared.Threading;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using UI.Views;
using TabbedWindow = Shared.Controls.TabbedWindow;

[Export(typeof(Module))]
public class WebhookUpdaterModule : BaseModule<WebhookUpdaterModule, ModuleSettings>
{
    private TimeSpan _dataContextUpdateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100));
    private IHandlebars _handleBarsContext;
    private HandlebarsDataContext _handleBarsDataContext;
    private readonly AsyncRef<double> _lastDataContextUpdate = new AsyncRef<double>(0);
    private readonly AsyncLock _webhookLock = new AsyncLock();

    private readonly List<Webhook> _webhooks = new List<Webhook>();

    [ImportingConstructor]
    public WebhookUpdaterModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    protected override string UrlModuleName => "webhook-updater";

    protected override string API_VERSION_NO => "1";

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        return new ModuleSettings(settings);
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconService.GetIcon("textures/webhook.png");
    }

    protected override string GetDirectoryName()
    {
        return null;
    }

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconService.GetIcon("textures/webhook.png");
    }

    private void UpdateWebhooks(GameTime gameTime)
    {
        try
        {
            if (this._webhookLock.IsFree())
            {
                using (this._webhookLock.Lock())
                {
                    foreach (Webhook webhook in this._webhooks)
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
                this.Logger.Debug("WebhookLock is busy.");
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

        await this.BuildHandlebarsDataContext();

        foreach (string name in this.ModuleSettings.WebhookNames.Value)
        {
            this.AddWebhook(name);
        }

        this.Gw2ApiManager.SubtokenUpdated += this.Gw2ApiManager_SubtokenUpdated;
        GameService.Gw2Mumble.CurrentMap.MapChanged += this.CurrentMap_MapChanged;
    }

    private void CurrentMap_MapChanged(object sender, ValueEventArgs<int> e)
    {
        this.TriggerDataContextRefresh();
    }

    private void Gw2ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
    {
        this.TriggerDataContextRefresh();
    }

    private void TriggerDataContextRefresh()
    {
        this._lastDataContextUpdate.Value = MathHelper.Clamp((float)this._dataContextUpdateInterval.TotalMilliseconds - 250, 0, (float)this._dataContextUpdateInterval.TotalMilliseconds);
    }

    private void BuildHandlebarContext()
    {
        this._handleBarsContext = Handlebars.Create();
        HandlebarsHelpers.Register(this._handleBarsContext, Category.Math);

        this._handleBarsContext.RegisterHelper("toJson", (writer, context, parameters) =>
        {
            if (parameters.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters), "toJson: Minimum one parameter required.");
            }

            if (parameters.Length > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters), "toJson: Only one parameter supported.");
            }

            object element = parameters[0];
            writer.Write(JsonConvert.SerializeObject(element, new JsonSerializerSettings { Converters = new List<JsonConverter> { new StringEnumConverter() } }), false);
        });
    }

    public async Task BuildHandlebarsDataContext()
    {
        await this.AccountService.WaitForCompletion(TimeSpan.FromMinutes(5));
        Account account = this.AccountService.Account;

        List<Character> characters = new List<Character>();
        try
        {
            if (this.Gw2ApiManager.HasPermission(TokenPermission.Characters))
            {
                characters.AddRange(await this.Gw2ApiManager.Gw2ApiClient.V2.Characters.AllAsync());
            }
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed to load characters from api.");
        }

        Map map = null;
        try
        {
            map = GameService.Gw2Mumble.IsAvailable ? await this.Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(GameService.Gw2Mumble.CurrentMap.Id) : null;
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed to load map from api.");
        }

        HandlebarsDataContext dataContext = new HandlebarsDataContext
        {
            // mumble is assigned in the class
            api = new HandlebarsDataContext.APIContext
            {
                Account = account,
                Characters = characters.ToArray(),
                Map = map
            }
        };

        this._handleBarsDataContext = dataContext;

        using (await this._webhookLock.LockAsync())
        {
            foreach (Webhook webhook in this._webhooks)
            {
                webhook.UpdateDataContext(this._handleBarsDataContext);
            }
        }
    }

    protected override void ConfigureServices(ServiceConfigurations configurations)
    {
        configurations.Account.Enabled = true;
        configurations.Account.AwaitLoading = true;
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        this.UpdateWebhooks(gameTime);

        _ = UpdateUtil.UpdateAsync(this.BuildHandlebarsDataContext, gameTime, this._dataContextUpdateInterval.TotalMilliseconds, this._lastDataContextUpdate);
    }

    protected override void OnSettingWindowBuild(TabbedWindow settingWindow)
    {
        WebhookSettingsView webhookView = new WebhookSettingsView(this.ModuleSettings, () => this._webhooks, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService) { DefaultColor = this.ModuleSettings.DefaultGW2Color };
        webhookView.AddWebhook += this.WebhookView_AddWebhook;
        webhookView.RemoveWebhook += this.WebhookView_RemoveWebhook;

        settingWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156736.png"), () => webhookView, "Webhooks"));
        settingWindow.Tabs.Add(new Tab(this.IconService.GetIcon("157097.png"), () => new HelpView(this.Gw2ApiManager, this.IconService, this.TranslationService) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Help"));
    }

    private Webhook AddWebhook(string name)
    {
        WebhookConfiguration configuration = this.ModuleSettings.AddWebhook(name);

        Webhook webhook = new Webhook(configuration, this._handleBarsContext, this.GetFlurlClient());
        webhook.UpdateDataContext(this._handleBarsDataContext);

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
            Webhook webhook = this._webhooks.Find(w => w.Configuration.Name == name);

            if (webhook == null)
            {
                return;
            }

            this.ModuleSettings.WebhookNames.Value = new List<string>(this.ModuleSettings.WebhookNames.Value.Where(n => n != name));
            this._webhooks.Remove(webhook);
            this.ModuleSettings.RemoveWebhook(webhook.Configuration);
        }
    }

    private void WebhookView_RemoveWebhook(object sender, Webhook e)
    {
        this.RemoveWebhook(e.Configuration.Name);
    }

    private void WebhookView_AddWebhook(object sender, WebhookSettingsView.AddWebhookEventArgs e)
    {
        Webhook webhook = this.AddWebhook(e.Name);
        e.Webhook = webhook;
    }

    protected override void Unload()
    {
        base.Unload();

        this.Gw2ApiManager.SubtokenUpdated -= this.Gw2ApiManager_SubtokenUpdated;
        GameService.Gw2Mumble.CurrentMap.MapChanged -= this.CurrentMap_MapChanged;

        this._handleBarsContext = null;
        using (this._webhookLock.Lock())
        {
            foreach (Webhook webhook in this._webhooks)
            {
                webhook.Unload();
            }

            this._webhooks?.Clear();
        }
    }

    protected override int CornerIconPriority => 1_289_351_273;
}