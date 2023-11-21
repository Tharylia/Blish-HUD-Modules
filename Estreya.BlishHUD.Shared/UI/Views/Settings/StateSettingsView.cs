namespace Estreya.BlishHUD.Shared.UI.Views.Settings;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Models;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

public class ServiceSettingsView : BaseSettingsView
{
    private const int LABEL_VALUE_X_LOCATION = 200;
    private readonly Func<Task> _reloadCalledAction;
    private readonly IEnumerable<ManagedService> _stateList;

    public ServiceSettingsView(IEnumerable<ManagedService> stateList, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, Func<Task> reloadCalledAction = null) : base(apiManager, iconService, translationService, settingEventService)
    {
        this._stateList = stateList;
        this._reloadCalledAction = reloadCalledAction;
    }

    protected override Dictionary<ControlType, BitmapFont> ControlFonts {
        get
        {
            var fonts = base.ControlFonts;
            fonts[ControlType.Label] = GameService.Content.DefaultFont18;

            return fonts;
        }
    }

    protected override void BuildView(FlowPanel parent)
    {
        parent.CanScroll = true;
        foreach (ManagedService state in this._stateList)
        {
            this.RenderState(state, parent);
        }

        if (this._reloadCalledAction != null)
        {
            this.RenderEmptyLine(parent);
            this.RenderEmptyLine(parent);

            this.RenderButtonAsync(parent, "Reload", this._reloadCalledAction);
        }
    }

    private void RenderState(ManagedService managedService, FlowPanel parent)
    {
        var isAPIState = this.IsAPIState(managedService);

        var title = managedService.GetType().Name;
        if (isAPIState) title += " - API State";

        FlowPanel stateGroup = new FlowPanel()
        {
            Parent = parent,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            HeightSizingMode = SizingMode.AutoSize,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            Title = title
        };
        stateGroup.Width = parent.ContentRegion.Width - (int)stateGroup.OuterControlPadding.X * 2;

        var configuration = (ServiceConfiguration)managedService.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Last(x => x.Name == "Configuration").GetValue(managedService);
        this.RenderLabel(stateGroup, "Configuration:", JsonConvert.SerializeObject(configuration,Formatting.Indented, new JsonSerializerSettings
        {
            Converters = new JsonConverter[]
            {
                new StringEnumConverter()
            }
        }), valueXLocation: LABEL_VALUE_X_LOCATION);

        this.RenderLabel(stateGroup, "Running:", managedService.Running.ToString(), textColorValue: managedService.Running ? Color.Green : Color.Red, valueXLocation: LABEL_VALUE_X_LOCATION);

        if (isAPIState)
        {
            bool loading = (bool)managedService.GetType().GetProperty(nameof(APIService<object>.Loading)).GetValue(managedService);
            bool finished = managedService.Running && !loading;
            this.RenderLabel(stateGroup, $"Loading finished:", finished.ToString(), textColorValue: finished ? Color.Green : Color.Red, valueXLocation: LABEL_VALUE_X_LOCATION);

            string progressText = (string)managedService.GetType().GetProperty(nameof(APIService<object>.ProgressText)).GetValue(managedService);
            this.RenderLabel(stateGroup, $"Progress Text:", progressText, valueXLocation: LABEL_VALUE_X_LOCATION);
        }
        else
        {
        }

        this.RenderEmptyLine(stateGroup, (int)stateGroup.OuterControlPadding.X);
    }

    private bool IsAPIState(ManagedService managedService)
    {
        List<Type> baseTypes = new List<Type>();

        Type baseType = managedService.GetType().BaseType;

        while (baseType != null)
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

        return managedService.GetType().BaseType.IsGenericType && baseTypes.Contains(typeof(APIService<>));
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}