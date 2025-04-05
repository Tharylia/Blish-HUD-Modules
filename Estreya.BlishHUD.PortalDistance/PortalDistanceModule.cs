namespace Estreya.BlishHUD.PortalDistance;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.PortalDistance.Controls;
using Estreya.BlishHUD.PortalDistance.Models;
using Estreya.BlishHUD.PortalDistance.UI.Views;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.UI.Views;
using Flurl.Util;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2;
using Microsoft.Xna.Framework;
using Shared.Extensions;
using Shared.Helpers;
using Shared.Modules;
using Shared.Settings;
using Shared.Threading;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

[Export(typeof(Module))]
public class PortalDistanceModule : BaseModule<PortalDistanceModule, ModuleSettings>
{
    private Vector3 _portalPosition;
    private PortalDefinition _activePortal;
    private DistanceMessageControl _messageControl;

    private List<PortalDefinition> _portals = new List<PortalDefinition>
    {
        new PortalDefinition(10198, () => GameService.Gw2Mumble.CurrentMap.Type is MapType.Pvp or MapType.Tournament ? 6000 : 5000), // Mesmer Portal
        new PortalDefinition(16437, () => 5000), // Thief Portal
        new PortalDefinition(34978, () => 5000), // White Mantle Portal Device
    };

    [ImportingConstructor]
    public PortalDistanceModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    protected override string UrlModuleName => "portal-distance";

    protected override string API_VERSION_NO => "1";

    protected override bool NeedsBackend => false;

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override async Task LoadAsync()
    {
        await base.LoadAsync();

        this._messageControl = new DistanceMessageControl();

        this.ModuleSettings.ManualKeyBinding.Value.Activated += this.ManualKeyBinding_Activated;
    }

    private void ManualKeyBinding_Activated(object sender, EventArgs e)
    {
        this.UpdatePortalPosition(this._portalPosition == Vector3.Zero ? GameService.Gw2Mumble.PlayerCharacter.Position : Vector3.Zero);
    }

    private void ArcDPSService_AreaCombatEvent(object sender, Shared.Models.ArcDPS.CombatEvent e)
    {
        if (e.Type != Shared.Models.ArcDPS.CombatEventType.BUFF) return;

#if DEBUG
        this.Logger.Debug($"Got buff change {e.SkillId} - {e.RawCombatEvent.SkillName}: {e.State}");
#endif

        var portals = this._portals.Where(x => x.SkillID == e.SkillId);
        if (!portals.Any()) return;

        var portal = portals.First();

        var position = GameService.Gw2Mumble.PlayerCharacter.Position;

        if (e.State == Shared.Models.ArcDPS.CombatEventState.BUFFAPPLY)
        {
            this.Logger.Debug($"Portal skill ({e.SkillId} - {e.RawCombatEvent.SkillName}) activated at {position}");
            this._activePortal = portal;
        }
        else
        {
            this.Logger.Debug($"Portal skill ({e.SkillId} - {e.RawCombatEvent.SkillName}) deactivated at {position}");
            this._activePortal = null;
        }

        this.UpdatePortalPosition(this._activePortal is not null ? position : Vector3.Zero);
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        this.UpdatePortalDistance();
    }

    private void UpdatePortalDistance()
    {
        var portalPosition = this._portalPosition;

        if (portalPosition == Vector3.Zero)
        {
            if (this._messageControl?.Visible ?? false)
            {
                this._messageControl.Hide();
            }

            return;
        }

        if (!this._messageControl?.Visible ?? false)
        {
            this._messageControl.Show();
        }

        var end = GameService.Gw2Mumble.PlayerCharacter.Position;
        var distance = Vector3.Distance(end, _portalPosition).ToInches();

        this._messageControl?.UpdateDistance(distance);
        if (this._activePortal is not null)
        {
            this._messageControl?.UpdateColor(distance > this._activePortal.GetMaxDistance() ? Color.Red : Color.Green);
        }
        else
        {
            this._messageControl?.UpdateColor(Color.Yellow);
        }
    }

    private void UpdatePortalPosition(Vector3 position)
    {
        this._portalPosition = GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode ? Vector3.Zero : position;
    }

    /// <inheritdoc />
    protected override void Unload()
    {
        this.ModuleSettings.ManualKeyBinding.Value.Activated -= this.ManualKeyBinding_Activated;

        if (this.ArcDPSService != null)
        {
            this.ArcDPSService.AreaCombatEvent -= this.ArcDPSService_AreaCombatEvent;
        }

        this._activePortal = null;

        this._messageControl?.Dispose();
        this._messageControl = null;

        base.Unload();
    }

    protected override void OnSettingWindowBuild(Shared.Controls.TabbedWindow settingWindow)
    {
        settingWindow.SavesSize = true;
        settingWindow.CanResize = true;
        settingWindow.RebuildViewAfterResize = true;
        settingWindow.UnloadOnRebuild = false;
        settingWindow.MinSize = settingWindow.Size;
        settingWindow.MaxSize = new Point(settingWindow.Width * 2, settingWindow.Height * 3);
        settingWindow.RebuildDelay = 500;

        this.SettingsWindow.Tabs.Add(new Tab(
            this.IconService.GetIcon("156736.png"),
            () => new GeneralSettingsView( this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, this.ModuleSettings) { DefaultColor = this.ModuleSettings.DefaultGW2Color },
            this.TranslationService.GetTranslation("generalSettingsView-title", "General")));       
    }

    public override IView GetSettingsView()
    {
        return new ModuleSettingsView(this.IconService, this.TranslationService);
    }

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        return new ModuleSettings(settings, this.Version);
    }

    protected override void ConfigureServices(ServiceConfigurations configurations)
    {
        var useArcDPS = this.ModuleSettings.UseArcDPS.Value;

        configurations.Skills.Enabled = useArcDPS;
        configurations.Skills.AwaitLoading = false;
        configurations.ArcDPS.Enabled = useArcDPS;
    }

    protected override void OnBeforeServicesStarted()
    {
        if (this.ArcDPSService != null)
        {
            this.ArcDPSService.AreaCombatEvent += this.ArcDPSService_AreaCombatEvent;
        }
    }

    protected override string GetDirectoryName()
    {
        return "portal_distance";
    }

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconService.GetIcon("740204.png");
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconService.GetIcon("textures/102338-grey.png");
    }

    protected override AsyncTexture2D GetErrorCornerIcon()
    {
        return this.IconService.GetIcon("textures/102338-grey-error.png");
    }

    protected override int CornerIconPriority => 1_289_351_269;
}