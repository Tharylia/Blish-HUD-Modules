namespace Estreya.BlishHUD.Automations;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Automations.Models.Automations.MapChange;
using Estreya.BlishHUD.Automations.Models.Automations.PositionChange;
using Estreya.BlishHUD.Automations.Models.Automations.IntervalChange;
using Estreya.BlishHUD.Automations.Services;
using Estreya.BlishHUD.Shared.Services;
using Gw2Sharp.WebApi.V2.Models;
using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using HandlebarsDotNet.Helpers.Enums;
using Microsoft.Xna.Framework;
using Humanizer;
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

    protected override bool NeedsBackend => false;

    private MapChangeAutomationService MapChangeAutomationService { get; set; }
    private PositionChangeAutomationService PositionChangeAutomationService { get; set; }
    private IntervalChangeAutomationService IntervalChangeAutomationService { get; set; }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override async Task LoadAsync()
    {
        this.BuildHandlebarContext();
        await base.LoadAsync();

        MapChangeAutomationEntry mapChangeToQueensdale = new MapChangeAutomationEntry("test map change");

        mapChangeToQueensdale.AddAction((input) =>
        {
            HandlebarsTemplate<object, object> template = this._handleBarsContext.Compile("{{#if From}}Changed map from \"{{From.Name}}\" ({{From.Id}}) to \"{{To.Name}}\" ({{To.Id}}){{else}}Changed map to \"{{To.Name}}\" ({{To.Id}}){{/if}}.");
            ScreenNotification.ShowNotification(template.Invoke(input));
        });

        this.MapChangeAutomationService.AddEntry(mapChangeToQueensdale);

        PositionChangeAutomationEntry positionChange = new PositionChangeAutomationEntry("position change");

        positionChange.AddAction((input) =>
        {
            HandlebarsTemplate<object, object> template = this._handleBarsContext.Compile("{{#if From}}{{From}} -> {{To}}{{else}}{{To}}{{/if}}.");
            Shared.Controls.ScreenNotification.ShowNotification(template.Invoke(input));
        });

        this.PositionChangeAutomationService.AddEntry(positionChange);

        IntervalChangeAutomationEntry intervalChange = new IntervalChangeAutomationEntry("interval change");

        intervalChange.AddAction((input) =>
        {
            HandlebarsTemplate<object, object> template = this._handleBarsContext.Compile("{{#if From}}{{timespan-humanize From 2}} -> {{timespan-humanize To 2}}{{else}}{{To}}{{/if}}.");
            Shared.Controls.ScreenNotification.ShowNotification(template.Invoke(input));
        });

        this.IntervalChangeAutomationService.AddEntry(intervalChange);
    }

    private void BuildHandlebarContext()
    {
        this._handleBarsContext = Handlebars.Create();
        HandlebarsHelpers.Register(this._handleBarsContext, Category.Math, Category.Humanizer);

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

        this._handleBarsContext.RegisterHelper("humanize", (writer, context, parameters) =>
        {
            if (parameters.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters), "humanize: Minimum one parameter required.");
            }

            //if (parameters.Length > 1)
            //{
            //    throw new ArgumentOutOfRangeException(nameof(parameters), "humanize: Only one parameter supported.");
            //}

            var value = parameters[0];
            var precision = parameters.Length >= 2 ? (int)parameters[1] : 1;

            switch (value)
            {
                case string stringValue:
                    if (DateTime.TryParse(stringValue, out var parsedAsDateTime))
                    {
                        writer.Write(parsedAsDateTime.Humanize(), false);
                    }

                    if (TimeSpan.TryParse(stringValue, out var parsedAsTimeSpan))
                    {
                        writer.Write(parsedAsTimeSpan.Humanize(), false);
                    }

                    writer.Write(stringValue.Humanize(), false);
                    break;
                case DateTime dateTimeValue:
                    writer.Write(dateTimeValue.Humanize(), false);
                    break;
                case TimeSpan timeSpanValue:
                    writer.Write(timeSpanValue.Humanize(precision), false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"The value '{value}' is not supported in the Humanize(...) method.");
            }
        });

        this._handleBarsContext.RegisterHelper("timespan-humanize", (writer, context, parameters) =>
        {
            if (parameters.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters), "humanize: Minimum one parameter required.");
            }

            if (parameters.Length > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters), "humanize: Only two parameter supported.");
            }

            var value = parameters[0];
            var precision = parameters.Length >= 2 ? (int)parameters[1] : 1;
            if (precision <= 0) precision = 1;

            switch (value)
            {
                case string stringValue:
                    if (TimeSpan.TryParse(stringValue, out var parsedAsTimeSpan))
                    {
                        writer.Write(parsedAsTimeSpan.Humanize(precision), false);
                    }

                    writer.Write(stringValue.Humanize(), false);
                    break;
                case TimeSpan timeSpanValue:
                    writer.Write(timeSpanValue.Humanize(precision), false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"The value '{value}' is not supported in the timespan-humanize method.");
            }
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

        this.PositionChangeAutomationService = new PositionChangeAutomationService(new ServiceConfiguration()
        {
            AwaitLoading = false,
            Enabled = true
        }, this.GetFlurlClient(), this.Gw2ApiManager, this._handleBarsContext);

        this.IntervalChangeAutomationService = new IntervalChangeAutomationService(new ServiceConfiguration()
        {
            AwaitLoading = false,
            Enabled = true
        }, this.GetFlurlClient(), this.Gw2ApiManager, this._handleBarsContext);

        Collection<ManagedService> services = new Collection<ManagedService>
        {
            this.MapChangeAutomationService,
            this.PositionChangeAutomationService,
            this.IntervalChangeAutomationService
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