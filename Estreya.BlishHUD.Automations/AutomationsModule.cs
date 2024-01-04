namespace Estreya.BlishHUD.Automations;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Automations.Models.Automations.MapChange;
using Estreya.BlishHUD.Automations.Services;
using Estreya.BlishHUD.Shared.Services;
using Gw2Sharp.WebApi.V2.Models;
using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using HandlebarsDotNet.Helpers.Enums;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Helpers;
using Shared.Modules;
using Shared.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

[Export(typeof(Module))]
public class AutomationsModule : BaseModule<AutomationsModule, ModuleSettings>
{
    private IHandlebars _handleBarsContext;

    [ImportingConstructor]
    public AutomationsModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    protected override string UrlModuleName => "automations";

    protected override string API_VERSION_NO => "1";

    protected override bool FailIfBackendDown => false;

    private MapChangeAutomationService MapChangeAutomationService { get; set; }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override async Task LoadAsync()
    {
        this.BuildHandlebarContext();
        await base.LoadAsync();

        MapChangeAutomationEntry mapChangeToQueensdale = new MapChangeAutomationEntry("test map change");

        mapChangeToQueensdale.AddAction(async (input) =>
        {
            Map fromMap = input.FromMapId is -1 or 0 ? null : await this.Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(input.FromMapId);
            Map toMap = input.ToMapId == -1 ? null : await this.Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(input.ToMapId);

            HandlebarsTemplate<object, object> template = this._handleBarsContext.Compile("{{#if fromMap}}Changed map from \"{{fromMap.Name}}\" ({{fromMap.Id}}) to \"{{toMap.Name}}\" ({{toMap.Id}}){{else}}Changed map to \"{{toMap.Name}}\" ({{toMap.Id}}){{/if}}.");
            ScreenNotification.ShowNotification(template.Invoke(new
            {
                toMap,
                fromMap
            }));
        });

        this.MapChangeAutomationService.AddAutomation(mapChangeToQueensdale);
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

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    /// <inheritdoc />
    protected override void Unload()
    {
        base.Unload();
    }


    public override IView GetSettingsView()
    {
        return new Shared.UI.Views.ModuleSettingsView(this.IconService, this.TranslationService);
    }

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        return new ModuleSettings(settings);
    }

    protected override Collection<ManagedService> GetAdditionalServices(string directoryPath)
    {
        this.MapChangeAutomationService = new MapChangeAutomationService(new ServiceConfiguration()
        {
            AwaitLoading = false,
            Enabled = true
        }, this.GetFlurlClient(), this.Gw2ApiManager, this._handleBarsContext);

        Collection<ManagedService> services = new Collection<ManagedService>
        {
            this.MapChangeAutomationService
        };

        return services;
    }

    protected override string GetDirectoryName()
    {
        return null;
    }

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconService.GetIcon("156764.png");
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconService.GetIcon("156764.png");
    }

    protected override int CornerIconPriority => 1_289_351_268;
}