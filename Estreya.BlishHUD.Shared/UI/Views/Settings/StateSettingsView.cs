namespace Estreya.BlishHUD.Shared.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Modules;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.Services;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using System.CodeDom;
    using System.Collections.Generic;

    public class ServiceSettingsView : BaseSettingsView
    {
        private readonly Collection<ManagedService> _stateList;
        private readonly Func<Task> _reloadCalledAction;

        public ServiceSettingsView(Collection<ManagedService> stateList, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, BitmapFont font = null, Func<Task> reloadCalledAction = null) : base(apiManager, iconService,translationService, settingEventService, font)
        {
            this._stateList = stateList;
            this._reloadCalledAction = reloadCalledAction;
        }

        protected override void BuildView(FlowPanel parent)
        {
            foreach (ManagedService state in _stateList)
            {
                List<Type> baseTypes = new List<Type>();

                var baseType = state.GetType().BaseType;

                while(baseType != null)
                {
                    if (baseType.IsGenericType)
                    {
                        baseTypes.Add(baseType.GetGenericTypeDefinition());
                    }
                    else
                    {
                        baseTypes.Add(baseType);
                    }

                    baseType = baseType.BaseType;
                }

                if (state.GetType().BaseType.IsGenericType && baseTypes.Contains(typeof(APIService<>)))
                {
                    var loading = (bool)state.GetType().GetProperty(nameof(APIService<object>.Loading)).GetValue(state);
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
