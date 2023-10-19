namespace Estreya.BlishHUD.ScrollingCombatText;

using Blish_HUD;
using Blish_HUD.ArcDps;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Models;
using Newtonsoft.Json;
using Shared.Models.ArcDPS;
using Shared.Modules;
using Shared.Services;
using Shared.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using UI.Views.Settings;
using ScreenNotification = Blish_HUD.Controls.ScreenNotification;
using TabbedWindow = Shared.Controls.TabbedWindow;

[Export(typeof(Module))]
public class ScrollingCombatTextModule : BaseModule<ScrollingCombatTextModule, ModuleSettings>
{
    private readonly Dictionary<string, ScrollingTextArea> _areas = new Dictionary<string, ScrollingTextArea>();

    [ImportingConstructor]
    public ScrollingCombatTextModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    protected override string UrlModuleName => "scrolling-combat-text";

    protected override string API_VERSION_NO => "1";

    protected override string GetDirectoryName()
    {
        return "scrolling_combat_text";
    }

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
        //await this.SkillService.WaitAsync();

        foreach (string areaName in this.ModuleSettings.ScrollingAreaNames.Value)
        {
            this.AddArea(areaName);
        }

#if DEBUG
        GameService.Input.Keyboard.KeyPressed += this.Keyboard_KeyPressed;
#endif
    }

    private void Keyboard_KeyPressed(object sender, KeyboardEventArgs e)
    {
        Blish_HUD.ArcDps.Models.CombatEvent ev = JsonConvert.DeserializeObject<Blish_HUD.ArcDps.Models.CombatEvent>("{\"Ev\":{\"Time\":2901217,\"SrcAgent\":2000,\"DstAgent\":2510,\"Value\":-98,\"BuffDmg\":0,\"OverStackValue\":0,\"SkillId\":9122,\"SrcInstId\":6563,\"DstInstId\":4895,\"SrcMasterInstId\":0,\"DstMasterInstId\":0,\"Iff\":1,\"Buff\":false,\"Result\":0,\"IsActivation\":0,\"IsBuffRemove\":0,\"IsNinety\":true,\"IsFifty\":false,\"IsMoving\":false,\"IsStateChange\":0,\"IsFlanking\":true,\"IsShields\":false,\"IsOffCycle\":false,\"Pad61\":0,\"Pad62\":0,\"Pad63\":0,\"Pad64\":0},\"Src\":{\"Name\":\"Asyna Estreya\",\"Id\":2000,\"Profession\":1,\"Elite\":62,\"Self\":1,\"Team\":1863},\"Dst\":{\"Name\":\"Golden Moa\",\"Id\":2510,\"Profession\":4947,\"Elite\":4294967295,\"Self\":0,\"Team\":855},\"Category\":0,\"Type\":1,\"Skill\":{\"Id\":9122,\"Name\":\"Bolt of Wrath\",\"Description\":\"Fire a bolt that damages foes.\",\"Icon\":{\"Url\":null},\"Specialization\":null,\"ChatLink\":\"[&BqIjAAA=]\",\"Type\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"WeaponType\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"Professions\":[\"Guardian\"],\"Slot\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"DualAttunement\":null,\"Flags\":[{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null}],\"Facts\":[{\"Text\":\"Range\",\"Icon\":{\"Url\":null},\"Type\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"RequiresTrait\":null,\"Overrides\":null},{\"Text\":\"Damage\",\"Icon\":{\"Url\":null},\"Type\":{\"IsUnknown\":true,\"Value\":0,\"RawValue\":null},\"RequiresTrait\":null,\"Overrides\":null}],\"TraitedFacts\":null,\"Categories\":null,\"SubSkills\":null,\"Attunement\":null,\"Cost\":null,\"DualWield\":null,\"FlipSkill\":51660,\"Initiative\":null,\"NextChain\":null,\"PrevChain\":null,\"TransformSkills\":null,\"BundleSkills\":null,\"ToolbeltSkill\":null,\"HttpResponseInfo\":null},\"SkillTexture\":null}");
        if (e.EventType != KeyboardEventType.KeyDown)
        {
            return;
        }

        if (GameService.Input.Keyboard.TextFieldIsActive())
        {
            return;
        }

        if (e.Key == Keys.U)
        {
            this.ArcDPSService?.SimulateCombatEvent(new RawCombatEventArgs(ev, RawCombatEventArgs.CombatEventType.Local));
        }
    }

    private void ArcDPSService_Stopped(object sender, EventArgs e)
    {
        ScreenNotification.ShowNotification("ArcDPS Service stopped!", ScreenNotification.NotificationType.Error, duration: 5);
    }

    private void ArcDPSService_Started(object sender, EventArgs e)
    {
        ScreenNotification.ShowNotification("ArcDPS Service started!", duration: 5);
    }

    protected override void OnBeforeServicesStarted()
    {
        this.ArcDPSService.Unavailable += this.ArcDPSService_Unavailable;
        this.ArcDPSService.Started += this.ArcDPSService_Started;
        this.ArcDPSService.Stopped += this.ArcDPSService_Stopped;
        this.ArcDPSService.LocalCombatEvent += this.ArcDPSService_LocalCombatEvent;
    }

    private void ArcDPSService_Unavailable(object sender, EventArgs e)
    {
        ScreenNotification.ShowNotification("ArcDPS Service unavailable!", ScreenNotification.NotificationType.Error, duration: 5);
    }

    private void ArcDPSService_LocalCombatEvent(object sender, CombatEvent e)
    {
        foreach (ScrollingTextArea area in this._areas.Values)
        {
            area.AddCombatEvent(e);
        }
    }

    protected override Collection<ManagedService> GetAdditionalServices(string directoryPath)
    {
        Collection<ManagedService> states = new Collection<ManagedService>();

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
            // Don't show if disabled.
            bool showArea = show && area.Enabled;

            if (showArea)
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

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconService?.GetIcon("156030.png"); // 156135 (32x32)
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconService?.GetIcon("156742.png");
    }

    protected override void OnSettingWindowBuild(TabbedWindow settingWindow)
    {
        this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156736.png"), () => new GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));
        AreaSettingsView areaSettingsView = new AreaSettingsView(() => this._areas.Values.Select(area => area.Configuration), this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService) { DefaultColor = this.ModuleSettings.DefaultGW2Color };
        areaSettingsView.AddArea += (s, e) =>
        {
            e.AreaConfiguration = this.AddArea(e.Name);
        };

        areaSettingsView.RemoveArea += (s, e) =>
        {
            this.RemoveArea(e);
        };

        this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon(@"156742.png"), () => areaSettingsView, "SCT Areas"));
    }

    private ScrollingTextAreaConfiguration AddArea(string areaName)
    {
        if (string.IsNullOrWhiteSpace(areaName))
        {
            throw new ArgumentNullException(nameof(areaName), "Area name can't be null or empty.");
        }

        ScrollingTextAreaConfiguration configuration = this.ModuleSettings.AddDrawer(areaName);
        this.AddArea(configuration);

        return configuration;
    }

    private void AddArea(ScrollingTextAreaConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.Name))
        {
            throw new ArgumentNullException(nameof(configuration.Name), "Area name can't be null or empty.");
        }

        if (!this.ModuleSettings.ScrollingAreaNames.Value.Contains(configuration.Name))
        {
            this.ModuleSettings.ScrollingAreaNames.Value = new List<string>(this.ModuleSettings.ScrollingAreaNames.Value) { configuration.Name };
        }

        ScrollingTextArea area = new ScrollingTextArea(configuration) { Parent = GameService.Graphics.SpriteScreen };

        this._areas.Add(configuration.Name, area);
    }

    private void RemoveArea(ScrollingTextAreaConfiguration configuration)
    {
        this.ModuleSettings.ScrollingAreaNames.Value = new List<string>(this.ModuleSettings.ScrollingAreaNames.Value.Where(areaName => areaName != configuration.Name));

        this._areas[configuration.Name]?.Dispose();
        _ = this._areas.Remove(configuration.Name);

        this.ModuleSettings.RemoveDrawer(configuration.Name);
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        this.ToggleContainers(this.ShowUI);

        foreach (ScrollingTextArea area in this._areas.Values)
        {
            this.ModuleSettings.CheckDrawerSizeAndPosition(area.Configuration);
        }
    }

    /// <inheritdoc />
    protected override void Unload()
    {
        this.Logger.Debug("Unload drawer.");

        foreach (ScrollingTextArea area in this._areas.Values)
        {
            area?.Dispose();
        }

        this._areas?.Clear();

        this.Logger.Debug("Unloaded drawer.");

#if DEBUG
        GameService.Input.Keyboard.KeyPressed -= this.Keyboard_KeyPressed;
#endif

        this.Logger.Debug("Unloading states...");
        if (this.ArcDPSService != null)
        {
            this.ArcDPSService.Unavailable -= this.ArcDPSService_Unavailable;
            this.ArcDPSService.Started -= this.ArcDPSService_Started;
            this.ArcDPSService.Stopped -= this.ArcDPSService_Stopped;
        }

        this.Logger.Debug("Finished unloading states.");

        this.Logger.Debug("Unload base.");

        base.Unload();

        this.Logger.Debug("Unloaded base.");
    }

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        ModuleSettings moduleSettings = new ModuleSettings(settings);

        return moduleSettings;
    }

    protected override void ConfigureServices(ServiceConfigurations configurations)
    {
        configurations.Skills.Enabled = true;
        configurations.ArcDPS.Enabled = true;
    }

    protected override int CornerIconPriority => 1_289_351_277;
}