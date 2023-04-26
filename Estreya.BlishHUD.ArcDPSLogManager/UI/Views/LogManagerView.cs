namespace Estreya.BlishHUD.ArcDPSLogManager.UI.Views
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Documents;

    public class LogManagerView : BaseView
    {
        private readonly ModuleSettings _moduleSettings;
        private readonly Func<List<string>> _getLogFiles;

        public LogManagerView(ModuleSettings moduleSettings, Func<List<string>> getLogFiles, Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
        {
            this._moduleSettings = moduleSettings;
            this._getLogFiles = getLogFiles;
        }

        protected override void InternalBuild(Panel parent)
        {
            var logList = new FlowPanel()
            {
                Parent = parent,
                FlowDirection = ControlFlowDirection.SingleTopToBottom
            };

            var logFiles = _getLogFiles();
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
}
