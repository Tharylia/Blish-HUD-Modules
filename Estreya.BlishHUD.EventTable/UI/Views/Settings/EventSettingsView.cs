namespace Estreya.BlishHUD.EventTable.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.EventTable.Helpers;
    using Estreya.BlishHUD.EventTable.Resources;
    using Estreya.BlishHUD.EventTable.Utils;
    using Newtonsoft.Json;
    using SemVer;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class EventSettingsView : BaseSettingsView
    {
        private SemVer.Version CurrentVersion = null;
        private SemVer.Version NewestVersion = null;
        public EventSettingsView(ModuleSettings settings) : base(settings)
        {
        }

        protected override void BuildView(Panel parent)
        {
            this.RenderChangedTypeSetting(parent, this.ModuleSettings.EventTimeSpan, (string val) =>
            {
                int.TryParse(val, out int result);

                return result;
            });
            this.RenderSetting(parent, this.ModuleSettings.EventHistorySplit);
            this.RenderSetting(parent, this.ModuleSettings.DrawEventBorder);
            this.RenderSetting(parent, this.ModuleSettings.UseEventTranslation);

            this.RenderEmptyLine(parent);

            this.RenderSetting(parent, this.ModuleSettings.EventCompletedAcion);

            this.RenderEmptyLine(parent);

            this.RenderButton(parent, Strings.EventSettingsView_ReloadEvents_Title, async () =>
            {
                await EventTableModule.ModuleInstance.LoadEvents();
                EventTable.Controls.ScreenNotification.ShowNotification(Strings.EventSettingsView_ReloadEvents_Success);
            });

            this.RenderEmptyLine(parent);

            this.RenderSetting(parent, this.ModuleSettings.AutomaticallyUpdateEventFile);
            this.RenderButton(parent, Strings.EventSettingsView_UpdateEventFile_Title, async () =>
            {
                await EventTableModule.ModuleInstance.EventFileState.ExportFile();
                await EventTableModule.ModuleInstance.LoadEvents();
                EventTable.Controls.ScreenNotification.ShowNotification(Strings.EventSettingsView_UpdateEventFile_Success);
            });

            this.RenderLabel(parent, Strings.EventSettingsView_CurrentVersion_Title, this.CurrentVersion?.ToString() ?? Strings.EventSettingsView_CurrentVersion_Unknown);
            this.RenderLabel(parent, Strings.EventSettingsView_NewestVersion_Title, this.NewestVersion?.ToString() ?? Strings.EventSettingsView_NewestVersion_Unknown);

            this.RenderButton(parent, "Diff in VS Code", async () =>
            {
                string filePath1 = FileUtil.CreateTempFile("json");

                var eventSettingsFile1 = await EventTableModule.ModuleInstance.EventFileState.GetExternalFile();

                await FileUtil.WriteStringAsync(filePath1, JsonConvert.SerializeObject(eventSettingsFile1, Formatting.Indented));

                string filePath2 = FileUtil.CreateTempFile("json");

                var eventSettingsFile2 = await EventTableModule.ModuleInstance.EventFileState.GetInternalFile();

                await FileUtil.WriteStringAsync(filePath2, JsonConvert.SerializeObject(eventSettingsFile2, Formatting.Indented));

                await VSCodeHelper.Diff(filePath1, filePath2);

                File.Delete(filePath1);
                File.Delete(filePath2);
            });

            this.RenderEmptyLine(parent);

            this.RenderButton(parent, Strings.EventSettingsView_ResetEventStates_Title, async () =>
            {
                await EventTableModule.ModuleInstance.EventState.Clear();
                EventTable.Controls.ScreenNotification.ShowNotification(Strings.EventSettingsView_ResetEventStates_Success);
            });

            this.RenderEmptyLine(parent);

            this.RenderSetting(parent, this.ModuleSettings.UseFiller);
            this.RenderSetting(parent, this.ModuleSettings.UseFillerEventNames);
            this.RenderColorSetting(parent, this.ModuleSettings.FillerTextColor);
        }

        protected override async Task<bool> InternalLoad(IProgress<string> progress)
        {
            this.CurrentVersion = (await EventTableModule.ModuleInstance.EventFileState.GetExternalFile())?.Version;
            this.NewestVersion = (await EventTableModule.ModuleInstance.EventFileState.GetInternalFile())?.Version;

            return true;
        }
    }
}
