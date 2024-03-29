﻿namespace Estreya.BlishHUD.FoodReminder.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using Shared.UI.Views;
using Shared.Services;
using System;
using System.Threading.Tasks;

public class GeneralSettingsView : BaseSettingsView
{
    private readonly ModuleSettings _moduleSettings;

    public GeneralSettingsView(ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService) : base(apiManager, iconService, translationService, settingEventService)
    {
        this._moduleSettings = moduleSettings;
    }

    protected override void BuildView(FlowPanel parent)
    {
        this.RenderBoolSetting(parent, this._moduleSettings.GlobalDrawerVisible);
        this.RenderKeybindingSetting(parent, this._moduleSettings.GlobalDrawerVisibleHotkey);
        this.RenderBoolSetting(parent, this._moduleSettings.RegisterCornerIcon);
        this.RenderEnumSetting(parent, this._moduleSettings.CornerIconLeftClickAction);
        this.RenderEnumSetting(parent, this._moduleSettings.CornerIconRightClickAction);

        this.RenderEmptyLine(parent);

        FlowPanel visibilityOptionGroup = new FlowPanel
        {
            Parent = parent,
            Width = MathHelper.Clamp((int)(parent.ContentRegion.Width * 0.55), 0, parent.ContentRegion.Width),
            HeightSizingMode = SizingMode.AutoSize,
            OuterControlPadding = new Vector2(10, 20),
            ShowBorder = true,
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        FormattedLabel lbl = new FormattedLabelBuilder().SetWidth(visibilityOptionGroup.ContentRegion.Width - 20).AutoSizeHeight().Wrap()
                                                        .CreatePart("These options are global. The individual area options have priority and will hide it if any matches!", builder =>
                                                        {
                                                            builder.MakeBold().SetFontSize(ContentService.FontSize.Size18);
                                                        }).Build();
        lbl.Parent = visibilityOptionGroup;

        this.RenderEmptyLine(visibilityOptionGroup, 10);

        this.RenderBoolSetting(visibilityOptionGroup, this._moduleSettings.HideOnMissingMumbleTicks);
        this.RenderBoolSetting(visibilityOptionGroup, this._moduleSettings.HideOnOpenMap);
        this.RenderBoolSetting(visibilityOptionGroup, this._moduleSettings.HideInCombat);
        this.RenderBoolSetting(visibilityOptionGroup, this._moduleSettings.HideInPvE_OpenWorld);
        this.RenderBoolSetting(visibilityOptionGroup, this._moduleSettings.HideInPvE_Competetive);
        this.RenderBoolSetting(visibilityOptionGroup, this._moduleSettings.HideInWvW);
        this.RenderBoolSetting(visibilityOptionGroup, this._moduleSettings.HideInPvP);
        this.RenderEmptyLine(visibilityOptionGroup, 20);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}