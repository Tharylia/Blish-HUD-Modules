namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Services;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    public class GeneralSettingsView : BaseSettingsView
    {
        private readonly ModuleSettings _moduleSettings;

        public GeneralSettingsView(ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, BitmapFont font = null) : base(apiManager, iconService, translationService, settingEventService, font)
        {
            this._moduleSettings = moduleSettings;
        }

        protected override void BuildView(FlowPanel parent)
        {
            this.RenderBoolSetting(parent, _moduleSettings.GlobalDrawerVisible);
            this.RenderKeybindingSetting(parent, _moduleSettings.GlobalDrawerVisibleHotkey);
            this.RenderBoolSetting(parent, _moduleSettings.RegisterCornerIcon);
            this.RenderEnumSetting(parent, _moduleSettings.CornerIconLeftClickAction);
            this.RenderEnumSetting(parent, _moduleSettings.CornerIconRightClickAction);

            this.RenderEmptyLine(parent);

            this.RenderKeybindingSetting(parent, _moduleSettings.MapKeybinding);

            this.RenderEmptyLine(parent);

            var visibilityOptionGroup = new FlowPanel()
            {
                Parent = parent,
                Width = MathHelper.Clamp((int)(parent.ContentRegion.Width *0.55), 0, parent.ContentRegion.Width),
                HeightSizingMode = SizingMode.AutoSize,
                OuterControlPadding = new Vector2(10,20),
                ShowBorder = true,
                FlowDirection = ControlFlowDirection.SingleTopToBottom
            };

            var lbl = new FormattedLabelBuilder().SetWidth(visibilityOptionGroup.ContentRegion.Width - 20).AutoSizeHeight().Wrap()
                .CreatePart(this.TranslationService.GetTranslation("generalSettingsView-uiVisibilityWarning", "These options are global. The individual area options have priority and will hide it if any matches!"), builder =>
                {
                    builder.MakeBold().SetFontSize(Blish_HUD.ContentService.FontSize.Size18);
                }).Build();
            lbl.Parent = visibilityOptionGroup;

            this.RenderEmptyLine(visibilityOptionGroup, 10);

            this.RenderBoolSetting(visibilityOptionGroup, _moduleSettings.HideOnMissingMumbleTicks);
            this.RenderBoolSetting(visibilityOptionGroup, _moduleSettings.HideOnOpenMap);
            this.RenderBoolSetting(visibilityOptionGroup, _moduleSettings.HideInCombat);
            this.RenderBoolSetting(visibilityOptionGroup, _moduleSettings.HideInPvE_OpenWorld);
            this.RenderBoolSetting(visibilityOptionGroup, _moduleSettings.HideInPvE_Competetive);
            this.RenderBoolSetting(visibilityOptionGroup, _moduleSettings.HideInWvW);
            this.RenderBoolSetting(visibilityOptionGroup, _moduleSettings.HideInPvP);
            this.RenderEmptyLine(visibilityOptionGroup, 20);

            this.RenderEmptyLine(parent);

            this.RenderEnumSetting(parent, _moduleSettings.MenuEventSortMode);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
