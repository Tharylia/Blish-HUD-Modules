namespace Estreya.BlishHUD.EventTable.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using System;
    using System.Threading.Tasks;

    public class GraphicsSettingsView : BaseSettingsView
    {
        public GraphicsSettingsView(ModuleSettings settings) : base(settings)
        {
        }

        protected override void BuildView(Panel parent)
        {
            this.RenderSetting(parent, this.ModuleSettings.LocationX);
            this.RenderSetting(parent, this.ModuleSettings.LocationY);
            this.RenderSetting(parent, this.ModuleSettings.Width);
            this.RenderSetting(parent, this.ModuleSettings.EventHeight);
            this.RenderEmptyLine(parent);
            this.RenderSetting(parent, this.ModuleSettings.Opacity);
            this.RenderSetting(parent, this.ModuleSettings.EventFontSize);
            this.RenderEmptyLine(parent);
            this.RenderSetting(parent, this.ModuleSettings.BackgroundColorOpacity);
            this.RenderColorSetting(parent, this.ModuleSettings.BackgroundColor);
            this.RenderEmptyLine(parent);
            this.RenderSetting(parent, this.ModuleSettings.RefreshRateDelay);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
