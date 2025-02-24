namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Controls.Input;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

public class GeneralSettingsView : BaseSettingsView
{
    private readonly ModuleSettings _moduleSettings;
    private readonly MetricsService _metricsService;

    public GeneralSettingsView(ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, MetricsService metricsService) : base(apiManager, iconService, translationService, settingEventService)
    {
        this._moduleSettings = moduleSettings;
        this._metricsService = metricsService;
    }

    protected override void BuildView(FlowPanel parent)
    {
        this.RenderBoolSetting(parent, this._moduleSettings.GlobalDrawerVisible);
        this.RenderKeybindingSetting(parent, this._moduleSettings.GlobalDrawerVisibleHotkey);
        this.RenderBoolSetting(parent, this._moduleSettings.RegisterCornerIcon);
        this.RenderEnumSetting(parent, this._moduleSettings.CornerIconLeftClickAction);
        this.RenderEnumSetting(parent, this._moduleSettings.CornerIconRightClickAction);

        this.RenderEmptyLine(parent);

        this.RenderBoolSetting(parent, this._moduleSettings.RegisterContext);
        //this.RenderButtonAsync(parent, "Change Metrics Consent", async () =>
        //{
        //    await this._metricsService.AskMetricsConsent(true);
        //});

        this.RenderEmptyLine(parent);

        this.RenderKeybindingSetting(parent, this._moduleSettings.MapKeybinding);

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
                                                        .CreatePart(this.TranslationService.GetTranslation("generalSettingsView-uiVisibilityWarning", "These options are global. The individual area options have priority and will hide it if any matches!"), builder =>
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

        this.RenderEmptyLine(parent);

        this.RenderEnumSetting(parent, this._moduleSettings.MenuEventSortMode);

        this.RenderEmptyLine(parent);

        this.RenderDevelopmentGroup(parent);

        this.RenderEmptyLine(parent);
    }

    private void RenderDevelopmentGroup(FlowPanel parent)
    {
        var groupPanel = new FlowPanel()
        {
            Parent = parent,
            OuterControlPadding = new Vector2(20,20),
            ShowBorder = true,
            Title = "Development",
            CanCollapse = true,
            Collapsed = true,
            HeightSizingMode =SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - (int)(parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        var lblBuilder = new FormattedLabelBuilder().SetWidth(groupPanel.ContentRegion.Width).AutoSizeHeight().SetVerticalAlignment(VerticalAlignment.Top);
        lblBuilder.CreatePart("Using the Development API happens on your own risk!", b => { b.MakeBold().SetFontSize(ContentService.FontSize.Size24); })
            .CreatePart("\n \n", b => { })
            .CreatePart("The API can be offline or in a broken state for your module version at any given time!", b => { })
            .CreatePart("\n", b => { })
            .CreatePart("This could lead to you not being able to use the module at all to reset this setting via the UI.", b => { })
            .CreatePart("\n \n", b => { })
            .CreatePart($"In this case open the settings.json file and set the option \"{this._moduleSettings.UseDevelopmentAPI.EntryKey}\" back to false.", b => { })
            .CreatePart("\n \n", b => { })
            .CreatePart("Changing this option needs a complete restart of BlishHUD!", b => { b.MakeItalic(); });

        var lbl = lblBuilder.Build();
        lbl.Parent = groupPanel;

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, this._moduleSettings.UseDevelopmentAPI, async (oldVal, newVal) =>
        {
            if (!newVal) return true;

            var confirmDialog = new ConfirmDialog(
                "Activating Development API", 
                "You are in the process of enabling the development api.\n\nThis API can be offline at any time preventing you from using the module!", 
                this.IconService);

            var result = await confirmDialog.ShowDialog();

            return result == System.Windows.Forms.DialogResult.OK;
        });

        this.RenderEmptyLine(groupPanel, (int)groupPanel.OuterControlPadding.Y);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}