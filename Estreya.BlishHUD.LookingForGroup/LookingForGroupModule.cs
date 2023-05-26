namespace Estreya.BlishHUD.LookingForGroup;


using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Flurl.Http;
using Gw2Sharp.Models;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Xna.Framework;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TabbedWindow = Shared.Controls.TabbedWindow;

/// <summary>
/// The event table module class.
/// </summary>
[Export(typeof(Module))]
public class LookingForGroupModule : BaseModule<LookingForGroupModule, ModuleSettings>
{
    [ImportingConstructor]
    public LookingForGroupModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
    {
    }

    public override string UrlModuleName => "looking-for-group";
    
    protected override string API_VERSION_NO => "1";

    protected override async Task LoadAsync()
    {
        Stopwatch sw = Stopwatch.StartNew();
        
        await base.LoadAsync();

        sw.Stop();
        this.Logger.Debug($"Loaded in {sw.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}ms");
    }

    

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        
    }

    
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

    }

    protected override string GetDirectoryName() => null;

    protected override void ConfigureServices(ServiceConfigurations configurations)
    {
        configurations.Account.Enabled = true;
        configurations.Account.AwaitLoading = true;
    }


    protected override Collection<ManagedService> GetAdditionalServices(string directoryPath)
    {
        return null;
    }

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconService.GetIcon(this.IsPrerelease ? "textures/emblem_demo.png" : "102392.png");
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconService.GetIcon($"textures/event_boss_grey{(this.IsPrerelease ? "_demo" : "")}.png");
    }

    protected override void Unload()
    {
        this.Logger.Debug("Unload module.");

        this.Logger.Debug("Unload base.");

        base.Unload();

        this.Logger.Debug("Unloaded base.");
    }

    protected override int CornerIconPriority => 1_289_351_278;
}