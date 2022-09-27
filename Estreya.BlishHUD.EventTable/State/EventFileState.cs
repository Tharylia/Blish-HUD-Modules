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

        private TimeSpan updateInterval = TimeSpan.FromHours(1);
        private AsyncRef<double> timeSinceUpdate = 0;

        private static object _lockObject = new object();

        private bool _notified = false;

        private string _filePath => Path.Combine(this._directoryPath, this._fileName);

        private readonly string _directoryPath;
        private readonly string _fileName;

        public EventFileState(StateConfiguration configuration, string directoryPath, string fileName):base(configuration)
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
            if (!this.ExternalFileExists())
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
                return File.Exists(this._filePath);
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
        public async Task<EventSettingsFile> GetExternalFile()
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
            await FileUtil.WriteStringAsync(this._filePath, content);

            await this.Clear();
        }

        public async Task ExportFile()
        {
            EventSettingsFile eventSettingsFile = await this.GetInternalFile();
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
}
