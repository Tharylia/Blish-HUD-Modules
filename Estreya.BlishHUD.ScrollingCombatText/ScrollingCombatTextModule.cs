namespace Estreya.BlishHUD.ScrollingCombatText
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.ScrollingCombatText.Controls;
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

        private List<ScrollingTextArea> _areas = new List<ScrollingTextArea>();

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
                    case nameof(this.ModuleSettings.Width):
                        //this.Drawer.UpdateSize(this.ModuleSettings.Width.Value, -1);
                        break;
                    case nameof(this.ModuleSettings.GlobalEnabled):
                        this.ToggleContainer(this.ModuleSettings.GlobalEnabled.Value);
                        break;
                    case nameof(this.ModuleSettings.BackgroundColor):
                    case nameof(this.ModuleSettings.BackgroundColorOpacity):
                        //this.Drawer.UpdateBackgroundColor();
                        break;
                    default:
                        break;
                }
            };

            var area = new ScrollingTextArea(new Models.ScrollingTextAreaConfiguration()
            {
                Name = "Test",
                Location = new Point(100,100),
                Size = new Point(500,300),
                ScrollSpeed= 0.5f,
                Curve = Models.ScrollingTextAreaCurve.Left
            }, this.Gw2ApiManager, this.SkillState, this.Font)
            {
                BackgroundColor = Color.LightBlue,
                Parent = GameService.Graphics.SpriteScreen
            };

            this._areas.Add(area);

            GameService.Input.Keyboard.KeyPressed += (s, e) =>
            {
                if (e.EventType != Blish_HUD.Input.KeyboardEventType.KeyDown) return;

                if (e.Key == Microsoft.Xna.Framework.Input.Keys.U)
                {
                    var ev = JsonConvert.DeserializeObject<Blish_HUD.ArcDps.Models.CombatEvent>("{\"Ev\":{\"Time\":2901217,\"SrcAgent\":2000,\"DstAgent\":2510,\"Value\":-98,\"BuffDmg\":0,\"OverStackValue\":0,\"SkillId\":9122,\"SrcInstId\":6563,\"DstInstId\":4895,\"SrcMasterInstId\":0,\"DstMasterInstId\":0,\"Iff\":1,\"Buff\":false,\"Result\":0,\"IsActivation\":0,\"IsBuffRemove\":0,\"IsNinety\":true,\"IsFifty\":false,\"IsMoving\":false,\"IsStateChange\":0,\"IsFlanking\":true,\"IsShields\":false,\"IsOffCycle\":false,\"Pad61\":0,\"Pad62\":0,\"Pad63\":0,\"Pad64\":0},\"Src\":{\"Name\":\"Asyna Estreya\",\"Id\":2000,\"Profession\":1,\"Elite\":62,\"Self\":1,\"Team\":1863},\"Dst\":{\"Name\":\"Golden Moa\",\"Id\":2510,\"Profession\":4947,\"Elite\":4294967295,\"Self\":0,\"Team\":855},\"Category\":0,\"Type\":1,\"Skill\":{\"Id\":9122,\"Name\":\"Bolt of Wrath\",\"Description\":\"Fire a bolt that damages foes.\",\"Icon\":{\"Url\":null},\"Specialization\":null,\"ChatLink\":\"[&BqIjAAA=]\",\"Type\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"WeaponType\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"Professions\":[\"Guardian\"],\"Slot\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"DualAttunement\":null,\"Flags\":[{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null}],\"Facts\":[{\"Text\":\"Range\",\"Icon\":{\"Url\":null},\"Type\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"RequiresTrait\":null,\"Overrides\":null},{\"Text\":\"Damage\",\"Icon\":{\"Url\":null},\"Type\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"RequiresTrait\":null,\"Overrides\":null}],\"TraitedFacts\":null,\"Categories\":null,\"SubSkills\":null,\"Attunement\":null,\"Cost\":null,\"DualWield\":null,\"FlipSkill\":51660,\"Initiative\":null,\"NextChain\":null,\"PrevChain\":null,\"TransformSkills\":null,\"BundleSkills\":null,\"ToolbeltSkill\":null,\"HttpResponseInfo\":null},\"SkillTexture\":null}");

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
            foreach(var area in this._areas)
            {
                area.AddCombatEvent(e);
            }
        }

        protected override Collection<ManagedState> GetAdditionalStates(string directoryPath)
        {
            Collection<ManagedState> states = new Collection<ManagedState>();


            return states;
        }

        private void ToggleContainer(bool show)
        {
            //if (this.Drawer == null)
            //{
            //    return;
            //}

            //if (!this.ModuleSettings.GlobalEnabled.Value)
            //{
            //    if (this.Drawer.Visible)
            //    {
            //        this.Drawer.Hide();
            //    }

            //    return;
            //}

            //if (show)
            //{
            //    if (!this.Drawer.Visible)
            //    {
            //        this.Drawer.Show();
            //    }
            //}
            //else
            //{
            //    if (this.Drawer.Visible)
            //    {
            //        this.Drawer.Hide();
            //    }
            //}
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);

            if (this.ModuleSettings.GlobalEnabled.Value)
            {
                this.ToggleContainer(true);
            }
        }

        protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
        {
            //this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"156736"), () => new UI.Views.Settings.GeneralSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, Strings.SettingsWindow_GeneralSettings_Title));
            //this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\tradingpost.png"), () => new UI.Views.Settings.TransactionSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Transactions"));
            //this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\graphics_settings.png"), () => new UI.Views.Settings.GraphicsSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, Strings.SettingsWindow_GraphicSettings_Title));

        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            this.ToggleContainer(this.ShowUI);

            //this.Drawer.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value); // Handle windows resize

            //this.ModuleSettings.CheckDrawerSizeAndPosition(this.Drawer.Width, this.Drawer.Height);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            this.Logger.Debug("Unload base.");

            base.Unload();

            this.Logger.Debug("Unloaded base.");

            this.Logger.Debug("Unload drawer.");

            foreach (var area in this._areas)
            {
                area.Dispose();
            }

            this.Logger.Debug("Unloaded drawer.");

            this.Logger.Debug("Unloading states...");
            this.Logger.Debug("Finished unloading states.");
        }

        protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
        {
            return new ModuleSettings(settings);
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

