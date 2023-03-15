namespace Estreya.BlishHUD.StatsGraph
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Settings;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using static Blish_HUD.ContentService;

    public class ModuleSettings : BaseModuleSettings
    {
        public SettingEntry<bool> ShowCategoryNames { get; private set; }

        public SettingEntry<bool> ShowAxisValues { get; private set; }

        public SettingEntry<int> Size { get; private set; }
        public SettingEntry<int> LocationX { get; private set; }
        public SettingEntry<int> LocationY { get; private set; }

        public SettingEntry<float> Zoom { get; private set; }

        public SettingEntry<float> Scale { get; private set; }

        public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding()) { }

        protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
        {
            this.ShowCategoryNames = globalSettingCollection.DefineSetting(nameof(this.ShowCategoryNames), true, () => "Show Category Names", () => "Whether the category names should be visible.");
            this.ShowAxisValues = globalSettingCollection.DefineSetting(nameof(this.ShowAxisValues), true, () => "Show Axis Values", () => "Whether the axis values should be visible.");
            this.Size = globalSettingCollection.DefineSetting(nameof(this.Size), 200, () => "Size", () => "Defines the Size.");
            this.LocationX = globalSettingCollection.DefineSetting(nameof(this.LocationX), 200, () => "Location X", () => "Defines the location on the x axis.");
            this.LocationY = globalSettingCollection.DefineSetting(nameof(this.LocationY), 200, () => "Location Y", () => "Defines the location on the y axis.");
            this.Zoom = globalSettingCollection.DefineSetting(nameof(this.Zoom), 1f, () => "Zoom", () => "Defines the zoom level.");
            this.Zoom.SetRange(0.5f, 3f);
            this.Scale = globalSettingCollection.DefineSetting(nameof(this.Scale), 1f, () => "Scale", () => "Defines the scale of the render.");
            this.Scale.SetRange(0.5f, 3f);
        }

        public void UpdateSizeAndLocationRanges()
        {
            int maxResX = (int)(GameService.Graphics.Resolution.X / GameService.Graphics.UIScaleMultiplier);
            int maxResY = (int)(GameService.Graphics.Resolution.Y / GameService.Graphics.UIScaleMultiplier);

            int minLocationX = 0;
            int maxLocationX = maxResX - this.Size.Value;
            int minLocationY = 0;
            int maxLocationY = maxResY - this.Size.Value;
            int minHeight = 0;
            int maxHeight = maxResY - this.LocationY.Value;

            this.LocationX.SetRange(minLocationX, maxLocationX);
            this.LocationY.SetRange(minLocationY, maxLocationY);
            this.Size.SetRange(minHeight, maxHeight);
        }
    }
}
