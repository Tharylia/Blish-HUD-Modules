namespace Estreya.BlishHUD.UniversalSearch;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Estreya.BlishHUD.Shared.Modules;
using Estreya.BlishHUD.Shared.Settings;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.UniversalSearch.Controls;
using Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using Estreya.BlishHUD.UniversalSearch.UI.Views;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    public override string WebsiteModuleName => "universal-search";

    protected override string API_VERSION_NO => "1";

    private LandmarkSearchHandler _landmarkSearchHandler;
    private SkillSearchHandler _skillSearchHandler;
    private TraitSearchHandler _traitSearchHandler;

    private StandardWindow _searchWindow;

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        return new ModuleSettings(settings);
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconState.GetIcon("358373.png");
    }

    protected override string GetDirectoryName() => "universal_search";

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconState.GetIcon("358373.png");
    }

    protected override async Task LoadAsync()
    {
        await base.LoadAsync();

        var traits = await this.Gw2ApiManager.Gw2ApiClient.V2.Traits.AllAsync();
        var skills = await this.Gw2ApiManager.Gw2ApiClient.V2.Skills.AllAsync(); // Don't use skillState until json convert with render url is fixed

        this._landmarkSearchHandler = new LandmarkSearchHandler(new List<Shared.Models.GW2API.PointOfInterest.PointOfInterest>(), this.IconState);
        this._skillSearchHandler = new SkillSearchHandler(skills.Select(s => Skill.FromAPISkill(s)).ToList(), this.IconState);
        this._traitSearchHandler = new TraitSearchHandler(traits.ToList(), this.IconState);

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
            AsyncTexture2D windowBackground = this.IconState.GetIcon(@"textures\setting_window_background.png");

            Rectangle settingsWindowSize = new Rectangle(35, 26, 1100, 714);
            int contentRegionPaddingY = settingsWindowSize.Y - 15;
            int contentRegionPaddingX = settingsWindowSize.X;
            Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 6, settingsWindowSize.Height - contentRegionPaddingY);

            var searchWindowEmblem = this.IconState.GetIcon("358373.png");

            this._searchWindow = new StandardWindow(windowBackground, settingsWindowSize, contentRegion)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = GameService.Graphics.SpriteScreen.Size / new Point(2) - new Point(256, 178) / new Point(2),
                Title = "Universal Search",
                SavesPosition = true,
                Id = $"{nameof(UniversalSearchModule)}_090afc97-559c-4f1d-8196-0b77f5d0a9c9",
            };

            if (searchWindowEmblem.HasSwapped)
            {
                this._searchWindow.Emblem = searchWindowEmblem;
            }else
            {
                searchWindowEmblem.TextureSwapped += this.SearchWindowEmblem_TextureSwapped;
            }

            this._searchWindow.Size = new Point(512, 256);
        }
    }

    private void SearchWindowEmblem_TextureSwapped(object sender, ValueChangedEventArgs<Texture2D> e)
    {
        this._searchWindow.Emblem = e.NewValue;
    }

    protected override void OnBeforeStatesStarted()
    {
        //this.SkillState.Updated += this.SkillState_Updated;
        this.PointOfInterestState.Updated += this.PointOfInterestState_Updated;
    }

    private void PointOfInterestState_Updated(object sender, System.EventArgs e)
    {
        this._landmarkSearchHandler.UpdateSearchItems(this.PointOfInterestState.PointOfInterests.ToArray().ToList());
    }

    private void SkillState_Updated(object sender, System.EventArgs e)
    {
        this._skillSearchHandler.UpdateSearchItems(this.SkillState.Skills.Where(s => s.Category == Shared.Models.GW2API.Skills.SkillCategory.Skill).ToList());
    }

    protected override void ConfigureStates(StateConfigurations configurations)
    {
        //configurations.Skills.Enabled = true;
        //configurations.Skills.AwaitLoading = false;
        //configurations.Skills.UpdateInterval = Timeout.InfiniteTimeSpan;

        configurations.PointOfInterests.Enabled = true;
        configurations.PointOfInterests.AwaitLoading = false;
        configurations.PointOfInterests.UpdateInterval = Timeout.InfiniteTimeSpan;
    }

    protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
    {
        settingWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156736.png"), () => new UI.Views.Settings.GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconState, this.TranslationState, this.SettingEventState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));
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
                if (this._searchWindow.CurrentView == null)
                {
                    var view = new SearchWindowView(new SearchHandler[] {this._landmarkSearchHandler, this._skillSearchHandler, this._traitSearchHandler }, this.ModuleSettings, this.Gw2ApiManager, this.IconState, this.TranslationState) { DefaultColor = this.ModuleSettings.DefaultGW2Color };
                    view.RequestClose += this.View_RequestClose;

                    _searchWindow.Show(view);
                }
                else
                {
                    this._searchWindow.Show();
                }
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
        base.Unload();

        //this.SkillState.Updated -= this.SkillState_Updated;
        this.PointOfInterestState.Updated -= this.PointOfInterestState_Updated;

        if (_searchWindow != null && _searchWindow.CurrentView is SearchWindowView swv)
        {
            swv.RequestClose -= this.View_RequestClose;
        }

        _searchWindow?.Dispose();
        _searchWindow = null;
    }
}
