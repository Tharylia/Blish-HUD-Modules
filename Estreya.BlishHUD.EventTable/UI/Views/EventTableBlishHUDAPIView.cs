namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Models.BlishHudAPI;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Helpers;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.CodeDom;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

public class EventTableBlishHUDAPIView : BlishHUDAPIView
{
    private const string DASHBOARD_URL = "https://blish-hud.estreya.de";

    protected override bool DrawKofiStatus => false;

    public EventTableBlishHUDAPIView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BlishHudApiService blishHudApiService, IFlurlClient flurlClient) : base(apiManager, iconService, translationService, blishHudApiService, flurlClient)
    {
    }

    protected override FormattedLabelBuilder GetDescriptionBuilder()
    {
        return new FormattedLabelBuilder()
            .CreatePart("Login to use the custom events you created ", b => { })
            .CreatePart("here.", b => b.SetHyperLink(DASHBOARD_URL))
            .CreatePart("\n \n", b => { })
            .CreatePart("The created events will show up in your areas/tables, if not disabled", b=> { });
    }
}