namespace Estreya.BlishHUD.WebhookUpdater;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Humanizer.Localisation;
using Microsoft.Xna.Framework.Input;
using Models;
using Shared.Settings;
using System.Collections.Generic;

public class ModuleSettings : BaseModuleSettings
{
    private SettingCollection _webhookSettings;

    public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(ModifierKeys.Alt, Keys.W)) { }
    public SettingEntry<List<string>> WebhookNames { get; private set; }

    protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
    {
        this.WebhookNames = globalSettingCollection.DefineSetting(nameof(this.WebhookNames), new List<string>());
    }

    protected override void InitializeAdditionalSettings(SettingCollection settings)
    {
        this._webhookSettings = settings.AddSubCollection("webhooks");
    }

    public WebhookConfiguration AddWebhook(string name)
    {
        SettingEntry<bool> enabled = this._webhookSettings.DefineSetting($"{name}-enabled", true, () => "Enabled", () => "Defines if the webhook is enabled.");
        SettingEntry<string> url = this._webhookSettings.DefineSetting($"{name}-url", string.Empty, () => "Url", () => "Defines the webhook url.");
        SettingEntry<string> content = this._webhookSettings.DefineSetting($"{name}-content", string.Empty, () => "Content", () => "Defines the webhook content.");
        SettingEntry<string> contentType = this._webhookSettings.DefineSetting($"{name}-contentType", "text/plain", () => "Content Type", () => "Defines the content type which the request should represent.");
        SettingEntry<UpdateMode> mode = this._webhookSettings.DefineSetting($"{name}-mode", UpdateMode.Interval, () => "Mode", () => "Defines the webhook update mode.");
        SettingEntry<string> interval = this._webhookSettings.DefineSetting($"{name}-interval", "5", () => "Interval", () => "Defines the webhook update interval.");
        SettingEntry<TimeUnit> intervalUnit = this._webhookSettings.DefineSetting($"{name}-intervalUnit", TimeUnit.Minute, () => "Interval Unit", () => "Defines the webhook update interval unit.");
        SettingEntry<bool> onlyOnChange = this._webhookSettings.DefineSetting($"{name}-onlyOnChange", true, () => "Update only on change", () => "Whether the webhook should only be called if the url or the data changed.");
        SettingEntry<HTTPMethod> httpMethod = this._webhookSettings.DefineSetting($"{name}-httpMethod", HTTPMethod.POST, () => "HTTP Method", () => "Defines the method for the http request.");
        SettingEntry<bool> collectProtocols = this._webhookSettings.DefineSetting($"{name}-collectProtocols", true, () => "Collect Protocols", () => "Defines if protocols should be collected on webhook trigger.");
        SettingEntry<List<WebhookProtocol>> protocol = this._webhookSettings.DefineSetting($"{name}-protocol", new List<WebhookProtocol>(), () => "Protocol", () => "Logs all performed requests.");

        WebhookConfiguration configuration = new WebhookConfiguration(name)
        {
            Enabled = enabled,
            Url = url,
            Content = content,
            ContentType = contentType,
            Mode = mode,
            Interval = interval,
            IntervalUnit = intervalUnit,
            OnlyOnUrlOrDataChange = onlyOnChange,
            HTTPMethod = httpMethod,
            CollectProtocols = collectProtocols,
            Protocol = protocol
        };

        return configuration;
    }

    public void RemoveWebhook(WebhookConfiguration webhook)
    {
        this._webhookSettings.UndefineSetting($"{webhook.Name}-enabled");
        this._webhookSettings.UndefineSetting($"{webhook.Name}-url");
        this._webhookSettings.UndefineSetting($"{webhook.Name}-content");
        this._webhookSettings.UndefineSetting($"{webhook.Name}-contentType");
        this._webhookSettings.UndefineSetting($"{webhook.Name}-mode");
        this._webhookSettings.UndefineSetting($"{webhook.Name}-interval");
        this._webhookSettings.UndefineSetting($"{webhook.Name}-intervalUnit");
        this._webhookSettings.UndefineSetting($"{webhook.Name}-onlyOnChange");
        this._webhookSettings.UndefineSetting($"{webhook.Name}-httpMethod");
        this._webhookSettings.UndefineSetting($"{webhook.Name}-collectProtocols");
        this._webhookSettings.UndefineSetting($"{webhook.Name}-protocol");
    }

    public override void Unload()
    {
        base.Unload();
    }
}