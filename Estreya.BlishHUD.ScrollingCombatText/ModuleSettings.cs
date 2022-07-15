namespace Estreya.BlishHUD.ScrollingCombatText
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.ScrollingCombatText.Models;
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
            DrawerConfiguration drawer = base.AddDrawer(name);

            SettingEntry<List<CombatEventCategory>> categories = this.DrawerSettings.DefineSetting($"{name}-categories", new List<CombatEventCategory>()
            {
                CombatEventCategory.PLAYER_OUT,
                CombatEventCategory.PLAYER_IN,
                CombatEventCategory.PET_OUT,
                CombatEventCategory.PET_IN
            }, () => "Combat Event Categories", () => "The combat categories of the drawer.");

            SettingEntry<List<CombatEventType>> types = this.DrawerSettings.DefineSetting($"{name}-types", new List<CombatEventType>()
            {
                CombatEventType.PHYSICAL,
                CombatEventType.CRIT,
                CombatEventType.BLEEDING,
                CombatEventType.BURNING,
                CombatEventType.POISON,
                CombatEventType.CONFUSION,
                CombatEventType.RETALIATION,
                CombatEventType.TORMENT,
                CombatEventType.DOT,
                CombatEventType.HEAL,
                CombatEventType.HOT,
                CombatEventType.SHIELD_RECEIVE,
                CombatEventType.SHIELD_REMOVE,
                CombatEventType.BLOCK,
                CombatEventType.EVADE,
                CombatEventType.INVULNERABLE,
                CombatEventType.MISS
            }, () => "Combat Event Types", () => "The combat types of the drawer.");

            SettingEntry<ScrollingTextAreaCurve> curve = this.DrawerSettings.DefineSetting($"{name}-curve", ScrollingTextAreaCurve.Straight, () => "Curve", () => "The curve of the drawer.");

            SettingEntry<int> eventHeight = this.DrawerSettings.DefineSetting($"{name}-eventHeight", -1, () => "Event Height", () => "The event height of the drawer.");
            eventHeight.SetRange(20, 100);

            SettingEntry<float> scrollSpeed = this.DrawerSettings.DefineSetting($"{name}-scrollSpeed", 1f, () => "Scrollspeed", () => "The scrollspeed of the drawer.");
            scrollSpeed.SetRange(0.3f, 2f);

            SettingEntry<List<CombatEventFormatRule>> formatRules = this.DrawerSettings.DefineSetting($"{name}-formatRules", new List<CombatEventFormatRule>()
            {
                // PLAYER_OUT
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.PHYSICAL,         Format = "{{skill.name}}: {{value}} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.CRIT,             Format = "{{skill.name}}: {{value}} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.BLEEDING,         Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.BURNING,          Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.POISON,           Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.CONFUSION,        Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.RETALIATION,      Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.TORMENT,          Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.DOT,              Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.HEAL,             Format = "+{{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.HOT,              Format = "+{{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.SHIELD_RECEIVE,   Format = "+{{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.SHIELD_REMOVE,    Format = "{{value}} -=absorb=-" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.BLOCK,            Format = "Block!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.EVADE,            Format = "Evade!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.INVULNERABLE,     Format = "Invulnerable!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.MISS,             Format = "Miss!" } ,

                //PLAYER_IN
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.PHYSICAL,         Format = "{{source.name}}: {{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.CRIT,             Format = "{{source.name}}: {{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.BLEEDING,         Format = "{{source.name}}: {{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.BURNING,          Format = "{{source.name}}: {{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.POISON,           Format = "{{source.name}}: {{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.CONFUSION,        Format = "{{source.name}}: {{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.RETALIATION,      Format = "{{source.name}}: {{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.TORMENT,          Format = "{{source.name}}: {{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.DOT,              Format = "{{source.name}}: {{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.HEAL,             Format = "+{{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.HOT,              Format = "+{{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.SHIELD_RECEIVE,   Format = "+{{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.SHIELD_REMOVE,    Format = "{{value}} -=absorb=-" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.BLOCK,            Format = "Block!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.EVADE,            Format = "Evade!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.INVULNERABLE,     Format = "Invulnerable!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.MISS,             Format = "Miss!" } ,

                //PET_OUT
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.PHYSICAL,         Format = "{{skill.name}}: {{value}} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.CRIT,             Format = "{{skill.name}}: {{value}} ({{destination.name}})" }     ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.BLEEDING,         Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.BURNING,          Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.POISON,           Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.CONFUSION,        Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.RETALIATION,      Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.TORMENT,          Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.DOT,              Format = "{{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.HEAL,             Format = "+{{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.HOT,              Format = "+{{value}}" },
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.SHIELD_RECEIVE,   Format = "+{{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.SHIELD_REMOVE,    Format = "{{value}} -=absorb=-" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.BLOCK,            Format = "Block!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.EVADE,            Format = "Evade!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.INVULNERABLE,     Format = "Invulnerable!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.MISS,             Format = "Miss!" } ,

                //PET_IN
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.PHYSICAL,         Format = "Pet {{skill.name}}: {{value}} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.CRIT,             Format = "Pet {{skill.name}}: {{value}} ({{destination.name}})" }     ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.BLEEDING,         Format = "Pet {{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.BURNING,          Format = "Pet {{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.POISON,           Format = "Pet {{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.CONFUSION,        Format = "Pet {{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.RETALIATION,      Format = "Pet {{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.TORMENT,          Format = "Pet {{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.DOT,              Format = "Pet {{value} ({{destination.name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.HEAL,             Format = "Pet +{{value}}" },
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.HOT,              Format = "Pet +{{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.SHIELD_RECEIVE,   Format = "Pet +{{value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.SHIELD_REMOVE,    Format = "Pet {{value}} -=absorb=-" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.BLOCK,            Format = "Pet Block!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.EVADE,            Format = "Pet Evade!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.INVULNERABLE,     Format = "Pet Invulnerable!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.MISS,             Format = "Pet Miss!" } ,
            }, () => "Format Rules", () => "The format rules of the drawer.");

            return new ScrollingTextAreaConfiguration()
            {
                Name = drawer.Name,
                BuildDirection = drawer.BuildDirection,
                BackgroundColor = drawer.BackgroundColor,
                FontSize = drawer.FontSize,
                TextColor = drawer.TextColor,
                Location = drawer.Location,
                Opacity = drawer.Opacity,
                Size = drawer.Size,
                Categories = categories,
                Types = types,
                Curve = curve,
                EventHeight = eventHeight,
                ScrollSpeed = scrollSpeed,
                FormatRules = formatRules,
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
            this.DrawerSettings.UndefineSetting($"{name}-formatRules");
        }

        public override void Unload()
        {
            base.Unload();
        }
    }
}
