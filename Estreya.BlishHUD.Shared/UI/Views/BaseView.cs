namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Resources;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.UI.Views.Controls;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

public abstract class BaseView : View
{
    private const int LEFT_PADDING = 20;
    private const int CONTROL_X_SPACING = 20;
    private const int LABEL_WIDTH = 250;
    private const int BINDING_WIDTH = 170;

    private static readonly Logger Logger = Logger.GetLogger<BaseView>();

    protected static IEnumerable<Gw2Sharp.WebApi.V2.Models.Color> Colors { get; set; }

    protected static Panel ColorPickerPanel { get; set; }

    protected static ColorPicker ColorPicker { get; set; }

    protected string SelectedColorSetting { get; set; }
    private Container BuildPanel { get; set; }
    private Panel ErrorPanel { get; set; }
    private CancellationTokenSource ErrorCancellationTokenSource = new CancellationTokenSource();

    public Gw2ApiManager APIManager { get; init; }
    public Gw2Sharp.WebApi.V2.Models.Color DefaultColor { get; init; }
    public BitmapFont Font { get; init; }

    public IconState IconState { get; init; }

    protected sealed override async Task<bool> Load(IProgress<string> progress)
    {
        if (Colors == null)
        {
            progress.Report(Strings.BaseSettingsView_LoadingColors);

            try
            {
                if (this.APIManager != null)
                {
                    Colors = await this.APIManager.Gw2ApiClient.V2.Colors.AllAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not load gw2 colors: {ex.Message}");
                if (this.DefaultColor != null)
                {
                    Logger.Debug($"Adding default color: {this.DefaultColor.Name}");
                    Colors = new List<Gw2Sharp.WebApi.V2.Models.Color>() { this.DefaultColor };
                }
            }
        }

        if (ColorPicker == null)
        {
            progress.Report(Strings.BaseSettingsView_LoadingColorPicker);
            // build initial colorpicker

            ColorPickerPanel = new Panel()
            {
                Location = new Point(10, 10),
                WidthSizingMode = SizingMode.AutoSize,
                HeightSizingMode = SizingMode.AutoSize,
                Visible = false,
                ZIndex = int.MaxValue,
                BackgroundColor = Color.Black,
                ShowBorder = false,
            };

            ColorPicker = new ColorPicker()
            {
                Location = new Point(10, 10),
                Parent = ColorPickerPanel,
                HeightSizingMode = SizingMode.AutoSize,
                WidthSizingMode = SizingMode.AutoSize,
                CanScroll = true,
                Visible = true
            };

            progress.Report(Strings.BaseSettingsView_AddingColorsToColorPicker);
            if (Colors != null)
            {
                foreach (Gw2Sharp.WebApi.V2.Models.Color color in Colors.OrderBy(color => color.Categories.FirstOrDefault()))
                {
                    ColorPicker.Colors.Add(color);
                }
            }
        }

        progress.Report(string.Empty);

        return await this.InternalLoad(progress);
    }

    protected abstract Task<bool> InternalLoad(IProgress<string> progress);

    protected sealed override void Build(Container buildPanel)
    {
        Rectangle bounds = buildPanel.ContentRegion;

        Panel parentPanel = new Panel()
        {
            Size = bounds.Size,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.Fill,
            AutoSizePadding = new Point(15, 15),
            Parent = buildPanel
        };

        this.BuildPanel = parentPanel;
        this.RegisterErrorPanel(buildPanel);

        this.DoBuild(parentPanel);
    }

    protected abstract void DoBuild(Panel parent);

    protected void RenderEmptyLine(Panel parent, int height = 25)
    {
        ViewContainer settingContainer = new ViewContainer()
        {
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            Parent = parent
        };

        settingContainer.Show(new EmptyLineView(height));
    }

    protected Panel RenderProperty<TObject, TProperty>(Panel parent, TObject obj, Expression<Func<TObject, TProperty>> expression, Func<TObject, bool> isEnabled, (float Min, float Max)? range = null, string title = null, string description = null, int width = -1)
    {
        return this.RenderPropertyWithValidation<TObject, TProperty>(parent, obj, expression, isEnabled, null, range, title, description, width);
    }

    protected Panel RenderPropertyWithValidation<TObject, TProperty>(Panel parent, TObject obj, Expression<Func<TObject, TProperty>> expression, Func<TObject, bool> isEnabled, Func<TProperty, (bool Valid, string Message)> validationFunction, (float Min, float Max)? range = null, string title = null, string description = null, int width = -1)
    {
        return this.RenderPropertyWithChangedTypeValidation<TObject, TProperty, TProperty>(parent, obj, expression, isEnabled, validationFunction, range, title, description, width);
    }

    protected Panel RenderPropertyWithChangedTypeValidation<TObject, TProperty, TOverrideType>(Panel parent, TObject obj, Expression<Func<TObject, TProperty>> expression, Func<TObject, bool> isEnabled, Func<TOverrideType, (bool Valid, string Message)> validationFunction, (float Min, float Max)? range = null, string title = null, string description = null, int width = -1)
    {
        Panel panel = this.GetPanel(parent);

        Label label = this.GetLabel(panel, title ?? string.Empty);

        try
        {
            Control ctrl = ControlHandler.CreateFromPropertyWithChangedTypeValidation(obj, expression, isEnabled, (TOverrideType val) =>
            {
                (bool Valid, string Message) validationResult = validationFunction != null ? validationFunction.Invoke(val) : (true, null);
                if (!validationResult.Valid)
                {
                    this.ShowError(validationResult.Message);
                }

                return validationResult.Valid;
            }, range, width == -1 ? BINDING_WIDTH : width, -1, label.Right + CONTROL_X_SPACING, 0);
            ctrl.Parent = panel;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Type \"{typeof(TProperty).FullName}\" with override \"{typeof(TOverrideType).FullName}\" could not be found in internal type lookup:");
        }

        return panel;
    }

    protected Panel RenderTextbox(Panel parent, string description, string placeholder, Action<string> onEnterAction)
    {
        Panel panel = this.GetPanel(parent);

        Label label = this.GetLabel(panel, description);

        try
        {
            TextBox textBox = (TextBox)ControlHandler.Create<string>(BINDING_WIDTH, -1, label.Right + CONTROL_X_SPACING, 0);
            textBox.Parent = panel;
            textBox.BasicTooltipText = description;
            textBox.PlaceholderText = placeholder;
            textBox.EnterPressed += (s, e) =>
            {
                onEnterAction?.Invoke(textBox.Text);
                textBox.Text = string.Empty;
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Type \"{typeof(string).FullName}\" could not be found in internal type lookup:");
        }

        return panel;
    }

    protected Panel GetPanel(Container parent)
    {
        return new Panel
        {
            HeightSizingMode = SizingMode.AutoSize,
            WidthSizingMode = SizingMode.AutoSize,
            Parent = parent
        };
    }

    protected Label GetLabel(Panel parent, string text)
    {
        return new Label()
        {
            Parent = parent,
            Text = text,
            AutoSizeHeight = true,
            Width = LABEL_WIDTH
        };
    }

    private StandardButton BuildButton(Panel parent, string text, Func<bool> disabledCallback = null)
    {
        StandardButton button = new StandardButton()
        {
            Parent = parent,
            Text = text,
            //Width = (int)EventTableModule.ModuleInstance.Font.MeasureString(text).Width + 10,
            Enabled = !disabledCallback?.Invoke() ?? true,
        };

        var measuredWidth = (int)(this.Font ?? GameService.Content.DefaultFont14).MeasureString(text).Width + 10;

        if (button.Width < measuredWidth)
        {
            button.Width = measuredWidth;
        }

        return button;
    }

    protected StandardButton RenderButton(Panel parent, string text, Action action, Func<bool> disabledCallback = null)
    {
        var button = this.BuildButton(parent, text, disabledCallback);

        button.Click += (s, e) =>
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        };

        return button;
    }

    protected StandardButton RenderButtonAsync(Panel parent, string text, Func<Task> action, Func<bool> disabledCallback = null)
    {
        var button = this.BuildButton(parent, text, disabledCallback);

        button.Click += (s, e) => Task.Run(async () =>
        {
            try
            {
                await action.Invoke();
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });

        return button;
    }

    protected StandardButton RenderButtonAsyncWait(Panel parent, string text, Func<Task> action, Func<bool> disabledCallback = null)
    {
        var button = this.BuildButton(parent, text, disabledCallback);

        button.Click += (s, e) => 
        {
            try
            {
                AsyncHelper.RunSync(action);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        };

        return button;
    }

    protected (Label TitleLabel, Label ValueLabel) RenderLabel(Panel parent, string title, string value = null, Color? textColorTitle = null, Color? textColorValue = null)
    {
        Panel panel = this.GetPanel(parent);

        Label titleLabel = this.GetLabel(panel, title);
        titleLabel.TextColor = textColorTitle ?? titleLabel.TextColor;

        Label valueLabel = null;

        if (value != null)
        {
            valueLabel = this.GetLabel(panel, value);
            valueLabel.Left = titleLabel.Right + CONTROL_X_SPACING;
            valueLabel.TextColor = textColorValue ?? valueLabel.TextColor;
        }
        else
        {
            titleLabel.AutoSizeWidth = true;
        }

        return (titleLabel,valueLabel);

    }

    protected ColorBox RenderColor(Panel parent, Gw2Sharp.WebApi.V2.Models.Color defaultColor, string identifierKey, Action<Gw2Sharp.WebApi.V2.Models.Color> onChange)
    {
        Panel panel = this.GetPanel(parent);

        ColorBox colorBox = new ColorBox()
        {
            Location = new Point(0, 0),
            Parent = panel,
            Color = defaultColor
        };

        colorBox.LeftMouseButtonPressed += (s, e) =>
        {
            ColorPickerPanel.Parent = parent;
            ColorPickerPanel.HeightSizingMode = SizingMode.Fill;
            ColorPickerPanel.WidthSizingMode = SizingMode.Fill;
            //ColorPickerPanel.Size = new Point(panel.Width - 30, 850);

            // Hack to get lineup right
            Gw2Sharp.WebApi.V2.Models.Color tempColor = new Gw2Sharp.WebApi.V2.Models.Color()
            {
                Id = int.MaxValue,
                Name = "temp"
            };

            ColorPicker.RecalculateLayout();
            ColorPicker.Colors.Add(tempColor);
            ColorPicker.Colors.Remove(tempColor);
            ColorPicker.RecalculateLayout();

            ColorPickerPanel.Visible = !ColorPickerPanel.Visible;
            this.SelectedColorSetting = identifierKey;
        };

        ColorPicker.SelectedColorChanged += (sender, eArgs) =>
        {
            if (this.SelectedColorSetting != identifierKey)
            {
                return;
            }

            Gw2Sharp.WebApi.V2.Models.Color selectedColor = ColorPicker.SelectedColor;

            onChange?.Invoke(selectedColor);
            ColorPickerPanel.Visible = false;
            ColorPickerPanel.Parent = null;
            colorBox.Color = selectedColor;
        };

        return colorBox;
    }

    private void RegisterErrorPanel(Container parent)
    {
        Panel panel = this.GetPanel(parent);
        panel.ZIndex = 1000;
        panel.WidthSizingMode = SizingMode.Fill;
        panel.Visible = false;

        this.ErrorPanel = panel;
    }

    public async void ShowError(string message)
    {
        lock (this.ErrorPanel)
        {
            if (this.ErrorPanel.Visible)
            {
                this.ErrorCancellationTokenSource.Cancel();
                this.ErrorCancellationTokenSource = new CancellationTokenSource();
            }
        }

        this.ErrorPanel.ClearChildren();
        BitmapFont font = GameService.Content.DefaultFont32;
        message = DrawUtil.WrapText(font, message, this.ErrorPanel.Width * (3f / 4f));

        Label label = this.GetLabel(this.ErrorPanel, message);
        label.Width = this.ErrorPanel.Width;

        label.Font = font;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.TextColor = Color.Red;

        this.ErrorPanel.Height = label.Height;
        this.ErrorPanel.Bottom = this.BuildPanel.ContentRegion.Bottom;

        lock (this.ErrorPanel)
        {
            this.ErrorPanel.Show();
        }

        try
        {
            await Task.Delay(5000, this.ErrorCancellationTokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            Logger.Debug("Task was canceled to show new error:");
        }

        lock (this.ErrorPanel)
        {
            this.ErrorPanel.Hide();
        }
    }

    protected override void Unload()
    {
        base.Unload();
        this.ErrorPanel.Dispose();
    }
}
