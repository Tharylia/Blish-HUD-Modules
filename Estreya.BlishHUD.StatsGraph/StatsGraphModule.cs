namespace Estreya.BlishHUD.StatsGraph;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Modules;
using Estreya.BlishHUD.Shared.Settings;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Estreya.BlishHUD.StatsGraph.Controls;
using Flurl.Http;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ScottPlot;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Documents;

[Export(typeof(Blish_HUD.Modules.Module))]
public class StatsGraphModule : BaseModule<StatsGraphModule, ModuleSettings>
{
    private GraphControl _control;

    private Plot _plot;

    [ImportingConstructor]
    public StatsGraphModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    public override string WebsiteModuleName => "stats-graph";

    protected override string API_VERSION_NO => "1";

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        return new ModuleSettings(settings);
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconState.GetIcon("textures/webhook.png");
    }

    protected override string GetDirectoryName() => null;

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconState.GetIcon("textures/webhook.png");
    }

    protected override async Task LoadAsync()
    {
        await base.LoadAsync();
        this.ModuleSettings.ShowAxisValues.SettingChanged += this.ShowAxisValues_SettingChanged;
        this.ModuleSettings.ShowCategoryNames.SettingChanged += this.ShowCategoryNames_SettingChanged;
        this.ModuleSettings.Zoom.SettingChanged += this.Zoom_SettingChanged;
        this.ModuleSettings.Scale.SettingChanged += this.Scale_SettingChanged;
        this.ModuleSettings.LocationX.SettingChanged += this.LocationChanged;
        this.ModuleSettings.LocationY.SettingChanged += this.LocationChanged;
        this.ModuleSettings.Size.SettingChanged += this.Size_SettingChanged;

        this._control = new GraphControl();
        this._control.Parent = GameService.Graphics.SpriteScreen;
        this.LocationChanged(this, null);

        this.UpdateData();
    }

    private void Size_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.UpdateData();
    }

    private void LocationChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this._control.Location = new Microsoft.Xna.Framework.Point(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value);
    }

    private void Scale_SettingChanged(object sender, ValueChangedEventArgs<float> e)
    {
        this.UpdateData();
    }

    private void Zoom_SettingChanged(object sender, ValueChangedEventArgs<float> e)
    {
        this.UpdateData();
    }

    private void ShowCategoryNames_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        this.UpdateData();
    }

    private void ShowAxisValues_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        this.UpdateData();
    }

    private void UpdateData()
    {
        this.CreatePlot();
        var texture = this.RenderPlot(this.GetValues());
        this._control.UpdateTexture(texture);
    }

    private double[,] GetValues()
    {
        double[,] values = {
            { 500, 750, 600, 400, 1500, 1700,200,1200, 700 },
        };

        return values;
    }

    private void CreatePlot()
    {
        this._plot = new ScottPlot.Plot();

        double[,] values = {
            {0,0,0,0,0,0,0,0,0 },
        };

        double[] maxValues = {
            2000, // Power
            2000, // Precision
            2000, // Toughness
            2000, // Vitality
            2000, // Concentration
            2000, // Condition Damage
            2000, // Expertise
            2000, // Ferocity
            2000, // Healing Power
        };

        var backgroundColorTemp = Microsoft.Xna.Framework.Color.White * 0.2f;
        var backgroundColor = System.Drawing.Color.FromArgb(backgroundColorTemp.A, backgroundColorTemp.R, backgroundColorTemp.G, backgroundColorTemp.B);

        _plot.Style(dataBackground: System.Drawing.Color.Transparent, figureBackground: backgroundColor);

        var radar = _plot.AddRadar(values, independentAxes: true, maxValues: maxValues);
        radar.CategoryLabels = new string[] { "Power", "Precision", "Toughness", "Vitality", "Concentration", "Condition Damage", "Expertise", "Ferocity", "Healing Power" };
        radar.AxisType = ScottPlot.RadarAxis.Polygon;
    }

    private Texture2D RenderPlot(double[,] values)
    {
        _plot.AxisZoom(this.ModuleSettings.Zoom.Value);

        var radar = _plot.GetPlottables().First() as RadarPlot;
        radar.ShowAxisValues = this.ModuleSettings.ShowAxisValues.Value;
        radar.ShowCategoryLabels = this.ModuleSettings.ShowCategoryNames.Value;
        radar.Update(values);

        Bitmap bitmap = new Bitmap(this.ModuleSettings.Size.Value, this.ModuleSettings.Size.Value);

        _plot.Render(bitmap, scale: this.ModuleSettings.Scale.Value);
        using MemoryStream memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
        var texture = this.GetTextureFromStream(memoryStream);
        return texture;
    }

    private Texture2D GetTextureFromStream(MemoryStream memoryStream)
    {
        using var ctx = GameService.Graphics.LendGraphicsDeviceContext();
        return Texture2D.FromStream(ctx.GraphicsDevice, memoryStream);
    }

    protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
    {
        settingWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156736.png"), () => new UI.Views.GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconState, this.TranslationState, this.SettingEventState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        this.ModuleSettings.UpdateSizeAndLocationRanges();
    }

    protected override void Unload()
    {
        base.Unload();

        this._control?.Dispose();
        this.ModuleSettings.ShowAxisValues.SettingChanged -= this.ShowAxisValues_SettingChanged;
        this.ModuleSettings.ShowCategoryNames.SettingChanged -= this.ShowCategoryNames_SettingChanged;
        this.ModuleSettings.Zoom.SettingChanged -= this.Zoom_SettingChanged;
        this.ModuleSettings.Scale.SettingChanged -= this.Scale_SettingChanged;
    }
}
