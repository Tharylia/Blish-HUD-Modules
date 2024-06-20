namespace Estreya.BlishHUD.BuffReminder;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Extensions;
using Flurl.Http;
using Gw2Sharp.Models;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using Shared.Modules;
using Shared.MumbleInfo.Map;
using Shared.Services;
using Shared.Settings;
using Shared.Threading;
using Shared.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ScreenNotification = Shared.Controls.ScreenNotification;
using TabbedWindow = Shared.Controls.TabbedWindow;
using Estreya.BlishHUD.Shared.Helpers;
using Blish_HUD.ArcDps.Models;
using Estreya.BlishHUD.Shared.Contexts;
using Windows.UI.WindowManagement;
using Microsoft.Xna.Framework.Audio;
using Blish_HUD.ArcDps;
using Estreya.BlishHUD.BuffReminder.Models;
using Newtonsoft.Json;
using Estreya.BlishHUD.Shared.Controls;

/// <summary>
/// The event table module class.
/// </summary>
[Export(typeof(Module))]
public class BuffReminderModule : BaseModule<BuffReminderModule, ModuleSettings>
{

    //private ConcurrentDictionary<string, EventArea> _areas;

    [ImportingConstructor]
    public BuffReminderModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
    {
    }

    protected override string UrlModuleName => "buff-reminder";

    protected override bool NeedsBackend => false;

    protected override bool EnableMetrics => false;

    protected override string API_VERSION_NO => "1";

    public List<Buff> _trackedBuffs = new List<Buff>();

    public List<Buff> _activeBuffs = new List<Buff>();

    protected override void Initialize()
    {
        base.Initialize();

        //this._areas = new ConcurrentDictionary<string, EventArea>();

        this._trackedBuffs.Add(new Buff()
        {
            Name = "Quickness",
            Ids = new List<uint>() { 1187 }
        });
        this._trackedBuffs.Add(new Buff()
        {
            Name = "Might",
            Ids = new List<uint>() { 740 }
        });
        this._trackedBuffs.Add(new Buff()
        {
            Name = "Protection",
            Ids = new List<uint>() { 717 }
        });
    }

    protected override async Task LoadAsync()
    {
        Stopwatch sw = Stopwatch.StartNew();
        await base.LoadAsync();

        GameService.ArcDps.RawCombatEvent += this.ArcDps_RawCombatEvent;

        this.AddAllAreas();

        foreach (var trackedBuff in this._trackedBuffs)
        {
            await this.MessageContainer.Add(this, MessageContainer.MessageType.Debug, $"Tracking Buff \"{trackedBuff.Name}\" ({string.Join(", ", trackedBuff.Ids.ToArray())})");
        }

        sw.Stop();

        var elapsedMs = sw.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        this.Logger.Debug($"Loaded in {elapsedMs}ms.");
        await this.MessageContainer.Add(this, MessageContainer.MessageType.Debug, $"Loaded in {elapsedMs}ms.");
    }

    private void ArcDps_RawCombatEvent(object sender, RawCombatEventArgs e)
    {
        if (e.CombatEvent.Ev == null || e.CombatEvent.Dst == null) return;
        if (!e.CombatEvent.Ev.Buff || e.CombatEvent.Dst.Self != 1) return;

        bool buffAdded = e.CombatEvent.Ev.IsBuffRemove == Blish_HUD.ArcDps.ArcDpsEnums.BuffRemove.None;

        var trackedBuffs = this._trackedBuffs.Where(b => b.Ids?.Contains(e.CombatEvent.Ev.SkillId) ?? false).ToList();

        if (trackedBuffs.Count == 0)
        {
            this.Logger.Debug($"Buff \"{e.CombatEvent.SkillName}\" ({e.CombatEvent.Ev.SkillId}) is not tracked.");
            return;
        }

        lock (this._activeBuffs)
        {
            var activeBuffs = this._activeBuffs.Where(b => b.Ids?.Contains(e.CombatEvent.Ev.SkillId) ?? false).ToList();
            if (buffAdded && activeBuffs.Count == 0)
            {
                this._activeBuffs.AddRange(trackedBuffs);
            }
            else if (!buffAdded && activeBuffs.Count > 0)
            {
                foreach (var buff in trackedBuffs)
                {
                    this._activeBuffs.Remove(buff);
                }
            }

            this.Logger.Debug($"Now active: {string.Join(", ", this._activeBuffs.Select(b => b.Name).ToArray())}");
        }
    }

    protected override void OnModuleLoaded(EventArgs e)
    {
        base.OnModuleLoaded(e);
    }



    private void CheckDrawerSettings()
    {
        // Don't lock when it would freeze
        //if (this._eventCategoryLock.IsFree())
        //{
        //    using (this._eventCategoryLock.Lock())
        //    {
        //        foreach (KeyValuePair<string, EventArea> area in this._areas)
        //        {
        //            this.ModuleSettings.CheckDrawerSettings(area.Value.Configuration, this._eventCategories);
        //        }
        //    }
        //}
    }

    /// <summary>
    ///     Toggles all areas based on ui visibility calculations.
    /// </summary>
    private void ToggleContainers()
    {
        bool show = this.ShowUI && this.ModuleSettings.GlobalDrawerVisible.Value;

        //foreach (var area in this._areas.Values)
        //{
        //    // Don't show if disabled.
        //    bool showArea = show && area.Enabled && area.CalculateUIVisibility();

        //    if (showArea)
        //    {
        //        if (!area.Visible)
        //        {
        //            area.Show();
        //        }
        //    }
        //    else
        //    {
        //        if (area.Visible)
        //        {
        //            area.Hide();
        //        }
        //    }
        //}
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        this.ToggleContainers();

        //this.ModuleSettings.CheckGlobalSizeAndPosition();

        //foreach (EventArea area in this._areas.Values)
        //{
        //    this.ModuleSettings.CheckDrawerSizeAndPosition(area.Configuration);
        //}
    }

    /// <summary>
    ///     Adds all saved areas.
    /// </summary>
    private void AddAllAreas()
    {
        //if (this.ModuleSettings.EventAreaNames.Value.Count == 0)
        //{
        //    this.ModuleSettings.EventAreaNames.Value.Add("Main");
        //}

        //foreach (string areaName in this.ModuleSettings.EventAreaNames.Value)
        //{
        //    this.AddArea(areaName);
        //}
    }

    /// <summary>
    ///     Adds a new area
    /// </summary>
    /// <param name="name">The name of the new area</param>
    /// <returns>The created area configuration.</returns>
    //private EventAreaConfiguration AddArea(string name)
    //{
    //    EventAreaConfiguration config = this.ModuleSettings.AddDrawer(name, this._eventCategories);
    //    this.AddArea(config);

    //    return config;
    //}

    /// <summary>
    ///     Adds a new area.
    /// </summary>
    /// <param name="configuration">The configuration of the new area.</param>
    //private void AddArea(EventAreaConfiguration configuration)
    //{
    //    if (!this.ModuleSettings.EventAreaNames.Value.Contains(configuration.Name))
    //    {
    //        this.ModuleSettings.EventAreaNames.Value = new List<string>(this.ModuleSettings.EventAreaNames.Value) { configuration.Name };
    //    }

    //    this.ModuleSettings.UpdateDrawerLocalization(configuration, this.TranslationService);

    //    EventArea area = new EventArea(
    //        configuration,
    //        this.IconService,
    //        this.TranslationService,
    //        this.EventStateService,
    //        this.WorldbossService,
    //        this.MapchestService,
    //        this.PointOfInterestService,
    //        this.AccountService,
    //        this.ChatService,
    //        this.MapUtil,
    //        this.GetFlurlClient(),
    //        this.MODULE_API_URL,
    //        () => this.NowUTC,
    //        () => this.Version,
    //        () => this.BlishHUDAPIService.AccessToken,
    //        () => this.ModuleSettings.EventAreaNames.Value.ToArray().ToList(),
    //        () => this.ModuleSettings.ReminderDisabledForEvents.Value.ToArray().ToList(),
    //        this.ContentsManager)
    //    { Parent = GameService.Graphics.SpriteScreen };

    //    area.CopyToAreaClicked += this.EventArea_CopyToAreaClicked;
    //    area.MoveToAreaClicked += this.EventArea_MoveToAreaClicked;
    //    area.EnableReminderClicked += this.EventArea_EnableReminderClicked;
    //    area.DisableReminderClicked += this.EventArea_DisableReminderClicked;
    //    area.Disposed += this.EventArea_Disposed;

    //    _ = this._areas.AddOrUpdate(configuration.Name, area, (name, prev) => area);
    //}

    private void EventArea_Disposed(object sender, EventArgs e)
    {
        //var area = sender as EventArea;
        //area.CopyToAreaClicked -= this.EventArea_CopyToAreaClicked;
        //area.MoveToAreaClicked -= this.EventArea_MoveToAreaClicked;
        //area.EnableReminderClicked -= this.EventArea_EnableReminderClicked;
        //area.DisableReminderClicked -= this.EventArea_DisableReminderClicked;
        //area.Disposed -= this.EventArea_Disposed;
    }

    /// <summary>
    ///     Removes the specified area.
    /// </summary>
    /// <param name="configuration">The configuration of the area which should be removed.</param>
    //private void RemoveArea(EventAreaConfiguration configuration)
    //{
    //    this.ModuleSettings.EventAreaNames.Value = new List<string>(this.ModuleSettings.EventAreaNames.Value.Where(areaName => areaName != configuration.Name));

    //    this._areas[configuration.Name]?.Dispose();
    //    _ = this._areas.TryRemove(configuration.Name, out _);

    //    this.ModuleSettings.RemoveDrawer(configuration.Name);
    //}

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings) => new ModuleSettings(settings);

    protected override void OnSettingWindowBuild(TabbedWindow settingWindow)
    {
        settingWindow.SavesSize = true;
        settingWindow.CanResize = true;
        settingWindow.RebuildViewAfterResize = true;
        settingWindow.UnloadOnRebuild = false;
        settingWindow.MinSize = settingWindow.Size;
        settingWindow.MaxSize = new Point(settingWindow.Width * 2, settingWindow.Height * 3);
        settingWindow.RebuildDelay = 500;
        // Reorder Icon: 605018

        //this.SettingsWindow.Tabs.Add(new Tab(
        //    this.IconService.GetIcon("156736.png"),
        //    () => new GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, this.MetricsService) { DefaultColor = this.ModuleSettings.DefaultGW2Color },
        //    this.TranslationService.GetTranslation("generalSettingsView-title", "General")));

        ////this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156740.png"), () => new UI.Views.Settings.GraphicsSettingsView() { APIManager = this.Gw2ApiManager, IconService = this.IconService, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Graphic Settings"));
        //AreaSettingsView areaSettingsView = new AreaSettingsView(
        //    () => this._areas.Values.Select(area => area.Configuration),
        //    () => this._eventCategories,
        //    this.ModuleSettings,
        //    this.AccountService,
        //    this.Gw2ApiManager,
        //    this.IconService,
        //    this.TranslationService,
        //    this.SettingEventService,
        //    this.EventStateService)
        //{ DefaultColor = this.ModuleSettings.DefaultGW2Color };
        //areaSettingsView.AddArea += (s, e) =>
        //{
        //    e.AreaConfiguration = this.AddArea(e.Name);
        //    if (e.AreaConfiguration != null)
        //    {
        //        EventArea newArea = this._areas.Values.Where(x => x.Configuration.Name == e.Name).First();
        //        this.SetAreaEvents(newArea);
        //    }
        //};

        //areaSettingsView.RemoveArea += (s, e) =>
        //{
        //    this.RemoveArea(e);
        //};

        //areaSettingsView.SyncEnabledEventsToReminders += (s, e) =>
        //{
        //    this.ModuleSettings.ReminderDisabledForEvents.Value = new List<string>(e.DisabledEventKeys.Value);
        //    return Task.CompletedTask;
        //};

        //areaSettingsView.SyncEnabledEventsFromReminders += (s, e) =>
        //{
        //    e.DisabledEventKeys.Value = new List<string>(this.ModuleSettings.ReminderDisabledForEvents.Value);
        //    return Task.CompletedTask;
        //};

        //areaSettingsView.SyncEnabledEventsToOtherAreas += (s, e) =>
        //{
        //    if (this._areas == null) throw new ArgumentNullException(nameof(this._areas), "Areas are not available.");

        //    foreach (EventArea area in this._areas.Values)
        //    {
        //        if (area.Configuration.Name == e.Name) continue;

        //        area.Configuration.DisabledEventKeys.Value = new List<string>(e.DisabledEventKeys.Value);
        //    }

        //    return Task.CompletedTask;
        //};

        //this.SettingsWindow.Tabs.Add(new Tab(
        //    this.IconService.GetIcon("605018.png"),
        //    () => areaSettingsView,
        //    this.TranslationService.GetTranslation("areaSettingsView-title", "Event Areas")));
    }

    protected override string GetDirectoryName() => "buff_reminder";



    protected override Collection<ManagedService> GetAdditionalServices(string directoryPath)
    {
        Collection<ManagedService> additionalServices = new Collection<ManagedService>();

        return additionalServices;
    }

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconService.GetIcon("102392.png");
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconService.GetIcon("102392.png");
    }

    protected override AsyncTexture2D GetErrorCornerIcon()
    {
        return this.IconService.GetIcon("102392.png");
    }

    protected override void Unload()
    {
        this.Logger.Debug("Unload module.");

        GameService.ArcDps.RawCombatEvent -= this.ArcDps_RawCombatEvent;

        this.Logger.Debug("Unload drawer.");

        //if (this._areas != null)
        //{
        //    foreach (EventArea area in this._areas.Values)
        //    {
        //        area?.Dispose();
        //    }

        //    this._areas?.Clear();
        //}

        this.Logger.Debug("Unloaded drawer.");

        this.Logger.Debug("Unload base.");

        base.Unload();

        this.Logger.Debug("Unloaded base.");
    }

    protected override int CornerIconPriority => 1_289_351_264;

}