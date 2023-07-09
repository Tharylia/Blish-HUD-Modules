namespace Estreya.BlishHUD.FoodReminder;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework.Input;
using Models;
using Shared.Models.Drawers;
using Shared.Settings;
using System.Collections.Generic;

public class ModuleSettings : BaseModuleSettings
{
    public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(ModifierKeys.Alt, Keys.F))
    {
    }

    public SettingEntry<List<string>> OverviewNames { get; set; }

    protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
    {
        this.OverviewNames = globalSettingCollection.DefineSetting(nameof(this.OverviewNames), new List<string>());
    }

    public OverviewDrawerConfiguration AddDrawer(string name)
    {
        DrawerConfiguration drawer = base.AddDrawer(name);

        SettingEntry<float> columnSizeName = this.DrawerSettings.DefineSetting($"{name}-columnSize-name", 100f, () => "Name Column Size", () => "Defines the width of the column name.");
        columnSizeName.SetRange(20, 300);

        SettingEntry<float> columnSizeFood = this.DrawerSettings.DefineSetting($"{name}-columnSize-food", 100f, () => "Food Column Size", () => "Defines the width of the column food.");
        columnSizeFood.SetRange(20, 300);

        SettingEntry<float> columnSizeUtility = this.DrawerSettings.DefineSetting($"{name}-columnSize-utility", 100f, () => "Utility Column Size", () => "Defines the width of the column utility.");
        columnSizeUtility.SetRange(20, 300);

        SettingEntry<float> columnSizeReinforced = this.DrawerSettings.DefineSetting($"{name}-columnSize-reinforced", 100f, () => "Reinforced Column Size", () => "Defines the width of the column reinforced.");
        columnSizeReinforced.SetRange(20, 300);

        SettingEntry<int> headerHeight = this.DrawerSettings.DefineSetting($"{name}-headerHeight", 30, () => "Header Height", () => "Defines the height of the header.");
        headerHeight.SetRange(20, 50);

        SettingEntry<int> playerHeight = this.DrawerSettings.DefineSetting($"{name}-playerHeight", 30, () => "Player Height", () => "Defines the height of the player entries.");
        playerHeight.SetRange(20, 50);

        return new OverviewDrawerConfiguration
        {
            Name = drawer.Name,
            Enabled = drawer.Enabled,
            EnabledKeybinding = drawer.EnabledKeybinding,
            BuildDirection = drawer.BuildDirection,
            BackgroundColor = drawer.BackgroundColor,
            FontSize = drawer.FontSize,
            TextColor = drawer.TextColor,
            Location = drawer.Location,
            Opacity = drawer.Opacity,
            Size = drawer.Size,
            ColumnSizes = new TableColumnSizes
            {
                Name = columnSizeName,
                Food = columnSizeFood,
                Utility = columnSizeUtility,
                Reinforced = columnSizeReinforced
            },
            HeaderHeight = headerHeight,
            PlayerHeight = playerHeight
        };
    }

    public new void RemoveDrawer(string name)
    {
        base.RemoveDrawer(name);

        this.DrawerSettings.UndefineSetting($"{name}-columnSize-name");
        this.DrawerSettings.UndefineSetting($"{name}-columnSize-food");
        this.DrawerSettings.UndefineSetting($"{name}-columnSize-utility");
        this.DrawerSettings.UndefineSetting($"{name}-columnSize-reinforced");
        this.DrawerSettings.UndefineSetting($"{name}-columnSize-headerHeight");
        this.DrawerSettings.UndefineSetting($"{name}-columnSize-playerHeight");
    }
}