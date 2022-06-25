namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.EventTable.Models.Settings;
    using Estreya.BlishHUD.EventTable.Utils;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    public class EventFileState : ManagedState
    {
        private static readonly Logger Logger = Logger.GetLogger<EventFileState>();

        private const string WEB_SOURCE_URL = $"{EventTableModule.WEBSITE_FILE_ROOT_URL}/event-table/events.json";

        private TimeSpan updateInterval = TimeSpan.FromHours(1);
        private double timeSinceUpdate = 0;

        private static object _lockObject = new object();
        private string Directory { get; set; }
        private string FileName { get; set; }

        private string FilePath => Path.Combine(this.Directory, this.FileName);

        private bool _notified = false;

        private ContentsManager ContentsManager { get; set; }

        public EventFileState(ContentsManager contentsManager, string directory, string fileName)
        {
            this.ContentsManager = contentsManager;
            this.Directory = directory;
            this.FileName = fileName;
        }

        protected override async Task InternalReload()
        {
            await this.CheckAndNotifyOrUpdate(null);
        }

        protected override async Task Initialize()
        {
            if (!this.ExternalFileExists())
            {
                await this.ExportFile();
            }

            this.timeSinceUpdate = this.updateInterval.TotalMilliseconds;
        }

        protected override void InternalUnload() { }

        protected override void InternalUpdate(GameTime gameTime)
        {
            _ = UpdateUtil.UpdateAsync(this.CheckAndNotifyOrUpdate, gameTime, this.updateInterval.TotalMilliseconds, ref this.timeSinceUpdate);
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

            if (await this.IsNewFileVersionAvaiable())
            {
                if (EventTableModule.ModuleInstance.ModuleSettings.AutomaticallyUpdateEventFile.Value)
                {
                    await this.ExportFile();
                    await EventTableModule.ModuleInstance.LoadEvents();
                }
                else
                {
                    Controls.ScreenNotification.ShowNotification(new string[]
                    {
                    "A new version of the event file is available.",
                    "Please update it from the settings window."
                    }, duration: 10);

                    lock (_lockObject)
                    {
                        this._notified = true;
                    }
                }
            }
        }

        private bool ExternalFileExists()
        {
            try
            {
                return File.Exists(this.FilePath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Check for existing external file failed: {ex.Message}");
                throw ex;
            }
        }

        public async Task<EventSettingsFile> GetInternalFile()
        {
            try
            {
                // Try fetching from web source.
                try
                {
                    Logger.Debug("Loading json from web source.");

                    string webJson = await EventTableModule.ModuleInstance.GetWebClient().DownloadStringTaskAsync(new Uri(WEB_SOURCE_URL));

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
                            Logger.Debug("Module does not statisfy min web file version");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Could not read json from web source.");
                }

                Logger.Debug("Load json from internal ref.");

                // Fall back to internal source.
                using Stream stream = this.ContentsManager.GetFileStream("events.json");
                string content = await FileUtil.ReadStringAsync(stream);
                EventSettingsFile internalEventSettingFile = JsonConvert.DeserializeObject<EventSettingsFile>(content);

                if (internalEventSettingFile.MinimumModuleVersion.IsSatisfied(EventTableModule.ModuleInstance.Version.BaseVersion()))
                {
                    Logger.Debug("Module statisfies min internal file version");
                    return internalEventSettingFile;
                }
                else
                {
                    Logger.Debug("Module does not statisfy min internal file version");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not load internal file.");
            }

            return null;
        }
        public async Task<EventSettingsFile> GetExternalFile()
        {
            try
            {
                string content = await FileUtil.ReadStringAsync(this.FilePath);
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
            try
            {
                EventSettingsFile internalEventFile = await this.GetInternalFile();
                EventSettingsFile externalEventFile = await this.GetExternalFile();

                return internalEventFile?.Version > externalEventFile?.Version;
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
            await FileUtil.WriteStringAsync(this.FilePath, content);

            await this.Clear();
        }

        public async Task ExportFile()
        {
            EventSettingsFile eventSettingsFile = await this.GetInternalFile();
            await this.ExportFile(eventSettingsFile);
        }

        public override Task Clear()
        {
            lock (_lockObject)
            {
                this._notified = false;
            }

            return Task.CompletedTask;
        }
    }
}
