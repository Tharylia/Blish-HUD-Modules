namespace Estreya.BlishHUD.EventTable.UI.Views.Wizard;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class WizardWelcomeView : WizardView
{
    public WizardWelcomeView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService) { }

    protected override void InternalBuild(Panel parent)
    {
        var welcomeLbl = new FormattedLabelBuilder().SetWidth(parent.ContentRegion.Width).Wrap().AutoSizeHeight().SetHorizontalAlignment(HorizontalAlignment.Center)
            .CreatePart("Welcome to the Event Table Setup Wizard!", b => { b.SetFontSize(Blish_HUD.ContentService.FontSize.Size24); })
            .CreatePart("\n \n", b => { })
            .CreatePart("This wizard will take you through the most essential steps to get a base configuration for event table!", b => { b.SetFontSize(Blish_HUD.ContentService.FontSize.Size18); })
            .CreatePart("\n \n \n \n \n", b => { })
            .CreatePart("Please click on next if you are ready to start.", b => { b.SetFontSize(Blish_HUD.ContentService.FontSize.Size18); })
            .Build();
        welcomeLbl.Top = (int)(parent.ContentRegion.Height * 0.2f);
        welcomeLbl.Parent = parent;

        var buttons = this.GetButtonPanel(parent);

        buttons.Top = parent.ContentRegion.Bottom - 20 - buttons.Height;
        buttons.Left = parent.ContentRegion.Width /2 - buttons.Width/2;
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);
}
