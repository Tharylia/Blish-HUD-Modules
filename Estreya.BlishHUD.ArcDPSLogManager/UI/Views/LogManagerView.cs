namespace Estreya.BlishHUD.ArcDPSLogManager.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using MonoGame.Extended.BitmapFonts;
using Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LogManagerView : BaseView
{
    private readonly Func<List<string>> _getLogFiles;
    private readonly ModuleSettings _moduleSettings;

    public LogManagerView(ModuleSettings moduleSettings, Func<List<string>> getLogFiles, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BitmapFont font = null) : base(apiManager, iconService, translationService, font)
    {
        this._moduleSettings = moduleSettings;
        this._getLogFiles = getLogFiles;
    }

    protected override void InternalBuild(Panel parent)
    {
        FlowPanel logList = new FlowPanel
        {
            Parent = parent,
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        List<string> logFiles = this._getLogFiles();
    }

    public void Rebuild()
    {
        this.InternalBuild(this.MainPanel);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}