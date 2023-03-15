namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Input;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.State;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public abstract class BaseView : View
{
    protected int CONTROL_X_SPACING { get; set; } = 20;
    protected int LABEL_WIDTH { get; set; } = 250;

    private static readonly Logger Logger = Logger.GetLogger<BaseView>();

    protected static List<Gw2Sharp.WebApi.V2.Models.Color> Colors { get; set; }

    private CancellationTokenSource _messageCancellationTokenSource;

    protected Gw2ApiManager APIManager { get; }
    protected BitmapFont Font { get; }
    public Gw2Sharp.WebApi.V2.Models.Color DefaultColor { get; set; }

    protected IconState IconState { get; }
    protected TranslationState TranslationState { get; }
    protected Panel MainPanel { get; private set; }

    public BaseView(Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, BitmapFont font = null)
    {
        this.APIManager = apiManager;
        this.IconState = iconState;
        this.TranslationState = translationState;
        this.Font = font ?? GameService.Content.DefaultFont16;
    }

    protected sealed override async Task<bool> Load(IProgress<string> progress)
    {
        if (Colors == null)
        {
            progress.Report(this.TranslationState.GetTranslation("baseView-loadingColors", "Loading Colors..."));

            try
            {
                if (this.APIManager != null)
                {
                    var colors = await this.APIManager.Gw2ApiClient.V2.Colors.AllAsync();
                    Colors = colors.ToList();
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
            //WidthSizingMode = SizingMode.Fill,
            //HeightSizingMode = SizingMode.Fill,
            AutoSizePadding = new Point(15, 15),
            Parent = buildPanel
        };

        this.MainPanel = parentPanel;

        try
        {
            this.InternalBuild(parentPanel);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"Failed building view {this.GetType().FullName}");
        }
    }

    protected abstract void InternalBuild(Panel parent);

    protected void RenderEmptyLine(Panel parent, int height = 25)
    {
        new Panel()
        {
            Parent = parent,
            Height = height,
            WidthSizingMode = SizingMode.Fill
        };
    }

    protected TextBox RenderTextbox(Panel parent, Point location, int width, string value, string placeholder, Action<string> onChangeAction = null, Action<string> onEnterAction = null, bool clearOnEnter = false)
    {
        TextBox textBox = new TextBox
        {
            Parent = parent,
            PlaceholderText = placeholder,
            Text = value,
            Location = location,
            Width = width,
            Font = this.Font
        };

        if (onChangeAction != null)
        {
            textBox.TextChanged += (s, e) =>
            {
                var scopeTextBox = s as TextBox;
                onChangeAction?.Invoke(scopeTextBox.Text);
            };
        }

        if (onEnterAction != null)
        {
            textBox.EnterPressed += (s, e) =>
            {
                var scopeTextBox = s as TextBox;

                onEnterAction?.Invoke(scopeTextBox.Text);

                if (clearOnEnter)
                {
                    textBox.Text = string.Empty;
                }
            };
        }

        return textBox;
    }

    protected TrackBar RenderTrackBar(Panel parent, Point location, int width, int value, (int Min, int Max)? range = null, Action<int> onChangeAction = null)
    {

        TrackBar trackBar = new TrackBar
        {
            Parent = parent,
            Location = location,
            Width = width,
        };

        trackBar.MinValue = range?.Min ?? 0;
        trackBar.MaxValue = range?.Max ?? 100;

        trackBar.Value = value;

        if (onChangeAction != null)
        {
            trackBar.ValueChanged += (s, e) =>
            {
                var scopeTrackBar = s as TrackBar;
                onChangeAction?.Invoke((int)scopeTrackBar.Value);
            };
        }

        return trackBar;
    }

    protected TrackBar RenderTrackBar(Panel parent, Point location, int width, float value, (float Min, float Max)? range = null, Action<float> onChangeAction = null)
    {

        TrackBar trackBar = new TrackBar
        {
            Parent = parent,
            SmallStep = true,
            Location = location,
            Width = width,
        };

        trackBar.MinValue = range?.Min ?? 0f;
        trackBar.MaxValue = range?.Max ?? 1f;

        trackBar.Value = value;

        if (onChangeAction != null)
        {
            trackBar.ValueChanged += (s, e) =>
            {
                var scopeTrackBar = s as TrackBar;
                onChangeAction?.Invoke(scopeTrackBar.Value);
            };
        }

        return trackBar;
    }

    protected Checkbox RenderCheckbox(Panel parent, Point location, bool value, Action<bool> onChangeAction = null)
    {

        Checkbox checkBox = new Checkbox
        {
            Parent = parent,
            Checked = value,
            Location = location,
        };

        if (onChangeAction != null)
        {
            checkBox.CheckedChanged += (s, e) =>
            {
                var scopeCheckbox = s as Checkbox;
                onChangeAction?.Invoke(scopeCheckbox.Checked);
            };
        }

        return checkBox;
    }

    protected Dropdown RenderDropdown(Panel parent, Point location, int width, string[] values, string value, Action<string> onChangeAction = null)
    {
        Dropdown dropdown = new Dropdown
        {
            Parent = parent,
            Width = width,
            Location = location,
        };

        if (values != null)
        {
            foreach (var valueToAdd in values)
            {
                dropdown.Items.Add(valueToAdd);
            }

            dropdown.SelectedItem = value;
        }

        if (onChangeAction != null)
        {
            dropdown.ValueChanged += (s, e) =>
            {
                var scopeDropdown = s as Dropdown;
                onChangeAction?.Invoke(scopeDropdown.SelectedItem);
            };
        }

        return dropdown;
    }

    protected Shared.Controls.KeybindingAssigner RenderKeybinding(Panel parent, Point location, int width, KeyBinding value, Action<KeyBinding> onChangeAction = null)
    {
        Shared.Controls.KeybindingAssigner keybindingAssigner = new Shared.Controls.KeybindingAssigner(false)
        {
            Parent = parent,
            Width = width,
            Location = location,
            KeyBinding = value,
        };

        if (onChangeAction != null)
        {
            keybindingAssigner.BindingChanged += (s, e) =>
            {
                var scopeKeybindingAssigner = s as Shared.Controls.KeybindingAssigner;
                onChangeAction?.Invoke(scopeKeybindingAssigner.KeyBinding);
            };
        }

        return keybindingAssigner;
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

    protected Label GetLabel(Panel parent, string text, Color? color = null, BitmapFont font= null)
    {
        font ??= this.Font;
        return new Label()
        {
            Parent = parent,
            Text = text,
            Font = font ,
            TextColor = color ?? Color.White,
            AutoSizeHeight = !string.IsNullOrWhiteSpace(text),
            Width = (int)font.MeasureString(text).Width + 20
        };
    }

    private StandardButton BuildButton(Panel parent, string text, Func<bool> disabledCallback = null)
    {
        StandardButton button = new StandardButton()
        {
            Parent = parent,
            Text = text,
            Enabled = !disabledCallback?.Invoke() ?? true,
        };

        int measuredWidth = (int)this.Font.MeasureString(text).Width + 10;

        if (button.Width < measuredWidth)
        {
            button.Width = measuredWidth;
        }

        return button;
    }

    protected StandardButton RenderButton(Panel parent, string text, Action action, Func<bool> disabledCallback = null)
    {
        StandardButton button = this.BuildButton(parent, text, disabledCallback);

        button.Click += (s, e) =>
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed executing action:");
                this.ShowError(ex.Message);
            }
        };

        return button;
    }

    protected StandardButton RenderButtonAsync(Panel parent, string text, Func<Task> action, Func<bool> disabledCallback = null)
    {
        StandardButton button = this.BuildButton(parent, text, disabledCallback);

        button.Click += (s, e) => Task.Run(async () =>
        {
            try
            {
                button.Enabled = false;
                await action?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed executing action:");
                this.ShowError(ex.Message);
            }
            finally
            {
                button.Enabled = true;
            }
        });

        return button;
    }

    protected (Label TitleLabel, Label ValueLabel) RenderLabel(Panel parent, string title, string value = null, Color? textColorTitle = null, Color? textColorValue = null)
    {
        Panel panel = this.GetPanel(parent);

        Label titleLabel = this.GetLabel(panel, title, color: textColorTitle);

        Label valueLabel = null;

        if (value != null)
        {
            valueLabel = this.GetLabel(panel, value, color: textColorValue);
            valueLabel.Left = titleLabel.Right + CONTROL_X_SPACING;
        }
        else
        {
            titleLabel.AutoSizeWidth = true;
        }

        return (titleLabel, valueLabel);

    }

    protected ColorBox RenderColorBox(Panel parent, Point location, Gw2Sharp.WebApi.V2.Models.Color initialColor, Action<Gw2Sharp.WebApi.V2.Models.Color> onChange, Panel selectorPanel = null, Thickness? innerSelectorPanelPadding = null)
    {
        Panel panel = this.GetPanel(parent);

        ColorBox colorBox = new ColorBox()
        {
            Location = location,
            Parent = panel,
            Color = initialColor
        };

        var selectorPanelCreated = selectorPanel == null;

        if (selectorPanel == null)
        {
            selectorPanel = this.GetPanel(parent);
            selectorPanel.Visible = false;
        }

        ColorPicker colorPicker = new ColorPicker()
        {
            Parent = selectorPanel,
            ZIndex = int.MaxValue,
            Visible = false,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.Fill,
            Padding = innerSelectorPanelPadding ?? Thickness.Zero,
            AssociatedColorBox = colorBox,
        };

        if (Colors != null)
        {
            foreach (Gw2Sharp.WebApi.V2.Models.Color color in Colors.OrderBy(color => color.Categories.FirstOrDefault()))
            {
                colorPicker.Colors.Add(color);
            }
        }

        colorBox.LeftMouseButtonPressed += (s, e) =>
        {
            if (selectorPanelCreated)
            {
                selectorPanel.Visible = true;
            }

            colorPicker.Visible = true;

            try
            {
                colorPicker.DoUpdate(null); // This is kinda painful.

                // Hack to get lineup right
                Gw2Sharp.WebApi.V2.Models.Color tempColor = new Gw2Sharp.WebApi.V2.Models.Color()
                {
                    Id = int.MaxValue,
                    Name = "temp"
                };

                colorPicker.RecalculateLayout();
                colorPicker.Colors.Add(tempColor);
                colorPicker.Colors.Remove(tempColor);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Hacky colorpicker resize failed.. Nothing to prevent this..");
            }

        };

        colorPicker.SelectedColorChanged += (sender, eArgs) =>
        {
            var colorPicker = sender as ColorPicker;

            Gw2Sharp.WebApi.V2.Models.Color selectedColor = colorPicker.SelectedColor;

            onChange?.Invoke(selectedColor);

            if (selectorPanelCreated)
            {
                selectorPanel.Visible = false;
            }

            colorPicker.Visible = false;
        };

        return colorBox;
    }

    private void ShowMessage(string message, Color color, int durationMS, BitmapFont font = null)
    {
        _messageCancellationTokenSource?.Cancel();
        _messageCancellationTokenSource = new CancellationTokenSource();

        font ??= this.Font;

        var textSize = font.MeasureString(message);

        var messagePanel = new Panel();
        messagePanel.HeightSizingMode = SizingMode.Standard;
        messagePanel.Height = (int)textSize.Height;
        messagePanel.WidthSizingMode = SizingMode.Standard;
        messagePanel.Width = (int)textSize.Width + 10;

        messagePanel.Location = new Point((this.MainPanel.Width / 2) - (messagePanel.Width / 2), this.MainPanel.Bottom - messagePanel.Height);

        _ = this.GetLabel(messagePanel, message, color: color, font: font);

        messagePanel.Parent = this.MainPanel;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(durationMS, _messageCancellationTokenSource.Token);
            }
            catch (Exception) { }

            messagePanel.Dispose();
        });
    }

    protected void ShowError(string message)
    {
        this.ShowMessage(message, Color.Red, 5000, GameService.Content.DefaultFont18);
    }

    protected void ShowInfo(string message)
    {
        this.ShowMessage(message, Color.White, 2500);
    }

    protected override void Unload()
    {
        this.MainPanel?.Children?.ToList().ForEach(c => c?.Dispose());
        this.MainPanel?.Children?.Clear();

        this.MainPanel?.Dispose();
        this.MainPanel = null;
    }
}
