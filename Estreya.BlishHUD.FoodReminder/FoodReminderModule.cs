namespace Estreya.BlishHUD.FoodReminder
{
    using Blish_HUD;
    using Blish_HUD.ArcDps.Common;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Modules;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.FoodReminder;
    using Estreya.BlishHUD.FoodReminder.Controls;
    using Estreya.BlishHUD.FoodReminder.Models;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Models.ArcDPS.Buff;
    using Estreya.BlishHUD.Shared.Modules;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.Service;
    using Estreya.BlishHUD.Shared.Threading;
    using Estreya.BlishHUD.Shared.Utils;
    using Flurl.Util;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.ServiceModel.Configuration;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class FoodReminderModule : BaseModule<FoodReminderModule, ModuleSettings>
    {
        public override string UrlModuleName => "food-reminder";

        protected override string API_VERSION_NO => "1";

        private DataFileDefinition _data { get; set; }

        private SynchronizedCollection<Models.Player> _currentPlayers = new SynchronizedCollection<Models.Player>();

        private List<OverviewTable> _areas { get; } = new List<OverviewTable>();

        [ImportingConstructor]
        public FoodReminderModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override async Task LoadAsync()
        {
            await base.LoadAsync();
            GameService.ArcDps.Common.Activate();
            GameService.ArcDps.Common.PlayerAdded += this.ArcDPS_PlayerAdded;
            GameService.ArcDps.Common.PlayerRemoved += this.ArcDPS_PlayerRemoved;
            this.ArcDPSService.AreaCombatEvent += this.ArcDPSService_AreaCombatEvent;

            var dataFileStream = this.ContentsManager.GetFileStream("data.json");

            if (dataFileStream == null) throw new ArgumentNullException(nameof(dataFileStream), "No data file found!");

            this._data = JsonConvert.DeserializeObject<DataFileDefinition>(await new StreamReader(dataFileStream).ReadToEndAsync());

            if (this.ModuleSettings.OverviewNames.Value.Count == 0)
            {
                this.ModuleSettings.OverviewNames.Value = new List<string>() { "Main" };
            }

            this.AddAllAreas();
        }

        protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
        {
            this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156736.png"), () => new UI.Views.GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));
            UI.Views.AreaSettingsView areaSettingsView = new UI.Views.AreaSettingsView(
                () => this._areas.Select(area => area.Configuration),
                this.ModuleSettings,
                this.Gw2ApiManager,
                this.IconService,
                this.TranslationService,
                this.SettingEventService,
                GameService.Content.DefaultFont16)
            { DefaultColor = this.ModuleSettings.DefaultGW2Color };
            areaSettingsView.AddArea += (s, e) =>
            {
                e.AreaConfiguration = this.AddArea(e.Name);
            };

            areaSettingsView.RemoveArea += (s, e) =>
            {
                this.RemoveArea(e);
            };

            this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("605018.png"), () => areaSettingsView, "Event Areas"));

        }

        private void AddAllAreas()
        {
            foreach (var areaName in this.ModuleSettings.OverviewNames.Value)
            {
               _ = this.AddArea(areaName);
            }
        }

        private OverviewDrawerConfiguration AddArea(string name)
        {
            if (!this.ModuleSettings.OverviewNames.Value.Contains(name))
            {
                this.ModuleSettings.OverviewNames.Value = new List<string>(this.ModuleSettings.OverviewNames.Value) { name };
            }

            OverviewDrawerConfiguration configuration = this.ModuleSettings.AddDrawer(name);

            var overviewTable = new OverviewTable(configuration , () => this._currentPlayers.ToList())
            {
                Parent = GameService.Graphics.SpriteScreen
            };

            this._areas.Add(overviewTable);

            return configuration;
        }

        private void RemoveArea(OverviewDrawerConfiguration configuration)
        {
            this.ModuleSettings.RemoveDrawer(configuration.Name);

            this.ModuleSettings.OverviewNames.Value = new List<string>(this.ModuleSettings.OverviewNames.Value.Where(n => n != configuration.Name));
        }

        private void ArcDPS_PlayerRemoved(CommonFields.Player player)
        {
            var foundPlayer = this._currentPlayers.Where(p => p.Name == player.CharacterName).FirstOrDefault();
            if (foundPlayer != null)
            {
                foundPlayer.Tracked = false;
                foundPlayer.Clear();
            }
        }

        private void ArcDPS_PlayerAdded(Blish_HUD.ArcDps.Common.CommonFields.Player player)
        {
            var foundPlayer = this._currentPlayers.Where(p => p.Name == player.CharacterName).FirstOrDefault();
            if (foundPlayer != null)
            {
                foundPlayer.Tracked = true;
                foundPlayer.ArcDPSPlayer = player;
            }
            else
            {
                this._currentPlayers.Add(new Models.Player(player.CharacterName)
                {
                    Tracked = true,
                    ArcDPSPlayer = player
                });
            }
        }

        private void ArcDPSService_AreaCombatEvent(object sender, Shared.Models.ArcDPS.CombatEvent e)
        {
            if (e is BuffApplyCombatEvent buffApplyCombatEvent)
            {
                var foundFood = this._data.Food.FirstOrDefault(f => f.ID == buffApplyCombatEvent.SkillId);
                var foundUtility = this._data.Utility.FirstOrDefault(u => u.ID == buffApplyCombatEvent.SkillId);
                var isReinforced = e.SkillId == this._data.ReinforcedSkillId;

                var buffType = this.GetBuffType(e);

                if (foundFood == null && buffType == BuffType.Food && buffApplyCombatEvent.Source.Self == 1 && buffApplyCombatEvent.Destination.Self == 1)
                {
                    foundFood = new FoodDefinition()
                    {
                        ID = (int)buffApplyCombatEvent.SkillId,
                        Display = "SOME",
                        Name = "Unknown"
                    };
                }

                if (foundUtility == null && buffType == BuffType.Utility && buffApplyCombatEvent.Source.Self == 1 && buffApplyCombatEvent.Destination.Self == 1)
                {
                    foundUtility = new UtilityDefinition()
                    {
                        ID = (int)buffApplyCombatEvent.SkillId,
                        Display = "SOME",
                        Name = "Unknown"
                    };
                }

                var player = this._currentPlayers.Where(p => p.Name == buffApplyCombatEvent.Source.Name).FirstOrDefault();
                if (player == null)
                {
                    this._currentPlayers.Add(new Models.Player(buffApplyCombatEvent.Source.Name)
                    {
                        Food = foundFood,
                        Utility = foundUtility,
                        Reinforced = isReinforced
                    });
                }
                else
                {

                    if (foundFood != null)
                    {
                        player.Food = foundFood;
                    }

                    if (foundUtility != null)
                    {
                        player.Utility = foundUtility;
                    }

                    if (isReinforced)
                    {
                        player.Reinforced = true;
                    }
                }
            }

            if (e is BuffRemoveCombatEvent buffRemoveCombatEvent)
            {
                var player = this._currentPlayers.Where(p => p.Name == buffRemoveCombatEvent.Source.Name).FirstOrDefault();
                if (player == null)
                {
                    this._currentPlayers.Add(new Models.Player(buffRemoveCombatEvent.Source.Name));
                }
                else
                {
                    if (buffRemoveCombatEvent.SkillId == player.Food?.ID && player.IsFoodRemoveable)
                    {
                        player.Food = null;
                    }

                    if (buffRemoveCombatEvent.SkillId == player.Utility?.ID && player.IsUtilityRemoveable)
                    {
                        player.Utility = null;
                    }

                    if (e.SkillId == this._data.ReinforcedSkillId)
                    {
                        player.Reinforced = false;
                    }
                }
            }
        }

        private BuffType GetBuffType(Shared.Models.ArcDPS.CombatEvent combatEvent)
        {
            var skillId = combatEvent.SkillId;
            if (this._data.Food.Any(f => f.ID == skillId)) return BuffType.Food;
            if (this._data.Utility.Any(u => u.ID == skillId)) return BuffType.Utility;

            if (combatEvent.RawCombatEvent.SkillName.ToLowerInvariant() == "nourishment") return BuffType.Food;
            if (combatEvent.RawCombatEvent.SkillName.ToLowerInvariant() == "enhancement") return BuffType.Utility;

            return BuffType.Unknown;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            this.TrackSelf();
        }

        private void TrackSelf()
        {
            if (!GameService.Gw2Mumble.IsAvailable) return;

            var player = this._currentPlayers.FirstOrDefault(p => p.Name == GameService.Gw2Mumble.PlayerCharacter.Name);
            if (player == null)
            {
                this._currentPlayers.Add(new Models.Player(GameService.Gw2Mumble.PlayerCharacter.Name)
                {
                    Tracked = true,
                });
            }
            else
            {
                // Check if self is first in list
                var index = this._currentPlayers.IndexOf(player);
                if (index > 0)
                {
                    this._currentPlayers.RemoveAt(index);
                    this._currentPlayers.Insert(0, player);
                }
            }
        }

        protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
        {
            return new ModuleSettings(settings);
        }

        protected override string GetDirectoryName()
        {
            return "food_reminder";
        }

        protected override AsyncTexture2D GetEmblem()
        {
            return this.IconService.GetIcon("2191071.png");// "866139.png");
        }

        protected override AsyncTexture2D GetCornerIcon()
        {
            return this.IconService.GetIcon("1377783.png");
        }

        protected override void ConfigureServices(ServiceConfigurations configurations)
        {
            configurations.Skills.Enabled = true;
            configurations.ArcDPS.Enabled = true;
        }

        protected override void Unload()
        {
            if (this.ArcDPSService != null)
            {
                this.ArcDPSService.AreaCombatEvent -= this.ArcDPSService_AreaCombatEvent;
            }

            this._areas?.ForEach(drawer => drawer?.Dispose());
            this._areas?.Clear();

            base.Unload();
        }
    }
}

