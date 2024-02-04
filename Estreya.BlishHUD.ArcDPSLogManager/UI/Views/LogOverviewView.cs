namespace Estreya.BlishHUD.ArcDPSLogManager.UI.Views;

using Blish_HUD.Controls;
using Shared.UI.Views;
using Estreya.BlishHUD.ArcDPSLogManager.Models;
using Humanizer;
using System;
using System.Threading.Tasks;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Services;
using Microsoft.Xna.Framework;

public class LogOverviewView : BaseView
{
    private readonly LogData _logData;
    private Label _titleLabel;

    public LogOverviewView(LogData logData, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService)
    {
        this._logData = logData;
    }

    protected override void InternalBuild(Panel parent)
    {
        parent.BackgroundColor = Color.Red;
        this._titleLabel = this.RenderLabel(parent, this._logData.GetLogTitle()).TitleLabel;

        this.RenderEmptyLine(parent);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}