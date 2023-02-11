namespace Estreya.BlishHUD.Shared.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Modules;
    using Estreya.BlishHUD.Shared.Settings;
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
        private readonly Func<Task> _reloadCalledAction;

        public StateSettingsView(Collection<ManagedState> stateList, Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, SettingEventState settingEventState, BitmapFont font = null, Func<Task> reloadCalledAction = null) : base(apiManager, iconState,translationState, settingEventState, font)
        {
            this._stateList = stateList;
            this._reloadCalledAction = reloadCalledAction;
        }

        protected override void BuildView(FlowPanel parent)
        {
            foreach (ManagedState state in _stateList)
            {
                if (state.GetType().BaseType.IsGenericType && state.GetType().BaseType.GetGenericTypeDefinition() == typeof(APIState<>))
                {
                    var loading = (bool)state.GetType().GetProperty(nameof(APIState<object>.Loading)).GetValue(state);
                    var finished = state.Running && !loading;
                    this.RenderLabel(parent, $"{state.GetType().Name} running & loaded:", finished.ToString(), textColorValue: finished ? Color.Green : Color.Red);
                } else
                {
                this.RenderLabel(parent, $"{state.GetType().Name} running:", state.Running.ToString(), textColorValue: state.Running ? Color.Green : Color.Red);
                }
            }

            if (_reloadCalledAction != null)
            {
                this.RenderEmptyLine(parent);
                this.RenderEmptyLine(parent);

                this.RenderButtonAsync(parent, "Reload", _reloadCalledAction);
            }
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
