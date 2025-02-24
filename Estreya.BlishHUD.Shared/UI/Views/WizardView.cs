namespace Estreya.BlishHUD.Shared.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Services;
    using Estreya.BlishHUD.Shared.Threading.Events;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class WizardView : BaseView
    {
        protected WizardView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService) { }

        public event AsyncEventHandler CancelClicked;
        public event AsyncEventHandler NextClicked;
        public event AsyncEventHandler FinishClicked;
        public event AsyncEventHandler PreviousClicked;

        public bool NextAvailable { get; internal set; }
        public bool NextIsFinish { get; internal set; }
        public bool PreviousAvailable { get; internal set; }

        protected virtual bool TestConfigurationsAvailable => false;

        protected virtual Task OnNextClicked() => Task.CompletedTask;
        protected virtual Task OnPreviousClicked() => Task.CompletedTask;

        private async Task Next()
        {
            await this.ApplyConfigurations();
            await this.OnNextClicked();
            await (this.NextClicked?.Invoke(this) ?? Task.CompletedTask);
        }

        private async Task Finish()
        {
            await this.ApplyConfigurations();
            await this.OnNextClicked(); 
            await (this.FinishClicked?.Invoke(this) ?? Task.CompletedTask);
        }

        private async Task Previous()
        {
            await this.OnPreviousClicked();
            await (this.PreviousClicked?.Invoke(this) ?? Task.CompletedTask);
        }

        private async Task Cancel()
        {
            await (this.CancelClicked?.Invoke(this) ?? Task.CompletedTask);
        }

        public FlowPanel GetButtonPanel(Panel parent)
        {
            var panel =  new FlowPanel
            {
                Parent = parent,
                FlowDirection = ControlFlowDirection.SingleLeftToRight,
                HeightSizingMode = SizingMode.AutoSize,
                WidthSizingMode = SizingMode.AutoSize
            };

            var prevButton = this.RenderButtonAsync(panel, "Previous", async () =>
            {
                await this.Previous();
            });
            prevButton.Enabled = this.PreviousAvailable;

            var cancelButton = this.RenderButtonAsync(panel, "Skip Wizard", async () =>
            {
                await this.Cancel();
            });
            cancelButton.BasicTooltipText = "Skips the wizard.\nOnly following pages will be skipped.\nAll already completed pages will keep their applied settings.";

            if (this.TestConfigurationsAvailable)
            {
                var testConfigurationsButton = this.RenderButtonAsync(panel, "Test Configurations", async () =>
                {
                    await this.ApplyConfigurations();
                });
                testConfigurationsButton.BasicTooltipText = "Applies all options on the current page to the module.";
            }

            var nextButton = this.RenderButtonAsync(panel, !this.NextIsFinish ? "Next" : "Finish", async () =>
            {
                if (!this.NextIsFinish)
                {
                    await this.Next();
                }
                else
                {
                    await this.Finish();
                }
            });
            nextButton.Enabled = this.NextAvailable;

            panel.RecalculateLayout();
            panel.Update(GameService.Overlay.CurrentGameTime);

            return panel;
        }

        protected virtual Task ApplyConfigurations() => Task.CompletedTask;
    }
}
