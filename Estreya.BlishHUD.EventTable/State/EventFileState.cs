namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.EventTable.Models.Settings;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Threading;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    public class EventFileState : ManagedState
    {
        private const string WEB_SOURCE_URL = $"{EventTableModule.WEBSITE_FILE_ROOT_URL}/event-table/events.json";

        public event EventHandler<NewEventFileVersionArgs> NewVersionAvailable;
        public event EventHandler Updated;

        private TimeSpan updateInterval = TimeSpan.FromHours(1);
        private AsyncRef<double> timeSinceUpdate = 0;

        private static object _lockObject = new object();

        private bool _notified = false;

        private string _filePath => Path.Combine(this._directoryPath, this._fileName);

        private readonly string _directoryPath;
        private readonly string _fileName;

        public EventFileState(StateConfiguration configuration, string directoryPath, string fileName) : base(configuration)
        {
            this._directoryPath = directoryPath;
            this._fileName = fileName;
        }

        protected override async Task InternalReload()
        {
            await this.CheckAndNotifyOrUpdate(null);
        }

        protected override async Task Initialize()
        {
            if (!this.LocalFileExists())
            {
                await this.ExportFile();
            }

            this.timeSinceUpdate = this.updateInterval.TotalMilliseconds;
        }

        protected override void InternalUnload() { }

        protected override void InternalUpdate(GameTime gameTime)
        {
            _ = UpdateUtil.UpdateAsync(this.CheckAndNotifyOrUpdate, gameTime, this.updateInterval.TotalMilliseconds, this.timeSinceUpdate);
        }

        protected override Task Load() => Task.CompletedTask;

        protected override Task Save() => Task.CompletedTask;

        private async Task CheckAndNotifyOrUpdate(GameTime gameTime)
        {
            lock (_lockObject)
            {
                if (this._notified)
                {
                    return;
                }
            }

            var onlineFile = await this.GetOnlineFile();

            if (await this.IsNewFileVersionAvaiable(onlineFile))
            {
                var autoUpdate = EventTableModule.ModuleInstance.ModuleSettings.AutomaticallyUpdateEventFile.Value;

                var localFile = await this.GetLocalFile();
                this.NewVersionAvailable?.Invoke(this, new NewEventFileVersionArgs()
                {
                    OldVersion = localFile.Version,
                    NewVersion = onlineFile.Version,
                    AlreadyNotified = _notified,
                    IsSelfUpdate = autoUpdate
                });

                if (autoUpdate)
                {
                    await this.ExportFile(onlineFile);
                    this.Updated?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    lock (_lockObject)
                    {
                        this._notified = true;
                    }
                }
            }
        }

        private bool LocalFileExists()
        {
            try
            {
                return File.Exists(this._filePath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Check for existing external file failed: {ex.Message}");
                throw ex;
            }
        }

        public async Task<EventSettingsFile> GetOnlineFile()
        {
            try
            {
                Logger.Debug("Loading json from web source.");

                string webJson = await EventTableModule.ModuleInstance.WebClient.DownloadStringTaskAsync(new Uri(WEB_SOURCE_URL));

                Logger.Debug($"Got content (length): {webJson?.Length ?? 0}");

                if (!string.IsNullOrWhiteSpace(webJson))
                {
                    EventSettingsFile webEventSettingFile = JsonConvert.DeserializeObject<EventSettingsFile>(webJson);
                    if (webEventSettingFile.MinimumModuleVersion.IsSatisfied(EventTableModule.ModuleInstance.Version.BaseVersion()))
                    {
                        Logger.Debug("Module statisfies min web file version");
                        return webEventSettingFile;
                    }
                    else
                    {
                        Logger.Error("Module does not statisfy min web file version");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not load internal file.");
            }

            return null;
        }
        public async Task<EventSettingsFile> GetLocalFile()
        {
            try
            {
                string content = await FileUtil.ReadStringAsync(this._filePath);
                return JsonConvert.DeserializeObject<EventSettingsFile>(content);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not load external file.");
            }

            return null;
        }

        private async Task<bool> IsNewFileVersionAvaiable()
        {
            EventSettingsFile onlineFile = await this.GetOnlineFile();

            return await this.IsNewFileVersionAvaiable(onlineFile);
        }

        private async Task<bool> IsNewFileVersionAvaiable(EventSettingsFile onlineFile)
        {
            try
            {
                EventSettingsFile localFile = await this.GetLocalFile();

                return onlineFile?.Version > localFile?.Version;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to check for new file version: {ex.Message}");
                return false;
            }
        }

        internal async Task ExportFile(EventSettingsFile eventSettingsFile)
        {
            eventSettingsFile ??= new EventSettingsFile();

            string content = JsonConvert.SerializeObject(eventSettingsFile, Formatting.Indented);
            await FileUtil.WriteStringAsync(this._filePath, content);

            await this.Clear();
        }

        public async Task ExportFile()
        {
            EventSettingsFile eventSettingsFile = await this.GetOnlineFile();
            await this.ExportFile(eventSettingsFile);
        }

        protected override Task Clear()
        {
            lock (_lockObject)
            {
                this._notified = false;
            }

            return Task.CompletedTask;
        }
    }

    public class NewEventFileVersionArgs
    {
        public bool IsSelfUpdate { get; set; }
        public bool AlreadyNotified { get; set; }
        public SemVer.Version OldVersion { get; set; }
        public SemVer.Version NewVersion { get; set; }
    }
}
