namespace Estreya.BlishHUD.StartupNotifications;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.StartupNotifications.UI.Views;
using Flurl.Util;
using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Models;
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
using System.Threading;
using System.Threading.Tasks;

[Export(typeof(Module))]
public class StartupNotificationsModule : BaseModule<StartupNotificationsModule, ModuleSettings>
{
    [ImportingConstructor]
    public StartupNotificationsModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    protected override string UrlModuleName => "startup-notifications";

    protected override string API_VERSION_NO => "1";

    protected override bool NeedsBackend => false;

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override async Task LoadAsync()
    {
        await base.LoadAsync();
        await this.LoadAndShowNotifications();
    }

    private async Task LoadAndShowNotifications()
    {
        var directoryPath = this.DirectoriesManager.GetFullDirectoryPath(this.GetDirectoryName());
        var files = Directory.GetFiles(directoryPath, "*.txt", SearchOption.TopDirectoryOnly).ToList();
        if (files.Count == 0)
        {
            files.Add(await this.CreateDummyFile(directoryPath));
        }

        var resetEvent = new ManualResetEvent(true);
        var cancellationTokenSource = new CancellationTokenSource();
        foreach (var file in files)
        {
            resetEvent.Reset();

            var content = await FileUtil.ReadStringAsync(file);
            if (string.IsNullOrWhiteSpace(content))
            {
                this.Logger.Warn($"Content of file \"{file}\" is empty.");
                continue;
            }

            var notification = ScreenNotification.ShowNotification(content, this.ModuleSettings.Type.Value, duration: this.ModuleSettings.Duration.Value);

            if (this.ModuleSettings.AwaitEach.Value)
            {
                EventHandler<EventArgs> notificationDisposedHandler = (object s, EventArgs e) =>
                {
                    resetEvent.Set();
                };

                notification.Disposed += notificationDisposedHandler;

                _ = await resetEvent.WaitOneAsync(TimeSpan.FromSeconds(60), cancellationTokenSource.Token);

                notification.Disposed -= notificationDisposedHandler;
            }
        }
    }

    private async Task<string> CreateDummyFile(string directoryPath)
    {
        var filePath = Path.Combine(directoryPath, "01_example.txt");

        await FileUtil.WriteStringAsync(filePath, "[Startup Notifications] Change me");

        return filePath;
    }

    /// <inheritdoc />
    protected override void Unload()
    {
        base.Unload();
    }

    public override IView GetSettingsView()
    {
        return new SettingsView(this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, this.ModuleSettings);
    }

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        return new ModuleSettings(settings);
    }

    protected override string GetDirectoryName()
    {
        return "startup";
    }

    protected override AsyncTexture2D GetEmblem()
    {
        return null;
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return null;
    }

    protected override int CornerIconPriority => 1_289_351_265;
}