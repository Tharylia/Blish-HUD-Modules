namespace Estreya.BlishHUD.Shared.UI.Views
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Services;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public class ContainerView : BaseView
    {
        public ContainerView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService)
        {
        }

        protected override void InternalBuild(Panel parent) { }

        public void Add(Control control)
        {
            control.Parent = this.MainPanel;
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);
    }
}
