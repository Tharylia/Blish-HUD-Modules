namespace Estreya.BlishHUD.ScrollingCombatText
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.ScrollingCombatText.Controls;
    using Estreya.BlishHUD.ScrollingCombatText.Models;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
    using Estreya.BlishHUD.Shared.Modules;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
    using Humanizer;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading.Tasks;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class ScrollingCombatTextModule : BaseModule<ScrollingCombatTextModule, ModuleSettings>
    {
        public override string WebsiteModuleName => "scrolling-combat-text";

        internal static ScrollingCombatTextModule ModuleInstance => Instance;

        internal DateTime DateTimeNow => DateTime.Now;

        private Dictionary<string, ScrollingTextArea> _areas = new Dictionary<string, ScrollingTextArea>();

        #region States
        #endregion

        [ImportingConstructor]
        public ScrollingCombatTextModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void Initialize()
        {

            GameService.Overlay.UserLocaleChanged += (s, e) =>
            {
            };
        }

        protected override async Task LoadAsync()
        {
            await base.LoadAsync();

            // Wait for skills to be loaded.
            await this.SkillState.WaitAsync();

            this.ModuleSettings.ModuleSettingsChanged += (sender, eventArgs) =>
            {
                switch (eventArgs.Name)
                {
                    case nameof(this.ModuleSettings.GlobalDrawerVisible):
                        this.ToggleContainers(this.ModuleSettings.GlobalDrawerVisible.Value);
                        break;
                    default:
                        break;
                }
            };

            /*
            var powerArea = new ScrollingTextArea(new Models.ScrollingTextAreaConfiguration()
            {
                Name = "Power",
                Location = new Point(100,100),
                Size = new Point(500,300),
                ScrollSpeed = 0.3f,
                EventHeight = 32,
                Curve = Models.ScrollingTextAreaCurve.Straight,
                Types= new List<Shared.Models.ArcDPS.CombatEventType>() { Shared.Models.ArcDPS.CombatEventType.PHYSICAL, Shared.Models.ArcDPS.CombatEventType.CRIT}
            }, this.Gw2ApiManager, this.SkillState, this.Font)
            {
                BackgroundColor = Color.LightBlue,
                Parent = GameService.Graphics.SpriteScreen
            };

            var condiArea = new ScrollingTextArea(new Models.ScrollingTextAreaConfiguration()
            {
                Name = "Condi",
                Location = new Point(600, 100),
                Size = new Point(500, 300),
                ScrollSpeed = 0.3f,
                EventHeight = 32,
                Curve = Models.ScrollingTextAreaCurve.Straight,
                Types = new List<Shared.Models.ArcDPS.CombatEventType>() { Shared.Models.ArcDPS.CombatEventType.BLEEDING, Shared.Models.ArcDPS.CombatEventType.BURNING, Shared.Models.ArcDPS.CombatEventType.POISON, Shared.Models.ArcDPS.CombatEventType.TORMENT, Shared.Models.ArcDPS.CombatEventType.CONFUSION, Shared.Models.ArcDPS.CombatEventType.DOT }
            }, this.Gw2ApiManager, this.SkillState, this.Font)
            {
                BackgroundColor = Color.OrangeRed,
                Parent = GameService.Graphics.SpriteScreen
            };

            var healArea = new ScrollingTextArea(new Models.ScrollingTextAreaConfiguration()
            {
                Name = "Condi",
                Location = new Point(1100, 100),
                Size = new Point(500, 300),
                ScrollSpeed = 0.3f,
                EventHeight = 32,
                Curve = Models.ScrollingTextAreaCurve.Straight,
                Types = new List<Shared.Models.ArcDPS.CombatEventType>() { Shared.Models.ArcDPS.CombatEventType.HOT, Shared.Models.ArcDPS.CombatEventType.HEAL }
            }, this.Gw2ApiManager, this.SkillState, this.Font)
            {
                BackgroundColor = Color.LightGreen,
                Parent = GameService.Graphics.SpriteScreen
            };

            this._areas.Add(powerArea);
            this._areas.Add(condiArea);
            this._areas.Add(healArea);
            */

            foreach (string areaName in this.ModuleSettings.ScrollingAreaNames.Value)
            {
                this.AddArea(this.ModuleSettings.AddDrawer(areaName));
            }

            var ev = JsonConvert.DeserializeObject<Blish_HUD.ArcDps.Models.CombatEvent>("{\"Ev\":{\"Time\":2901217,\"SrcAgent\":2000,\"DstAgent\":2510,\"Value\":-98,\"BuffDmg\":0,\"OverStackValue\":0,\"SkillId\":9122,\"SrcInstId\":6563,\"DstInstId\":4895,\"SrcMasterInstId\":0,\"DstMasterInstId\":0,\"Iff\":1,\"Buff\":false,\"Result\":0,\"IsActivation\":0,\"IsBuffRemove\":0,\"IsNinety\":true,\"IsFifty\":false,\"IsMoving\":false,\"IsStateChange\":0,\"IsFlanking\":true,\"IsShields\":false,\"IsOffCycle\":false,\"Pad61\":0,\"Pad62\":0,\"Pad63\":0,\"Pad64\":0},\"Src\":{\"Name\":\"Asyna Estreya\",\"Id\":2000,\"Profession\":1,\"Elite\":62,\"Self\":1,\"Team\":1863},\"Dst\":{\"Name\":\"Golden Moa\",\"Id\":2510,\"Profession\":4947,\"Elite\":4294967295,\"Self\":0,\"Team\":855},\"Category\":0,\"Type\":1,\"Skill\":{\"Id\":9122,\"Name\":\"Bolt of Wrath\",\"Description\":\"Fire a bolt that damages foes.\",\"Icon\":{\"Url\":null},\"Specialization\":null,\"ChatLink\":\"[&BqIjAAA=]\",\"Type\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"WeaponType\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"Professions\":[\"Guardian\"],\"Slot\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"DualAttunement\":null,\"Flags\":[{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null}],\"Facts\":[{\"Text\":\"Range\",\"Icon\":{\"Url\":null},\"Type\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"RequiresTrait\":null,\"Overrides\":null},{\"Text\":\"Damage\",\"Icon\":{\"Url\":null},\"Type\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"RequiresTrait\":null,\"Overrides\":null}],\"TraitedFacts\":null,\"Categories\":null,\"SubSkills\":null,\"Attunement\":null,\"Cost\":null,\"DualWield\":null,\"FlipSkill\":51660,\"Initiative\":null,\"NextChain\":null,\"PrevChain\":null,\"TransformSkills\":null,\"BundleSkills\":null,\"ToolbeltSkill\":null,\"HttpResponseInfo\":null},\"SkillTexture\":null}");
            GameService.Input.Keyboard.KeyPressed += (s, e) =>
            {
                if (e.EventType != Blish_HUD.Input.KeyboardEventType.KeyDown) return;
                if (GameService.Input.Keyboard.TextFieldIsActive()) return;

                if (e.Key == Microsoft.Xna.Framework.Input.Keys.U)
                {
                    this.ArcDPSState?.SimulateCombatEvent(new Blish_HUD.ArcDps.RawCombatEventArgs(ev, Blish_HUD.ArcDps.RawCombatEventArgs.CombatEventType.Local));
                }
            };
        }

        protected override void HandleDefaultStates()
        {
            this.ArcDPSState.LocalCombatEvent += this.ArcDPSState_LocalCombatEvent;
        }

        private void ArcDPSState_LocalCombatEvent(object sender, Shared.Models.ArcDPS.CombatEvent e)
        {
            foreach (var area in this._areas.Values)
            {
                area.AddCombatEvent(new Shared.Models.ArcDPS.CombatEvent(e.Ev, e.Src, e.Dst, e.Category, e.Type) { Skill = e.Skill });
            }

            e.Dispose();
        }

        protected override Collection<ManagedState> GetAdditionalStates(string directoryPath)
        {
            Collection<ManagedState> states = new Collection<ManagedState>();


            return states;
        }

        private void ToggleContainers(bool show)
        {

            if (!this.ModuleSettings.GlobalDrawerVisible.Value)
            {
                show = false;
            }

            this._areas.Values.ToList().ForEach(area =>
            {
                if (show)
                {
                    if (!area.Visible)
                    {
                        area.Show();
                    }
                }
                else
                {
                    if (area.Visible)
                    {
                        area.Hide();
                    }
                }
            });
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);

            if (this.ModuleSettings.GlobalDrawerVisible.Value)
            {
                this.ToggleContainers(true);
            }
        }

        protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
        {
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"156736"), () => new UI.Views.Settings.GeneralSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General Settings"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"textures\graphics_settings.png"), () => new UI.Views.Settings.GraphicsSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Graphic Settings"));
            var areaSettingsView = new UI.Views.Settings.AreaSettingsView(() => this._areas.Values.Select(area => area.Configuration)) { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color };
            areaSettingsView.AddArea += (s, e) =>
            {
                this.AddArea(e);
            };

            areaSettingsView.RemoveArea += (s, e) =>
            {
                this.RemoveArea(e);
            };

            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"textures\scrolling_combat_text.png"), () => areaSettingsView, "SCT Area Settings"));

        }

        private void AddArea(ScrollingTextAreaConfiguration configuration)
        {
            if (!this.ModuleSettings.ScrollingAreaNames.Value.Contains(configuration.Name))
            {
                this.ModuleSettings.ScrollingAreaNames.Value = new List<string>(this.ModuleSettings.ScrollingAreaNames.Value) { configuration.Name };
            }

            this._areas.Add(configuration.Name, new ScrollingTextArea(configuration)
            {
                Parent = GameService.Graphics.SpriteScreen
            });
        }

        private void RemoveArea(ScrollingTextAreaConfiguration configuration)
        {
            this.ModuleSettings.ScrollingAreaNames.Value = new List<string>(this.ModuleSettings.ScrollingAreaNames.Value.Where(areaName => areaName != configuration.Name));

            this._areas[configuration.Name]?.Dispose();
            this._areas.Remove(configuration.Name);

            this.ModuleSettings.RemoveDrawer(configuration.Name);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            this.ToggleContainers(this.ShowUI);

            //this.Drawer.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value); // Handle windows resize

            foreach (var area in this._areas.Values)
            {
                this.ModuleSettings.CheckDrawerSizeAndPosition(area.Configuration);
            }
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            this.Logger.Debug("Unload base.");

            base.Unload();

            this.Logger.Debug("Unloaded base.");

            this.Logger.Debug("Unload drawer.");

            foreach (var area in this._areas.Values)
            {
                area.Dispose();
            }

            _areas.Clear();

            this.Logger.Debug("Unloaded drawer.");

            this.Logger.Debug("Unloading states...");
            this.Logger.Debug("Finished unloading states.");
        }

        protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
        {
            var moduleSettings = new ModuleSettings(settings);


            return moduleSettings;
        }

        protected override string GetDirectoryName()
        {
            return "scrolling_combat_text";
        }

        protected override void ConfigureStates(StateConfigurations configurations)
        {
            configurations.PointOfInterests = false;
            configurations.TradingPost = false;
            configurations.Mapchests = false;
            configurations.Worldbosses = false;
        }
    }
}

