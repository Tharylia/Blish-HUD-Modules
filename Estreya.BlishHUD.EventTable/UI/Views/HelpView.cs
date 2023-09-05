namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Models;
using MonoGame.Extended.BitmapFonts;
using Shared.Extensions;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class HelpView : BaseView
{
    private const string DISCORD_USERNAME = "Estreya#0001";
    private static readonly Point PADDING = new Point(25, 25);
    private readonly string _apiUrl;
    private readonly Func<List<EventCategory>> _getEvents;
    private readonly List<string> _autocompleteAPIKeys = new List<string>();

    public HelpView(Func<List<EventCategory>> getEvents, string apiUrl, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService)
    {
        this._getEvents = getEvents;
        this._apiUrl = apiUrl;
    }

    protected override void InternalBuild(Panel parent)
    {
        FlowPanel flowPanel = new FlowPanel
        {
            Parent = parent,
            Width = parent.ContentRegion.Width - (PADDING.X * 2),
            Height = parent.ContentRegion.Height - (PADDING.Y * 2),
            Location = new Point(PADDING.X, PADDING.Y),
            CanScroll = true,
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        this.BuildEditEventSection(flowPanel);
        this.RenderEmptyLine(flowPanel);
        this.BuildSettingSliderSection(flowPanel);
        this.RenderEmptyLine(flowPanel);
        this.BuildNoEventsSection(flowPanel);
        this.RenderEmptyLine(flowPanel);
        this.BuildAutocompleteEventSection(flowPanel);
        this.RenderEmptyLine(flowPanel);
        this.BuildOnlyRemindersSection(flowPanel);
        this.RenderEmptyLine(flowPanel);
        this.BuildDeletedEventAreaSection(flowPanel);
        this.RenderEmptyLine(flowPanel);
        this.BuildLocalizationSection(flowPanel);
        this.RenderEmptyLine(flowPanel);
        this.BuildMapMovementDoesNotWorkSection(flowPanel);

        this.RenderEmptyLine(flowPanel);
        this.BuildQuestionNotFoundSection(flowPanel);
    }

    private void BuildAutocompleteEventSection(FlowPanel parent)
    {
        Panel autocompleteEventsPanel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart("What events are qualified for autocompletion?", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                 .CreatePart("\n \n", builder => { });

        List<EventCategory> events = this._getEvents();

        foreach (string apiKey in this._autocompleteAPIKeys)
        {
            IEnumerable<Event> eventsFromAPIKey = events.SelectMany(ec => ec.Events).Where(ev => ev.APICode == apiKey).DistinctBy(ev => ev.Key);
            foreach (Event ev in eventsFromAPIKey)
            {
                labelBuilder.CreatePart($"- {ev.Name}", builder => { })
                            .CreatePart("\n", builder => { });
            }
        }

        labelBuilder
            .CreatePart("\n \n", builder => { })
            .CreatePart("All other events ", builder => { })
            .CreatePart("CAN'T", builder => { builder.MakeBold().MakeUnderlined(); })
            .CreatePart(" be autocompleted as they are not available in the API.", builder => { });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = autocompleteEventsPanel;
    }

    private void BuildSettingSliderSection(FlowPanel parent)
    {
        Panel panel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart("Why are my setting sliders moving when I move others?", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("Some sliders have a connection. For example:", builder => { })
                                                 .CreatePart("\n", builder => { })
                                                 .CreatePart("If you make the table bigger, it can't be moved as far to the right.", builder => { });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = panel;
    }

    private void BuildEditEventSection(FlowPanel parent)
    {
        Panel panel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart("Can I edit the event names or colors?", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("No.", builder => { builder.MakeBold(); })
                                                 .CreatePart("\n", builder => { })
                                                 .CreatePart("All events are loaded in the backend from the following file: ", builder => { })
                                                 .CreatePart("CLICK HERE", builder => { builder.SetHyperLink("https://files.estreya.de/blish-hud/event-table/v1/events.json"); });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = panel;
    }

    private void BuildDeletedEventAreaSection(FlowPanel parent)
    {
        Panel panel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart("I deleted my event area. Can I recover it?", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("No.", builder => { builder.MakeBold(); });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = panel;
    }

    private void BuildNoEventsSection(FlowPanel parent)
    {
        Panel panel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart("Why can't I see any events?", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("There could be multiple reasons for this:", builder => { })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("- You have deactivated all events for your event area.", builder => { })
                                                 .CreatePart("\n", builder => { })
                                                 .CreatePart("- The backend service is currently not available.", builder => { })
                                                 .CreatePart("\n    You can check the following url (if it responds, the backend service is fine): ", builder => { })
                                                 .CreatePart("CLICK HERE", builder => { builder.SetHyperLink($"{this._apiUrl.TrimEnd('/')}/events"); })
                                                 .CreatePart("\n", builder => { })
                                                 .CreatePart("- You have abused the backend and are currently ratelimited (same result as the above answer).", builder => { })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart($"In case you can't figure it out, ping {DISCORD_USERNAME} on BlishHUD Discord.", builder => { builder.MakeBold(); });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = panel;
    }

    private void BuildMapMovementDoesNotWorkSection(FlowPanel parent)
    {
        Panel panel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart("Why does the automatic map movement not work?", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("The movement of the map is a complex mechanic that is very sensitive to wrong inputs.", builder => { })
                                                 .CreatePart("\n", builder => { })
                                                 .CreatePart("As far as I could analyse, there are some systems/thrid party programs that tend to mess with the movement.", builder => { })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("Can I do something about that?", builder => { builder.MakeBold(); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("No, not yet.", builder => { });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = panel;
    }

    private void BuildLocalizationSection(FlowPanel parent)
    {
        Panel panel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart("Why are some elements not translated?", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("Translation is an ongoing process. Most elements will get their translation at some point.", builder => { })
                                                 .CreatePart("\n", builder => { })
                                                 .CreatePart("If you are missing a specific translation over a long period, please ping ", builder => { })
                                                 .CreatePart(DISCORD_USERNAME, builder => { builder.MakeBold(); })
                                                 .CreatePart(" on BlishHUD Discord.", builder => { });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = panel;
    }

    private void BuildOnlyRemindersSection(FlowPanel parent)
    {
        Panel panel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart("I don't need the table. Can I just have reminders?", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("Yes, you can.", builder => { })
                                                 .CreatePart("\n", builder => { })
                                                 .CreatePart("Disable the only event area you have.", builder => { })
                                                 .CreatePart("\n", builder => { })
                                                 .CreatePart("Don't delete it! It will be back on the next restart.", builder => { });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = panel;
    }

    private void BuildQuestionNotFoundSection(FlowPanel parent)
    {
        Panel panel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart("My question was not listed?", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("Ping ", builder => { })
                                                 .CreatePart(DISCORD_USERNAME, builder => { builder.MakeBold(); })
                                                 .CreatePart(" on BlishHUD Discord.", builder => { });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = panel;
    }

    private FormattedLabelBuilder GetLabelBuilder(Panel parent)
    {
        return new FormattedLabelBuilder().SetWidth(parent.ContentRegion.Width - (PADDING.X * 2)).AutoSizeHeight().SetVerticalAlignment(VerticalAlignment.Top);
    }

    protected override async Task<bool> InternalLoad(IProgress<string> progress)
    {
        IApiV2ObjectList<MapChest> mapchests = await this.APIManager.Gw2ApiClient.V2.MapChests.AllAsync();

        IApiV2ObjectList<WorldBoss> worldbosses = await this.APIManager.Gw2ApiClient.V2.WorldBosses.AllAsync();

        this._autocompleteAPIKeys.AddRange(mapchests.Select(m => m.Id));

        this._autocompleteAPIKeys.AddRange(worldbosses.Select(w => w.Id));

        return true;
    }
}