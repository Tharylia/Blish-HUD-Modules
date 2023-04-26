namespace Estreya.BlishHUD.ArcDPSLogManager
{
    using Blish_HUD;
    using Blish_HUD.ArcDps.Common;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Modules;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.ArcDPSLogManager.Controls;
    using Estreya.BlishHUD.ArcDPSLogManager.EliteInsights;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Models.ArcDPS.Buff;
    using Estreya.BlishHUD.Shared.Modules;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.Services;
    using Estreya.BlishHUD.Shared.Threading;
    using Estreya.BlishHUD.Shared.Utils;
    using Flurl.Util;
    using GW2EIEvtcParser;
    using GW2EIGW2API;
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
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Media.Animation;
    using System.Xml.Linq;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class ArcDPSLogManagerModule : BaseModule<ArcDPSLogManagerModule, ModuleSettings>
    {
        private string ARCDPS_LOG_PATH;

        public override string WebsiteModuleName => "arcdps-log-manager";

        protected override string API_VERSION_NO => "1";

        private FileSystemWatcher _watcher;
        private TabbedWindow2 _managerWindow;
        private GW2APIController _parserAPIController;

        private List<GW2EIEvtcParser.ParsedEvtcLog> _logs = new List<ParsedEvtcLog>();

        [ImportingConstructor]
        public ArcDPSLogManagerModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void Initialize()
        {
            base.Initialize();
        }

        private EvtcParser GetParser()
        {
            return new GW2EIEvtcParser.EvtcParser(new GW2EIEvtcParser.EvtcParserSettings(false, false, false, false, false, 2000), this._parserAPIController); ;
        }


        protected override async Task LoadAsync()
        {
            await base.LoadAsync();

            this._parserAPIController = new GW2APIController();


            var gamePath = @"C:\Program Files\Guild Wars 2\Gw2-64.exe";

            if (string.IsNullOrWhiteSpace(gamePath) || !File.Exists(gamePath))
            {
                throw new ArgumentNullException(nameof(gamePath), "Could not determine gw2 executeable path.");
            }

            var arcSettingPath = Path.Combine(Path.GetDirectoryName(gamePath), "addons", "arcdps", "arcdps.ini");
            if (!File.Exists(arcSettingPath)) throw new ArgumentNullException(nameof(arcSettingPath), "arcdps.ini does not exist.");

            var iniContent = await FileUtil.ReadLinesAsync(arcSettingPath);
            if (iniContent == null) throw new ArgumentNullException(nameof(iniContent), "arcpds.ini could not be read.");

            var logPathEntry = iniContent.FirstOrDefault(c => c.StartsWith("boss_encounter_path"));
            if (logPathEntry == null) throw new ArgumentNullException(nameof(logPathEntry), "arcdps.ini does not contain logpath");
            var logPath = logPathEntry.Split('=')[1];
            this.ARCDPS_LOG_PATH = Path.Combine(logPath, "arcdps.cbtlogs");

            this.WatchDirectory(this.ARCDPS_LOG_PATH);

            _ = Task.Factory.StartNew(this.LoadLogs, TaskCreationOptions.LongRunning);
        }

        private async Task LoadLogs()
        {
            this.ReportLoading("logs", "Loading logs...");

            var files = Directory.GetFiles(this.ARCDPS_LOG_PATH, "*.*", SearchOption.AllDirectories).ToArray();

            string directoryName = this.GetDirectoryName() ?? throw new ArgumentNullException(nameof(GetDirectoryName), "No directory defined");
            var path = Path.Combine(this.DirectoriesManager.GetFullDirectoryPath(directoryName), "cache");

            IProgress<string> progress = new Progress<string>(status => this.ReportLoading("logs", $"Loading logs: {status}"));
            var operation = new EliteInsights.ParserController(null);
            var loadedLogs = 0;
            try
            {

                var tasks = new List<Task<ParsedEvtcLog>>();

                Parallel.ForEach(files.ChunkBy(25), new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 4

                }, fileChunk =>
                {
                    try
                    {
                        foreach (var file in fileChunk)
                        {
                            var log = this.GetParser().ParseLog(operation, new FileInfo(file), out var parsingFailureReason, true);
                            if (parsingFailureReason == null)
                            {
                                this._logs.Add(log);
                            }

                            var result = Interlocked.Increment(ref loadedLogs);

                            progress.Report($"{result}/{files.Length}");
                        }
                    }
                    catch (Exception)
                    {
                    }
                });



                var t = "";

            }
            catch (Exception ex)
            {
                this.Logger.Warn(ex, "Failed to parse logs:");
            }



            this.ReportLoading("logs", null);
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            base.OnModuleLoaded(e);

            this.CreateManagerWindow();
        }

        private void CreateManagerWindow()
        {
            this._managerWindow ??= WindowUtil.CreateTabbedWindow("ArcDPS Log Manager", this.GetType(), Guid.Parse("6a7d6c6b-3554-4e44-a478-0620b185ec61"), this.IconService);
            this._managerWindow.Visible = false;
            //this._managerWindow.CanResize = true;
            //this._managerWindow.Size = new Point(2000, 1200);

            this._managerWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156736.png"),
                () => new UI.Views.GeneralSettingsView(
                    this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, GameService.Content.DefaultFont16)
                { DefaultColor = this.ModuleSettings.DefaultGW2Color }
                , "General"));
        }

        private void WatchDirectory(string path)
        {
            this._watcher = new FileSystemWatcher
            {
                Path = path,
                Filter = "*.*",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
            };

            _watcher.Renamed += this.OnRenamedArcDPSLog;
            _watcher.Created += this.OnNewArcDPSLog;
            _watcher.Deleted += this.OnRemovedArcDPSLog;

            this.Logger.Info($"Started watching path '{path}'");
        }

        private void OnRenamedArcDPSLog(object sender, RenamedEventArgs e)
        {
            this.Logger.Debug($"Renamed logfile: {JsonConvert.SerializeObject(e)}");
        }

        private void OnNewArcDPSLog(object sender, FileSystemEventArgs e)
        {
            this.Logger.Debug($"New logfile: {JsonConvert.SerializeObject(e)}");
        }
        private void OnRemovedArcDPSLog(object sender, FileSystemEventArgs e)
        {
            this.Logger.Debug($"Removed logfile: {JsonConvert.SerializeObject(e)}");
        }

        protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
        {
            this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156736.png"), () => new UI.Views.GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            this.HandleManagerWindowVisibility();
        }

        private void HandleManagerWindowVisibility()
        {
            if (this.ShowUI && (!this._managerWindow?.Visible ?? false))
            {
                this._managerWindow?.Show();
            }
            else if (!this.ShowUI && (this._managerWindow?.Visible ?? false))
            {
                this._managerWindow?.Hide();
            }
        }

        protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
        {
            return new ModuleSettings(settings);
        }

        protected override string GetDirectoryName()
        {
            return "log_manager";
        }

        protected override AsyncTexture2D GetEmblem()
        {
            return this.IconService.GetIcon("2191071.png");// "866139.png");
        }

        protected override AsyncTexture2D GetCornerIcon()
        {
            return this.IconService.GetIcon("1377783.png");
        }

        protected override void Unload()
        {
            this._managerWindow?.Dispose();
            this._managerWindow = null;

            base.Unload();
        }
    }
}

