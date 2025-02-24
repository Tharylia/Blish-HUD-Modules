namespace Estreya.BlishHUD.EventTable.UI.Views.Wizard;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class WizardAreasView : WizardView
{
    private bool _useAreas;
    private bool _useFillers;

    private readonly List<EventAreaConfiguration> _allAreas;

    protected override bool TestConfigurationsAvailable => true;

    public WizardAreasView(List<EventAreaConfiguration> allAreas, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService)
    {
        this._allAreas = allAreas;
    }

    protected override void InternalBuild(Panel parent)
    {
        var welcomeLbl = new FormattedLabelBuilder().SetWidth(parent.ContentRegion.Width).Wrap().AutoSizeHeight().SetHorizontalAlignment(HorizontalAlignment.Center)
            .CreatePart("Event Areas", b => { b.SetFontSize(Blish_HUD.ContentService.FontSize.Size24); })
            .CreatePart("\n \n", b => { })
            .CreatePart("You are probably already seeing your first area created and ready to use on your screen.", b => { b.SetFontSize(Blish_HUD.ContentService.FontSize.Size18); })
            .CreatePart("\n \n", b => { })
            .CreatePart("Please change the settings below to fit your needs regarding event areas.", b => { b.SetFontSize(Blish_HUD.ContentService.FontSize.Size18); })
            .Build();
        welcomeLbl.Top = (int)(parent.ContentRegion.Height * 0.1f);
        welcomeLbl.Parent = parent;

        var useAreasLbl = this.RenderLabel(parent, "Use Areas:").TitleLabel;
        useAreasLbl.Parent = parent;
        useAreasLbl.AutoSizeWidth = false;
        useAreasLbl.Width = this.LABEL_WIDTH;
        useAreasLbl.Top = welcomeLbl.Bottom + 100;
        useAreasLbl.Left = 150;

        this._useAreas = this._allAreas.Any(a => a.Enabled.Value);
        var useAreasCheckbox = this.RenderCheckbox(parent, new Microsoft.Xna.Framework.Point(useAreasLbl.Right + 20, useAreasLbl.Top), this._useAreas, onChangeAction: val =>
        {
            this._useAreas = val;
        });
        useAreasCheckbox.BasicTooltipText = "Check this option if you would like to keep the already created area.\nUncheck this option, if you only want to use the reminder feature of this module.";

        var useFillersLbl = this.RenderLabel(parent, "Use Filler Events:").TitleLabel;
        useFillersLbl.Parent = parent;
        useFillersLbl.AutoSizeWidth = false;
        useFillersLbl.Width = this.LABEL_WIDTH;
        useFillersLbl.Top = useAreasLbl.Bottom + 5;
        useFillersLbl.Left = 150;

        this._useFillers = this._allAreas.Any(a => a.UseFiller.Value);
        var useFillersCheckbox = this.RenderCheckbox(parent, new Microsoft.Xna.Framework.Point(useFillersLbl.Right + 20, useFillersLbl.Top), this._useFillers, onChangeAction: val =>
        {
            this._useFillers = val;
        });
        useFillersCheckbox.BasicTooltipText = "Check this option if you would like to have events fill in the gaps between regular events and show the remaining time until they start.";

        var buttons = this.GetButtonPanel(parent);

        buttons.Top = parent.ContentRegion.Bottom - 20 - buttons.Height;
        buttons.Left = parent.ContentRegion.Width / 2 - buttons.Width / 2;
    }

    protected override Task ApplyConfigurations()
    {
        // If first start, it should only be one area anyway.
        this._allAreas.ForEach(a => a.Enabled.Value = this._useAreas);
        this._allAreas.ForEach(a => a.UseFiller.Value = this._useFillers);

        return Task.CompletedTask;
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);
}
