namespace Estreya.BlishHUD.EventTable.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.EventTable.State;
    using Microsoft.Xna.Framework;
    using System;
    using System.Threading.Tasks;

    public class DebugSettingsView : BaseSettingsView
    {
        public DebugSettingsView(ModuleSettings settings) : base(settings)
        {
        }

        protected override void BuildView(Panel parent)
        {
            foreach (ManagedState state in EventTableModule.ModuleInstance.States)
            {
                this.RenderLabel(parent, $"{state.GetType().Name} running:", state.Running.ToString(), textColorValue: state.Running ? Color.Green : Color.Red);
            }
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
