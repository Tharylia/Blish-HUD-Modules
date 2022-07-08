namespace Estreya.BlishHUD.ScrollingCombatText
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.ScrollingCombatText.Models;
    using Estreya.BlishHUD.Shared.Models;
    using Estreya.BlishHUD.Shared.Models.ArcDPS;
    using Estreya.BlishHUD.Shared.Models.Drawers;
    using Estreya.BlishHUD.Shared.Settings;
    using System.Collections.Generic;

    public class ModuleSettings : BaseModuleSettings
    {
        private static readonly Logger Logger = Logger.GetLogger<ModuleSettings>();

        #region Scrolling Text Areas
        private const string SCROLLING_AREA_SETTINGS = "scrolling-area-settings";
        public SettingCollection ScrollingAreaSettings { get; private set; }
        public SettingEntry<List<string>> ScrollingAreaNames { get; private set; }
        #endregion

        public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.S)) { }

        protected override void InitializeAdditionalSettings(SettingCollection settings)
        {
            this.ScrollingAreaSettings = settings.AddSubCollection(SCROLLING_AREA_SETTINGS);

            this.ScrollingAreaNames = this.ScrollingAreaSettings.DefineSetting(nameof(this.ScrollingAreaNames), new List<string>(), () => "Scrolling Area Names", () => "Defines the scrolling area names.");
        }

        public ScrollingTextAreaConfiguration AddDrawer(string name)
        {
            var drawer = base.AddDrawer(name);

            var categories = this.DrawerSettings.DefineSetting($"{name}-categories", new List<CombatEventCategory>(), () => "Combat Event Categories", () => "The combat categories of the drawer.");
            var types = this.DrawerSettings.DefineSetting($"{name}-types", new List<CombatEventType>(), () => "Combat Event Types", () => "The combat types of the drawer.");
            var curve = this.DrawerSettings.DefineSetting($"{name}-curve", ScrollingTextAreaCurve.Straight, () => "Curve", () => "The curve of the drawer.");
            var eventHeight = this.DrawerSettings.DefineSetting($"{name}-eventHeight", -1, () => "Event Height", () => "The event height of the drawer.");
            eventHeight.SetRange(20, 100);
            var scrollSpeed = this.DrawerSettings.DefineSetting($"{name}-scrollSpeed", 1f, () => "Scrollspeed", () => "The scrollspeed of the drawer.");
            scrollSpeed.SetRange(0.3f, 2f);

            return new ScrollingTextAreaConfiguration()
            {
                Name = drawer.Name,
                BuildDirection = drawer.BuildDirection,
                BackgroundColor = drawer.BackgroundColor,
                FontSize = drawer.FontSize,
                Location = drawer.Location,
                Opacity = drawer.Opacity,
                Size = drawer.Size,
                Categories = categories,
                Types = types,
                Curve = curve,
                EventHeight = eventHeight,
                ScrollSpeed = scrollSpeed
            };
        }
        public new void RemoveDrawer(string name)
        {
            base.RemoveDrawer(name);

            this.DrawerSettings.UndefineSetting($"{name}-categories");
            this.DrawerSettings.UndefineSetting($"{name}-types");
            this.DrawerSettings.UndefineSetting($"{name}-curve");
            this.DrawerSettings.UndefineSetting($"{name}-eventHeight");
            this.DrawerSettings.UndefineSetting($"{name}-scrollSpeed");
        }

        public override void Unload()
        {
            base.Unload();
        }
    }
}
