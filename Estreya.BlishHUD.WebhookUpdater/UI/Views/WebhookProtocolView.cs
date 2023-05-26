namespace Estreya.BlishHUD.WebhookUpdater.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using Models;
using MonoGame.Extended.BitmapFonts;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Linq;
using System.Threading.Tasks;

public class WebhookProtocolView : BaseView
{
    private Webhook webhook;

    public WebhookProtocolView(Webhook webhook, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BitmapFont font = null) : base(apiManager, iconService, translationService, font)
    {
        this.webhook = webhook;
    }

    protected override void InternalBuild(Panel parent)
    {
        FlowPanel protocolStack = new FlowPanel
        {
            Parent = parent,
            OuterControlPadding = new Vector2(20, 20),
            Size = parent.ContentRegion.Size,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true
        };

        foreach (WebhookProtocol protocol in this.webhook.Configuration.Protocol.Value.OrderByDescending(p => p.TimestampUTC))
        {
            FlowPanel protocolInfo = new FlowPanel
            {
                Parent = protocolStack,
                Width = protocolStack.ContentRegion.Width - (20 * 2),
                ShowBorder = true,
                HeightSizingMode = SizingMode.AutoSize,
                FlowDirection = ControlFlowDirection.SingleTopToBottom
            };

            const int valueXLocation = 110;

            this.RenderLabel(protocolInfo, "Time:", protocol.TimestampUTC.ToLocalTime().ToString(), valueXLocation: valueXLocation);
            this.RenderLabel(protocolInfo, "Url:", protocol.Url, valueXLocation: valueXLocation);
            this.RenderLabel(protocolInfo, "Message:", protocol.Message, valueXLocation: valueXLocation);
            this.RenderLabel(protocolInfo, "Method:", protocol.Method.ToString(), valueXLocation: valueXLocation);
            this.RenderLabel(protocolInfo, "Content-Type:", protocol.ContentType, valueXLocation: valueXLocation);
            this.RenderLabel(protocolInfo, "Payload:", protocol.Payload, valueXLocation: valueXLocation);
            this.RenderLabel(protocolInfo, "Status Code:", protocol.StatusCode.ToString(), textColorValue: (int)protocol.StatusCode is >= 200 and < 400 ? Color.Green : Color.Red, valueXLocation: valueXLocation);
            this.RenderEmptyLine(protocolInfo);
            this.RenderLabel(protocolInfo, "Exception:");
            this.RenderLabel(protocolInfo, "Message:", protocol.Exception?.Message, valueXLocation: valueXLocation);
            this.RenderLabel(protocolInfo, "Stacktrace:", protocol.Exception?.Stacktrace, valueXLocation: valueXLocation);

            this.RenderEmptyLine(protocolInfo);

            FlowPanel buttonRow = new FlowPanel
            {
                Parent = protocolInfo,
                FlowDirection = ControlFlowDirection.LeftToRight,
                Width = protocolInfo.ContentRegion.Width,
                HeightSizingMode = SizingMode.AutoSize
            };

            this.RenderButtonAsync(buttonRow, "Copy Response Content", async () =>
            {
                await ClipboardUtil.WindowsClipboardService.SetTextAsync(protocol.Message);
            }, () => string.IsNullOrWhiteSpace(protocol.Message));

            this.RenderButtonAsync(buttonRow, "Copy Exception Message", async () =>
            {
                await ClipboardUtil.WindowsClipboardService.SetTextAsync(protocol.Exception?.Message);
            }, () => string.IsNullOrWhiteSpace(protocol.Exception?.Message));

            this.RenderButtonAsync(buttonRow, "Copy Exception Stacktrace", async () =>
            {
                await ClipboardUtil.WindowsClipboardService.SetTextAsync(protocol.Exception?.Stacktrace);
            }, () => string.IsNullOrWhiteSpace(protocol.Exception?.Stacktrace));

            this.RenderEmptyLine(protocolStack);
        }
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }

    protected override void Unload()
    {
        base.Unload();
        this.webhook = null;
    }
}