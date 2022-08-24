namespace Estreya.BlishHUD.Shared.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;

    public class StateSettingsView : BaseSettingsView
    {
        private readonly Collection<ManagedState> _stateList;

        public StateSettingsView(Collection<ManagedState> stateList, Gw2ApiManager apiManager, IconState iconState, BitmapFont font = null) : base(apiManager, iconState,font)
        {
            this._stateList = stateList;
        }

        protected override void BuildView(Panel parent)
        {
            foreach (ManagedState state in _stateList)
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
