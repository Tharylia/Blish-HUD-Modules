namespace Estreya.BlishHUD.UniversalSearch;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Json.Converter;
using Estreya.BlishHUD.Shared.Modules;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Settings;
using Estreya.BlishHUD.Shared.Utils;
using Estreya.BlishHUD.UniversalSearch.Controls;
using Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using Estreya.BlishHUD.UniversalSearch.UI.Views;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[Export(typeof(Blish_HUD.Modules.Module))]
public class UniversalSearchModule : BaseModule<UniversalSearchModule, ModuleSettings>
{
    [ImportingConstructor]
    public UniversalSearchModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    public override string UrlModuleName => "universal-search";

    protected override string API_VERSION_NO => "1";

    private LandmarkSearchHandler _landmarkSearchHandler;
    private SkillSearchHandler _skillSearchHandler;
    private TraitSearchHandler _traitSearchHandler;
    private AchievementSearchHandler _achievementSearchHandler;

    private Shared.Controls.StandardWindow _searchWindow;

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        return new ModuleSettings(settings);
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconService.GetIcon("358373.png");
    }

    protected override string GetDirectoryName() => "universal_search";

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconService.GetIcon("358373.png");
    }

    protected override async Task LoadAsync()
    {
        await base.LoadAsync();

        this.LoadSearchHandlers();
    }

    private void InitializeSearchHandlers()
    {
        this._landmarkSearchHandler = new LandmarkSearchHandler(new List<Shared.Models.GW2API.PointOfInterest.PointOfInterest>(), this.ModuleSettings.AddSearchHandler("landmarks", "Landmarks"), this.IconService);

        this._skillSearchHandler = new SkillSearchHandler(new List<Shared.Models.GW2API.Skills.Skill>(), this.ModuleSettings.AddSearchHandler("skills", "Skills"), this.IconService);

        this._traitSearchHandler = new TraitSearchHandler(new List<Gw2Sharp.WebApi.V2.Models.Trait>(), this.ModuleSettings.AddSearchHandler("traits", "Traits"), this.IconService);

        this._achievementSearchHandler = new AchievementSearchHandler(new List<Gw2Sharp.WebApi.V2.Models.Achievement>(), this.ModuleSettings.AddSearchHandler("achievements", "Achievements"), this.IconService);
    }

    private void LoadSearchHandlers()
    {
        try
        {
            this.ReportLoading("traits", "Loading traits...");

            _ = this.Gw2ApiManager.Gw2ApiClient.V2.Traits.AllAsync().ContinueWith(t =>
            {
                this.ReportLoading("traits", null);

                if (t.IsFaulted) throw t.Exception;

                this._traitSearchHandler.UpdateSearchItems(t.Result);
            });
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Traits could not be loaded.");
        }
    }

    protected override void OnModuleLoaded(EventArgs e)
    {
        base.OnModuleLoaded(e);
        this.CreateSearchWindow();
    }

    private void CreateSearchWindow()
    {
        if (this._searchWindow == null)
        {
            this._searchWindow = WindowUtil.CreateStandardWindow(this.ModuleSettings, "Universal Search", this.GetType(), Guid.Parse("090afc97-559c-4f1d-8196-0b77f5d0a9c9"), this.IconService, this.IconService.GetIcon("358373.png"));

            this._searchWindow.Parent = GameService.Graphics.SpriteScreen;
            this._searchWindow.Location = GameService.Graphics.SpriteScreen.Size / new Point(2) - new Point(256, 178) / new Point(2);
            this._searchWindow.SavesPosition = true;
            this._searchWindow.SavesSize = true;
            this._searchWindow.CanResize = true;
            this._searchWindow.RebuildViewAfterResize = true;
            this._searchWindow.UnloadOnRebuild = false;
            this._searchWindow.Size = new Point(512, 256);
            this._searchWindow.MinSize = this._searchWindow.Size;
            this._searchWindow.MaxSize = new Point(this._searchWindow.Width * 2, this._searchWindow.Height * 3);
            this._searchWindow.Hidden += this.SearchWindow_Hidden;

            this.UpdateSearchWindowView();
        }
    }

    private void SearchWindow_Hidden(object sender, EventArgs e)
    {
        if (!this.ShowUI) return; // Window was not closed by user

        this.ModuleSettings.GlobalDrawerVisible.Value = false;
    }

    private void UpdateSearchWindowView()
    {
        if (_searchWindow != null && _searchWindow.CurrentView is SearchWindowView swv)
        {
            swv.RequestClose -= this.View_RequestClose;
        }

        var view = new SearchWindowView(this.GetSearchHandlers(), this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService) { DefaultColor = this.ModuleSettings.DefaultGW2Color };
        view.RequestClose += this.View_RequestClose;

        _searchWindow.SetView(view);
    }

    private void SearchWindowEmblem_TextureSwapped(object sender, ValueChangedEventArgs<Texture2D> e)
    {
        this._searchWindow.Emblem = e.NewValue;
    }

    protected override void OnBeforeServicesStarted()
    {
        this.InitializeSearchHandlers();

        this.SkillService.Updated += this.SkillState_Updated;
        this.PointOfInterestService.Updated += this.PointOfInterestState_Updated;
        this.AchievementService.Updated += this.AchievementService_Updated;
    }

    private void AchievementService_Updated(object sender, EventArgs e)
    {
        this._achievementSearchHandler.UpdateSearchItems(this.AchievementService.Achievements);
    }

    private void PointOfInterestState_Updated(object sender, System.EventArgs e)
    {
        this._landmarkSearchHandler.UpdateSearchItems(this.PointOfInterestService.PointOfInterests.ToArray().ToList());
    }

    private void SkillState_Updated(object sender, System.EventArgs e)
    {
        this._skillSearchHandler.UpdateSearchItems(this.SkillService.Skills.Where(s => s.Category == Shared.Models.GW2API.Skills.SkillCategory.Skill).ToList());
    }

    protected override void ConfigureServices(ServiceConfigurations configurations)
    {
        configurations.Skills.Enabled = true;
        configurations.Skills.AwaitLoading = false;
        configurations.Skills.UpdateInterval = Timeout.InfiniteTimeSpan;

        configurations.PointOfInterests.Enabled = true;
        configurations.PointOfInterests.AwaitLoading = false;
        configurations.PointOfInterests.UpdateInterval = Timeout.InfiniteTimeSpan;

        configurations.Achievements.Enabled = true;
        configurations.Achievements.AwaitLoading = false;

        configurations.Items.Enabled = true;
        configurations.Items.AwaitLoading = false; 
    }

    private IEnumerable<SearchHandler> GetSearchHandlers()
    {
        return new SearchHandler[]
        {
            this._achievementSearchHandler,
            this._landmarkSearchHandler,
            this._skillSearchHandler,
            this._traitSearchHandler
        };
    }

    protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
    {
        var generalSettingsView = new UI.Views.Settings.GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color };
        generalSettingsView.ReloadServicesRequested += this.GeneralSettingsView_ReloadServicesRequested;

        settingWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156736.png"), () => generalSettingsView, "General"));
        settingWindow.Tabs.Add(new Tab(this.IconService.GetIcon("605018.png"), () => new UI.Views.Settings.SearchHandlerSettingsView(() => this.GetSearchHandlers().Select(s => s.Configuration), this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Search Handler"));
    }

    private async Task GeneralSettingsView_ReloadServicesRequested(object sender)
    {
        await this.ReloadServices();
    }

    private void ToggleWindow(bool show)
    {
        if (this._searchWindow == null) return;

        if (!this.ModuleSettings.GlobalDrawerVisible.Value)
        {
            show = false;
        }

        if (show)
        {
            if (!this._searchWindow.Visible)
            {
                this._searchWindow.Show();
            }
        }
        else
        {
            if (this._searchWindow.Visible)
            {
                this._searchWindow.Hide();
            }
        }
    }

    private void View_RequestClose(object sender, EventArgs e)
    {
        this.ModuleSettings.GlobalDrawerVisible.Value = false;
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        this.ToggleWindow(this.ShowUI);
    }

    protected override void Unload()
    {

        //this.SkillState.Updated -= this.SkillState_Updated;
        if (this.PointOfInterestService != null)
        {
            this.PointOfInterestService.Updated -= this.PointOfInterestState_Updated;
        }

        if (this.AchievementService != null)
        {
            this.AchievementService.Updated -= this.AchievementService_Updated;
        }

        if (_searchWindow != null)
        {
            this._searchWindow.Hidden -= this.SearchWindow_Hidden;

            if (_searchWindow.CurrentView is SearchWindowView swv)
            {
                swv.RequestClose -= this.View_RequestClose;
            }
        }

        _searchWindow?.Dispose();
        _searchWindow = null;

        foreach (var searchHandler in this.GetSearchHandlers())
        {
            searchHandler?.Dispose();
        }

        base.Unload();
    }
}
