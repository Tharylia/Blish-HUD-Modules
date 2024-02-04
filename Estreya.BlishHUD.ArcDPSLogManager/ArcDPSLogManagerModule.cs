namespace Estreya.BlishHUD.ArcDPSLogManager;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.ArcDPSLogManager.Models;
using Estreya.BlishHUD.ArcDPSLogManager.Processing;
using GW2EIEvtcParser;
using GW2EIEvtcParser.ParserHelpers;
using GW2EIGW2API;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Shared.Extensions;
using Shared.Modules;
using Shared.Settings;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UI.Views;
using Shared.IO;
using ParserController = EliteInsights.ParserController;
using Newtonsoft.Json.Converters;
using System.Diagnostics;
using SharpDX.Direct2D1;
using GW2EIBuilders;
using System.Collections.Concurrent;
using Estreya.BlishHUD.Shared.Threading;

[Export(typeof(Module))]
public class ArcDPSLogManagerModule : BaseModule<ArcDPSLogManagerModule, ModuleSettings>
{
    private static TimeSpan _processQueuedLogsInterval = TimeSpan.FromSeconds(5);
    private AsyncRef<double> _lastQueuedLogsProcessed = new AsyncRef<double>(_processQueuedLogsInterval.TotalMilliseconds);

    private List<LogData> _logs;
    private ConcurrentQueue<string> _logQueue;
    private AsyncLock _logLock;

    private GW2APIController _parserAPIController;

    private FileSystemWatcher _watcher;
    private string ARCDPS_LOG_PATH;

    [ImportingConstructor]
    public ArcDPSLogManagerModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    protected override int CornerIconPriority => 1_289_351_267;

    protected override string UrlModuleName => "arcdps-log-manager";

    protected override string API_VERSION_NO => "1";

    protected override void Initialize()
    {
        base.Initialize();
        this._logLock = new AsyncLock();
        this._logs = new List<LogData>();
        this._logQueue = new ConcurrentQueue<string>();
    }

    private LogProcessor GetLogProcessor(IProgress<string> progress)
    {
        var getParser = () => new EvtcParser(
            new EvtcParserSettings(
                this.ModuleSettings.AnonymousPlayers.Value,
                this.ModuleSettings.SkipFailedTries.Value,
                this.ModuleSettings.ParsePhases.Value,
                this.ModuleSettings.ParseCombatReplay.Value,
                this.ModuleSettings.ComputeDamageModifiers.Value,
                this.ModuleSettings.TooShortLimit.Value,
                this.ModuleSettings.DetailedWvW.Value
            ),
            this._parserAPIController);

        var operation = new ParserController(progress);

        return new LogProcessor(getParser, operation);
    }

    private GW2APIController GetAPIController()
    {
        string directoryName = this.GetDirectoryName() ?? throw new ArgumentNullException(nameof(this.GetDirectoryName), "No directory defined");
        string cacheDir = Path.Combine(this.DirectoriesManager.GetFullDirectoryPath(directoryName), "cache");
        var skillPath = Path.Combine(cacheDir, "skills.json");
        var specPath = Path.Combine(cacheDir, "specs.json");
        var traitPath = Path.Combine(cacheDir, "traits.json");
        var controller = new GW2APIController(skillPath, specPath, traitPath);

        return controller;
    }

    protected override async Task LoadAsync()
    {
        await base.LoadAsync();

        await Task.Factory.StartNew(() =>
        {
            this._parserAPIController = this.GetAPIController();
        }, TaskCreationOptions.LongRunning);

        string gamePath = GW2Utils.GetInstallPath();

        if (string.IsNullOrWhiteSpace(gamePath) || !File.Exists(gamePath))
        {
            throw new DirectoryNotFoundException("Could not determine gw2 executeable path.");
        }

        string arcSettingPath = Path.Combine(Path.GetDirectoryName(gamePath), "addons", "arcdps", "arcdps.ini");
        if (!File.Exists(arcSettingPath))
        {
            throw new ArgumentNullException(nameof(arcSettingPath), "arcdps.ini does not exist.");
        }

        string[] iniContent = await FileUtil.ReadLinesAsync(arcSettingPath)
            ?? throw new ArgumentNullException(nameof(iniContent), "arcpds.ini could not be read.");

        string logPathEntry = iniContent.FirstOrDefault(c => c.StartsWith("boss_encounter_path"))
            ?? throw new ArgumentNullException(nameof(logPathEntry), "arcdps.ini does not contain logpath");

        string logPath = logPathEntry.Split('=')[1];
        this.ARCDPS_LOG_PATH = Path.Combine(logPath, "arcdps.cbtlogs");

        this.WatchDirectory(this.ARCDPS_LOG_PATH);

        _ = Task.Factory.StartNew(this.LoadLogs, TaskCreationOptions.LongRunning);
    }

    private JsonSerializerSettings GetCacheSerizalizerSettings()
    {
        return new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Converters = new JsonConverter[]
            {
                new StringEnumConverter()
            }
        };
    }

    private async Task LoadLogCache()
    {
        this.ReportLoading("logs", "Loading log cache...");

        string directoryName = this.GetDirectoryName() ?? throw new ArgumentNullException(nameof(this.GetDirectoryName), "No directory defined");
        string filePath = Path.Combine(this.DirectoriesManager.GetFullDirectoryPath(directoryName), "cache", "logs.json");

        if (!File.Exists(filePath)) return;

        using FileStream stream = FileUtil.ReadStream(filePath);

        using ReadProgressStream progressStream = new ReadProgressStream(stream);
        progressStream.ProgressChanged += (s, e) => this.ReportLoading("logs", $"Parsing cache... {Math.Round(e.Progress, 0)}%");

        JsonSerializer serializer = JsonSerializer.CreateDefault(this.GetCacheSerizalizerSettings());

        using StreamReader sr = new StreamReader(progressStream);
        using JsonReader reader = new JsonTextReader(sr);

        List<LogData> entities = serializer.Deserialize<List<LogData>>(reader);

        using (await this._logLock.LockAsync())
        {
            this._logs.AddRange(entities.Where(l => File.Exists(l.FilePath)));
        }

        this.ReportLoading("logs", null);
    }

    private async Task SaveLogsToCache()
    {
        this.ReportLoading("logs", "Saving log cache...");

        string directoryName = this.GetDirectoryName() ?? throw new ArgumentNullException(nameof(this.GetDirectoryName), "No directory defined");
        string filePath = Path.Combine(this.DirectoriesManager.GetFullDirectoryPath(directoryName), "cache", "logs.json");

        string json = null;

        using (await this._logLock.LockAsync())
        {
            json = JsonConvert.SerializeObject(this._logs, Formatting.Indented, this.GetCacheSerizalizerSettings());
        }

        await FileUtil.WriteStringAsync(filePath, json);

        this.ReportLoading("logs", null);
    }

    private async Task LoadLogs()
    {
        await this.LoadLogCache();

        string[] files = Directory.GetFiles(this.ARCDPS_LOG_PATH, "*.*", SearchOption.AllDirectories).ToArray();

        var filteredFiles = files.Where(f => !this._logs.Any(l => l.FileInfo.FullName == f)).ToArray();

        foreach (var file in filteredFiles)
        {
            this._logQueue.Enqueue(file);
        }
    }

    private async Task<LogData> ParseLog(string filePath, LogProcessor processor = null, bool saveToCache = true)
    {
        if (processor == null)
        {
            IProgress<string> progress = new Progress<string>(status => this.ReportLoading("logs", $"Parsing log: {Path.GetFileName(filePath)}: {status}"));
            processor = this.GetLogProcessor(progress);
        }

        var sw = Stopwatch.StartNew();
        this.Logger.Debug($"Start parsing log \"{filePath}\"...");

        var log = processor.Parse(filePath);

        sw.Stop();
        this.Logger.Debug($"Finished parsing log \"{filePath}\": {sw.Elapsed.TotalMilliseconds}ms");

        if (this.ModuleSettings.GenerateHTMLAfterParsing.Value)
        {
            sw = Stopwatch.StartNew();
            this.Logger.Debug($"Start generating html for log \"{filePath}\"...");
            processor.ReportProgress("Generating html file");

            log._parsedEvtcLog.TryGetTarget(out var evtcLog);

            log.HTMLFilePath = processor.BuildHTML(
                evtcLog,
                log.FilePath,
                Path.Combine(this.DirectoriesManager.GetFullDirectoryPath(this.GetDirectoryName()), "html"),
                typeof(EvtcParser).Assembly.GetName().Version);

            sw.Stop();
            this.Logger.Debug($"Finished generating html for log \"{filePath}\": {sw.Elapsed.TotalMilliseconds}ms");
        }

        if (saveToCache)
        {
            processor.ReportProgress("Saving to cache");
            await this.SaveLogsToCache();
        }

        processor.ReportProgress("Finished");

        return log;
    }

    protected override void OnModuleLoaded(EventArgs e)
    {
        base.OnModuleLoaded(e);
    }

    private void WatchDirectory(string path)
    {
        this._watcher = new FileSystemWatcher
        {
            Path = path,
            Filter = "*.*",
            EnableRaisingEvents = true,
            IncludeSubdirectories = true
        };

        this._watcher.Renamed += this.OnRenamedArcDPSLog;
        this._watcher.Created += this.OnNewArcDPSLog;
        this._watcher.Deleted += this.OnRemovedArcDPSLog;

        this.Logger.Info($"Started watching path '{path}'");
    }

    private async void OnRenamedArcDPSLog(object sender, RenamedEventArgs e)
    {
        this.Logger.Debug($"Renamed logfile: {JsonConvert.SerializeObject(e)}");
        // Update log paths
        using (await this._logLock.LockAsync())
        {
            this._logs.Where(l => l.FilePath == e.OldFullPath).ToList().ForEach(l => l.FilePath = e.FullPath);
        }

        await this.SaveLogsToCache();
    }

    private void OnNewArcDPSLog(object sender, FileSystemEventArgs e)
    {
        this.Logger.Debug($"New logfile: {JsonConvert.SerializeObject(e)}");
        this._logQueue.Enqueue(e.FullPath);
    }

    private async void OnRemovedArcDPSLog(object sender, FileSystemEventArgs e)
    {
        this.Logger.Debug($"Removed logfile: {JsonConvert.SerializeObject(e)}");
        using (await this._logLock.LockAsync())
        {
            this._logs.RemoveAll(l => l.FilePath == e.FullPath);
        }

        await this.SaveLogsToCache();
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

        settingWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156736.png"), () => new GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));

        settingWindow.Tabs.Add(new Tab(this.IconService.GetIcon("605018.png"), () => new LogManagerView(this.ModuleSettings, () =>
        {
            using (this._logLock.Lock())
            {
                return this._logs.ToArray().ToList();
            }
        }, this.Gw2ApiManager, this.IconService, this.TranslationService)
        { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Logs"));
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _ = UpdateUtil.UpdateAsync(this.ProcessQueuedLogs, gameTime, _processQueuedLogsInterval.TotalMilliseconds, _lastQueuedLogsProcessed, false, TaskCreationOptions.LongRunning);
    }

    private async Task ProcessQueuedLogs()
    {
        var files = new List<string>();
        while (this._logQueue.TryDequeue(out var file))
        {
            files.Add(file);
        }

        if (files.Count == 0) return;

        this.ReportLoading("logs", "Parsing logs...");

        System.Globalization.CultureInfo savedThreadCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        int loadedLogs = 0;
        IProgress<string> progress = new Progress<string>(status => this.ReportLoading("logs", $"Parsing logs: {loadedLogs}/{files.Count} (E: {this._logs.Where(l => l.ParsingStatus == Models.Enums.ParsingStatus.Failed).Count()}) - {status}"));

        var processedLogs = new List<LogData>();

        try
        {
            var processor = this.GetLogProcessor(progress);
            foreach (var file in files)
            {
                if (this.CancellationToken.IsCancellationRequested) break;

                try
                {
                    var log = await this.ParseLog(file, processor, false);

                    if (log != null)
                    {
                        processedLogs.Add(log);
                    }

                    int result = Interlocked.Increment(ref loadedLogs);
                }
                catch (Exception)
                {
                }
            }

            if (files.Count > 0)
            {
                using (await this._logLock.LockAsync())
                {
                    this._logs.AddRange(processedLogs);
                }

                // Don't save if we didn't load anything additional
                await this.SaveLogsToCache();
            }
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed to parse logs:");
        }

        this.ReportLoading("logs", null);
        Thread.CurrentThread.CurrentCulture = savedThreadCulture;
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
        return this.IconService.GetIcon("62864.png"); // 63127.png, 66526.png
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconService.GetIcon("156702.png"); // "155054.png"
    }

    protected override void Unload()
    {
        using (this._logLock.Lock())
        {
            this._logs?.Clear();
            this._logs = null;
        }

        this._logQueue = null;

        base.Unload();
    }
}