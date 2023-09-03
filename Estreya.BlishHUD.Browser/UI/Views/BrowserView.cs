namespace Estreya.BlishHUD.Browser.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Browser.Controls;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.UI.Views;
using Humanizer;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Threading.Tasks;

public class BrowserView : BaseView
{
    private readonly Func<string> _getHomepage;
    private BrowserControl _browserControl;

    public BrowserView(Func<string> getHomepage, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BitmapFont font = null) : base(apiManager, iconService, translationService, font)
    {
        this._getHomepage = getHomepage;
    }

    protected override void InternalBuild(Panel parent)
    {
        FlowPanel flowPanel = new FlowPanel()
        {
            Parent = parent,
            Size = parent.ContentRegion.Size,
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        FlowPanel navigation = new FlowPanel()
        {
            Parent = flowPanel,
            Width = parent.ContentRegion.Width,
            Height = 30,
            FlowDirection = ControlFlowDirection.SingleLeftToRight,
        };

        this._browserControl ??= new BrowserControl(this._getHomepage())
        {
            Parent = flowPanel,
            Width = flowPanel.ContentRegion.Width,
            Height = flowPanel.ContentRegion.Height - navigation.Bottom,
        };

        Button backButton = this.RenderButton(navigation, "Back", this._browserControl.HandleBackNavigation);
        Button forwardButton = this.RenderButton(navigation, "Forward", this._browserControl.HandleForwardNavigation);
        TextBox addressBar = this.RenderTextbox(navigation, Point.Zero, 400, this._browserControl.GetCurrentAddress(), string.Empty, onEnterAction: address =>
        {
            var result = AsyncHelper.RunSync(async () => await this._browserControl.HandleAddressChange(address));

            if (result.Success) return;

            if (!address.StartsWith("http"))
            {
                AsyncHelper.RunSync(async () => await this._browserControl.HandleAddressChange($"https://google.com/search?q={address}"));
            }
            else
            {

                this.ShowError(result.ErrorCode.Humanize(), 10_000);
            }
        });

        this._browserControl.AddressChanged += (s, e) =>
        {
            addressBar.Text = e.Address;
        };
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }

    protected override void Unload()
    {
        base.Unload();
        this._browserControl?.Dispose();
        this._browserControl = null;
    }
}
