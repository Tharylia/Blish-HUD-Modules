namespace Estreya.BlishHUD.ScrollingCombatText
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.ScrollingCombatText.Models;
    using Estreya.BlishHUD.Shared.Models.ArcDPS;
    using Estreya.BlishHUD.Shared.Models.Drawers;
    using Estreya.BlishHUD.Shared.Settings;
    using System;
    using System.Collections.Generic;

    public class ModuleSettings : BaseModuleSettings
    {
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
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.PHYSICAL,       State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{skill.Name}}: {{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.CRIT,           State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size18, Format = "<c=#ff0000>{{skill.Name}}: {{combatEvent.Value}} ({{destination.Name}})</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.BLEEDING,       State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#e84b30>{{combatEvent.Value}} ({{destination.Name}})</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.BURNING,        State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#ff9e32>{{combatEvent.Value}} ({{destination.Name}})</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.POISON,         State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#00c400>{{combatEvent.Value}} ({{destination.Name}})</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.CONFUSION,      State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#b243ff>{{combatEvent.Value}} ({{destination.Name}})</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.RETALIATION,    State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#ffed00>{{combatEvent.Value}} ({{destination.Name}})</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.TORMENT,        State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#501678>{{combatEvent.Value}} ({{destination.Name}})</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.DOT,            State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#45cdff>{{combatEvent.Value}} ({{destination.Name}})</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.HEAL,           State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#10FB10>{{skill.Name}} +{{combatEvent.Value}}</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.HOT,            State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#10FB10>{{skill.Name}} +{{combatEvent.Value}}</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.SHIELD_RECEIVE, State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#ffff00>{{skill.Name}} +{{combatEvent.Value}}</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.SHIELD_REMOVE,  State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#ffff00>{{combatEvent.Value}} -=absorb=-</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.BLOCK,          State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#0000ff>Block!</c>" },
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.EVADE,          State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#0000ff>Evade!</c>" },
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.INVULNERABLE,   State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#0000ff>Invulnerable!</c>" },
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_OUT, Type = CombatEventType.MISS,           State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#0000ff>Miss!</c>" },

                //PLAYER_IN
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.PHYSICAL,      State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{source.Name}}: {{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.CRIT,          State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size18, Format = "{{source.Name}}: {{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.BLEEDING,      State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{source.Name}}: {{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.BURNING,       State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{source.Name}}: {{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.POISON,        State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{source.Name}}: {{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.CONFUSION,     State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{source.Name}}: {{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.RETALIATION,   State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{source.Name}}: {{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.TORMENT,       State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{source.Name}}: {{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.DOT,           State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{source.Name}}: {{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.HEAL,          State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#10FB10>{{skill.Name}} +{{combatEvent.Value}}</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.HOT,           State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#10FB10>{{skill.Name}} +{{combatEvent.Value}}</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.SHIELD_RECEIVE,State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#ffff00>{{skill.Name}} +{{combatEvent.Value}}</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.SHIELD_REMOVE, State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "<c=#ffff00>{{combatEvent.Value}} -=absorb=-</c>" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.BLOCK,         State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Block!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.EVADE,         State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Evade!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.INVULNERABLE,  State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Invulnerable!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PLAYER_IN, Type = CombatEventType.MISS,          State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Miss!" } ,

                //PET_OUT
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.PHYSICAL,      State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{skill.Name}}: {{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.CRIT,          State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size18, Format = "{{skill.Name}}: {{combatEvent.Value}} ({{destination.Name}})" }     ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.BLEEDING,      State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.BURNING,       State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.POISON,        State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.CONFUSION,     State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.RETALIATION,   State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.TORMENT,       State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.DOT,           State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.HEAL,          State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{skill.Name}} +{{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.HOT,           State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{skill.Name}} +{{combatEvent.Value}}" },
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.SHIELD_RECEIVE,State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{skill.Name}} +{{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.SHIELD_REMOVE, State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "{{combatEvent.Value}} -=absorb=-" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.BLOCK,         State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Block!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.EVADE,         State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Evade!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.INVULNERABLE,  State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Invulnerable!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_OUT, Type = CombatEventType.MISS,          State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Miss!" } ,

                //PET_IN
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.PHYSICAL,      State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{skill.Name}}: {{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.CRIT,          State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size18, Format = "Pet {{skill.Name}}: {{combatEvent.Value}} ({{destination.Name}})" }     ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.BLEEDING,      State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.BURNING,       State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.POISON,        State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.CONFUSION,     State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.RETALIATION,   State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.TORMENT,       State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.DOT,           State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{combatEvent.Value}} ({{destination.Name}})" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.HEAL,          State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{skill.Name}} +{{combatEvent.Value}}" },
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.HOT,           State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{skill.Name}} +{{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.SHIELD_RECEIVE,State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{skill.Name}} +{{combatEvent.Value}}" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.SHIELD_REMOVE, State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet {{combatEvent.Value}} -=absorb=-" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.BLOCK,         State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet Block!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.EVADE,         State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet Evade!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.INVULNERABLE,  State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet Invulnerable!" } ,
                new CombatEventFormatRule() { Category = CombatEventCategory.PET_IN, Type = CombatEventType.MISS,          State = CombatEventState.NORMAL, TextColor = this.DefaultGW2Color, FontSize = ContentService.FontSize.Size16, Format = "Pet Miss!" } ,
            }, () => "Format Rules", () => "The format rules of the drawer.");

            return new ScrollingTextAreaConfiguration()
            {
                Name = drawer.Name,
                Enabled = drawer.Enabled,
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
