namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Input;
using Blish_HUD.Modules.Managers;
using Controls;
using Estreya.BlishHUD.Shared.Models;
using Extensions;
using Gw2Sharp.WebApi.V2;
using Humanizer;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Color = Gw2Sharp.WebApi.V2.Models.Color;
using KeybindingAssigner = Controls.KeybindingAssigner;
using Thickness = Blish_HUD.Controls.Thickness;

public abstract class BaseView : View
{
    protected readonly Logger _logger;

    private CancellationTokenSource _messageCancellationTokenSource;

    public BaseView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService)
    {
        this._logger = Logger.GetLogger(this.GetType());

        this.APIManager = apiManager;
        this.IconService = iconService;
        this.TranslationService = translationService;
    }

    protected int CONTROL_X_SPACING { get; set; } = 20;
    protected int LABEL_WIDTH { get; set; } = 250;

    protected static List<Color> Colors { get; set; }

    protected Gw2ApiManager APIManager { get; }
    public Color DefaultColor { get; set; }

    protected IconService IconService { get; }
    protected TranslationService TranslationService { get; }
    protected Panel MainPanel { get; private set; }

    protected sealed override async Task<bool> Load(IProgress<string> progress)
    {
        if (Colors == null)
        {
            progress.Report(this.TranslationService.GetTranslation("baseView-loadingColors", "Loading Colors..."));

            try
            {
                if (this.APIManager != null)
                {
                    IApiV2ObjectList<Color> colors = await this.APIManager.Gw2ApiClient.V2.Colors.AllAsync();
                    Colors = colors.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not load gw2 colors: {ex.Message}");
                if (this.DefaultColor != null)
                {
                    _logger.Debug($"Adding default color: {this.DefaultColor.Name}");
                    Colors = new List<Color> { this.DefaultColor };
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

        Panel parentPanel = new Panel
        {
            Size = bounds.Size,
            //WidthSizingMode = SizingMode.Fill,
            //HeightSizingMode = SizingMode.Fill,
            //AutoSizePadding = new Point(15, 15),
            Parent = buildPanel
        };

        this.MainPanel = parentPanel;

        try
        {
            this.InternalBuild(parentPanel);
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, $"Failed building view {this.GetType().FullName}");
        }
    }

    protected abstract void InternalBuild(Panel parent);

    protected virtual Dictionary<ControlType, BitmapFont> ControlFonts { get; } = new Dictionary<ControlType, BitmapFont>()
    {
        { ControlType.Label, GameService.Content.DefaultFont16 },
        { ControlType.Dropdown, GameService.Content.DefaultFont16 },
        { ControlType.Button, GameService.Content.DefaultFont16 },
        { ControlType.TextBox, GameService.Content.DefaultFont16 }
    };

    protected void RenderEmptyLine(Panel parent, int height = 25)
    {
        new Panel
        {
            Parent = parent,
            Height = height,
            WidthSizingMode = SizingMode.Fill
        };
    }

    protected TextBox RenderTextbox(Panel parent, Point location, int width, string value, string placeholder, Action<string> onChangeAction = null, Action<string> onEnterAction = null, bool clearOnEnter = false, Func<string, string, Task<bool>> onBeforeChangeAction = null)
    {
        onBeforeChangeAction ??= (_, _) => Task.FromResult(true);
        bool changing = false;

        TextBox textBox = new TextBox
        {
            Parent = parent,
            PlaceholderText = placeholder,
            Text = value,
            Location = location,
            Width = width,
            Font = this.ControlFonts[ControlType.TextBox]
        };

        if (onChangeAction != null)
        {
            textBox.TextChanged += (s, e) =>
            {
                if (changing)
                {
                    return;
                }

                changing = true;

                TextBox scopeTextBox = s as TextBox;
                ValueChangedEventArgs<string> ea = e as ValueChangedEventArgs<string>;

                onBeforeChangeAction(ea?.PreviousValue, scopeTextBox.Text).ContinueWith(resultTask =>
                {
                    if (resultTask.Result)
                    {
                        onChangeAction?.Invoke(scopeTextBox.Text);
                    }
                    else
                    {
                        scopeTextBox.Text = ea.PreviousValue;
                    }

                    changing = false;
                });
            };
        }

        if (onEnterAction != null)
        {
            textBox.EnterPressed += (s, e) =>
            {
                TextBox scopeTextBox = s as TextBox;

                onEnterAction?.Invoke(scopeTextBox.Text);

                if (clearOnEnter)
                {
                    textBox.Text = string.Empty;
                }
            };
        }

        return textBox;
    }

    protected TrackBar RenderTrackBar(Panel parent, Point location, int width, int value, (int Min, int Max)? range = null, Action<int> onChangeAction = null, Func<int, int, Task<bool>> onBeforeChangeAction = null)
    {
        onBeforeChangeAction ??= (_, _) => Task.FromResult(true);

        TrackBar trackBar = new TrackBar
        {
            Parent = parent,
            Location = location,
            Width = width
        };

        trackBar.MinValue = range?.Min ?? 0;
        trackBar.MaxValue = range?.Max ?? 100;

        trackBar.Value = value;

        if (onChangeAction != null)
        {
            trackBar.ValueChanged += (s, e) =>
            {
                TrackBar scopeTrackBar = s as TrackBar;
                onChangeAction?.Invoke((int)scopeTrackBar.Value);
            };
        }

        return trackBar;
    }

    protected TrackBar RenderTrackBar(Panel parent, Point location, int width, float value, (float Min, float Max)? range = null, Action<float> onChangeAction = null, Func<float, float, Task<bool>> onBeforeChangeAction = null)
    {
        onBeforeChangeAction ??= (_, _) => Task.FromResult(true);

        TrackBar trackBar = new TrackBar
        {
            Parent = parent,
            SmallStep = true,
            Location = location,
            Width = width
        };

        trackBar.MinValue = range?.Min ?? 0f;
        trackBar.MaxValue = range?.Max ?? 1f;

        trackBar.Value = value;

        if (onChangeAction != null)
        {
            trackBar.ValueChanged += (s, e) =>
            {
                TrackBar scopeTrackBar = s as TrackBar;
                onChangeAction?.Invoke(scopeTrackBar.Value);
            };
        }

        return trackBar;
    }

    protected Checkbox RenderCheckbox(Panel parent, Point location, bool value, Action<bool> onChangeAction = null, Func<bool, bool, Task<bool>> onBeforeChangeAction = null)
    {
        onBeforeChangeAction ??= (_, _) => Task.FromResult(true);

        Checkbox checkBox = new Checkbox
        {
            Parent = parent,
            Checked = value,
            Location = location
        };

        if (onChangeAction != null)
        {
            checkBox.CheckedChanged += (s, e) =>
            {
                onBeforeChangeAction(!e.Checked, e.Checked).ContinueWith(resultTask =>
                {
                    Checkbox scopeCheckbox = s as Checkbox;
                    if (resultTask.Result)
                    {
                        onChangeAction?.Invoke(scopeCheckbox.Checked);
                    }
                    else
                    {
                        scopeCheckbox.Checked = !e.Checked;
                    }
                });
            };
        }

        return checkBox;
    }

    protected Controls.Dropdown<string> RenderDropdown<T>(Panel parent, Point location, int width, T? value, T[] values = null, Action<T> onChangeAction = null, Func<string, string, Task<bool>> onBeforeChangeAction = null) where T : struct, Enum
    {
        onBeforeChangeAction ??= (_, _) => Task.FromResult(true);
        LetterCasing casing = LetterCasing.Title;

        Controls.Dropdown<string> dropdown = new Controls.Dropdown<string>
        {
            Parent = parent,
            Width = width,
            Location = location,
            Font = this.ControlFonts[ControlType.Dropdown]
        };

        values ??= (T[])Enum.GetValues(typeof(T));

        string[] formattedValues = values.Select(value => value.GetTranslatedValue(this.TranslationService, casing)).ToArray();

        string selectedValue = null;
        if (value != null)
        {
            selectedValue = value.GetTranslatedValue(this.TranslationService, casing);
        }

        foreach (string valueToAdd in formattedValues)
        {
            dropdown.Items.Add(valueToAdd);
        }

        dropdown.SelectedItem = selectedValue;

        if (onChangeAction != null)
        {
            dropdown.ValueChanged += (s, e) =>
            {
                Dropdown<string> scopeDropdown = s as Dropdown<string>;
                onChangeAction?.Invoke(values[formattedValues.ToList().IndexOf(scopeDropdown.SelectedItem)]);
            };
        }

        return dropdown;
    }

    protected Controls.Dropdown<T> RenderDropdown<T>(Panel parent, Point location, int width, T[] values, T value, Action<T> onChangeAction = null, Func<T, T, Task<bool>> onBeforeChangeAction = null)
    {
        bool onBeforeChangeActionSubscribed = onBeforeChangeAction != null;
        onBeforeChangeAction ??= (_, _) => Task.FromResult(true);

        Controls.Dropdown<T> dropdown = new Controls.Dropdown<T>
        {
            Parent = parent,
            Width = width,
            Location = location,
            Font = this.ControlFonts[ControlType.Dropdown]
        };

        if (values != null)
        {
            foreach (T valueToAdd in values)
            {
                dropdown.Items.Add(valueToAdd);
            }

            dropdown.SelectedItem = value;
        }

        if (onChangeAction != null || onBeforeChangeActionSubscribed)
        {
            dropdown.ValueChanged += (s, e) =>
            {
                Controls.Dropdown<T> scopeDropdown = s as Controls.Dropdown<T>;
                onBeforeChangeAction(e.PreviousValue, e.NewValue).ContinueWith(resultTask =>
                {
                    if (resultTask.Result)
                    {
                        onChangeAction?.Invoke(scopeDropdown.SelectedItem);
                    }
                    else
                    {
                        scopeDropdown.SelectedItem = e.PreviousValue;
                    }
                });
            };
        }

        return dropdown;
    }

    protected KeybindingAssigner RenderKeybinding(Panel parent, Point location, int width, KeyBinding value, Action<KeyBinding> onChangeAction = null, Func<KeyBinding, KeyBinding, Task<bool>> onBeforeChangeAction = null)
    {
        onBeforeChangeAction ??= (_, _) => Task.FromResult(true);

        KeybindingAssigner keybindingAssigner = new KeybindingAssigner(false)
        {
            Parent = parent,
            Width = width,
            Location = location,
            KeyBinding = value
        };

        if (onChangeAction != null)
        {
            keybindingAssigner.BindingChanged += (s, e) =>
            {
                KeybindingAssigner scopeKeybindingAssigner = s as KeybindingAssigner;
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

    protected Label GetLabel(Panel parent, string text, Microsoft.Xna.Framework.Color? color = null, BitmapFont font = null)
    {
        font ??= this.ControlFonts[ControlType.Label];
        return new Label
        {
            Parent = parent,
            Text = text,
            Font = font,
            TextColor = color ?? Microsoft.Xna.Framework.Color.White,
            AutoSizeHeight = !string.IsNullOrWhiteSpace(text),
            Width = (int)font.MeasureString(text).Width + 20
        };
    }

    private Button BuildButton(Panel parent, string text, Func<bool> disabledCallback = null)
    {
        Button button = new Button
        {
            Parent = parent,
            Text = text,
            Enabled = !disabledCallback?.Invoke() ?? true,
            Font = this.ControlFonts[ControlType.Button],
        };

        int measuredWidth = (int)button.Font.MeasureString(text).Width + 10;

        if (button.Width < measuredWidth)
        {
            button.Width = measuredWidth;
        }

        return button;
    }

    protected Button RenderButton(Panel parent, string text, Action action, Func<bool> disabledCallback = null)
    {
        Button button = this.BuildButton(parent, text, disabledCallback);

        button.Click += (s, e) =>
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed executing action:");
                this.ShowError(ex.Message);
            }
        };

        return button;
    }

    protected Button RenderButtonAsync(Panel parent, string text, Func<Task> action, Func<bool> disabledCallback = null)
    {
        Button button = this.BuildButton(parent, text, disabledCallback);

        button.Click += (s, e) => Task.Run(async () =>
        {
            try
            {
                button.Enabled = false;
                await action?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed executing action:");
                this.ShowError(ex.Message);
            }
            finally
            {
                button.Enabled = true;
            }
        });

        return button;
    }

    protected FormattedLabel RenderFormattedLabel(Panel parent, Action<FormattedLabelBuilder> formattedLabelBuilderAction)
    {
        Panel panel = this.GetPanel(parent);

        var formattedLabelBuilder = new FormattedLabelBuilder()
            .SetVerticalAlignment(VerticalAlignment.Top)
            .SetHorizontalAlignment(HorizontalAlignment.Left)
            .AutoSizeHeight()
            .AutoSizeWidth();

        formattedLabelBuilderAction.Invoke(formattedLabelBuilder);

        FormattedLabel fl = formattedLabelBuilder.Build();

        fl.Parent = panel;

        return fl;
    }

    protected (Label TitleLabel, Label ValueLabel) RenderLabel(Panel parent, string title, string value = null, Microsoft.Xna.Framework.Color? textColorTitle = null, Microsoft.Xna.Framework.Color? textColorValue = null, int? valueXLocation = null)
    {
        Panel panel = this.GetPanel(parent);

        Label titleLabel = this.GetLabel(panel, title, textColorTitle);
        titleLabel.VerticalAlignment = VerticalAlignment.Middle;

        Label valueLabel = null;

        if (value != null)
        {
            valueLabel = this.GetLabel(panel, value, textColorValue);
            valueLabel.Left = valueXLocation ?? titleLabel.Right + this.CONTROL_X_SPACING;
            valueLabel.VerticalAlignment = VerticalAlignment.Middle;
        }
        else
        {
            titleLabel.AutoSizeWidth = true;
        }

        return (titleLabel, valueLabel);
    }

    protected ColorBox RenderColorBox(Panel parent, Point location, Color initialColor, Action<Color> onChange, Panel selectorPanel = null, Thickness? innerSelectorPanelPadding = null)
    {
        Panel panel = this.GetPanel(parent);

        ColorBox colorBox = new ColorBox
        {
            Location = location,
            Parent = panel,
            Color = initialColor
        };

        bool selectorPanelCreated = selectorPanel == null;

        if (selectorPanel == null)
        {
            selectorPanel = this.GetPanel(parent);
            selectorPanel.Visible = false;
        }

        ColorPicker colorPicker = new ColorPicker
        {
            Parent = selectorPanel,
            ZIndex = int.MaxValue,
            Visible = false,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.Fill,
            Padding = innerSelectorPanelPadding ?? Thickness.Zero,
            AssociatedColorBox = colorBox
        };

        if (Colors != null)
        {
            foreach (Color color in Colors.OrderBy(color => color.Categories.FirstOrDefault()))
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
                Color tempColor = new Color
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
                _logger.Warn(ex, "Hacky colorpicker resize failed.. Nothing to prevent this..");
            }
        };

        colorPicker.SelectedColorChanged += (sender, eArgs) =>
        {
            ColorPicker colorPicker = sender as ColorPicker;

            Color selectedColor = colorPicker.SelectedColor;

            onChange?.Invoke(selectedColor);

            if (selectorPanelCreated)
            {
                selectorPanel.Visible = false;
            }

            colorPicker.Visible = false;
        };

        return colorBox;
    }

    private void ShowMessage(string message, Microsoft.Xna.Framework.Color color, int durationMS, BitmapFont font = null)
    {
        this._messageCancellationTokenSource?.Cancel();
        this._messageCancellationTokenSource = new CancellationTokenSource();

        font ??= this.ControlFonts[ControlType.Label];

        Size2 textSize = font.MeasureString(message);

        Panel messagePanel = new Panel();
        messagePanel.HeightSizingMode = SizingMode.Standard;
        messagePanel.Height = (int)textSize.Height;
        messagePanel.WidthSizingMode = SizingMode.Standard;
        messagePanel.Width = (int)textSize.Width + 10;

        messagePanel.Location = new Point((this.MainPanel.Width / 2) - (messagePanel.Width / 2), this.MainPanel.Bottom - messagePanel.Height);

        _ = this.GetLabel(messagePanel, message, color, font);

        messagePanel.Parent = this.MainPanel;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(durationMS, this._messageCancellationTokenSource.Token);
            }
            catch (Exception) { }

            messagePanel.Dispose();
        });
    }

    protected void ShowError(string message)
    {
        this.ShowError(message, 5000);
    }

    protected void ShowError(string message, int durationMS)
    {
        this.ShowMessage(message, Microsoft.Xna.Framework.Color.Red, durationMS, GameService.Content.DefaultFont18);
    }

    protected void ShowInfo(string message)
    {
        this.ShowMessage(message, Microsoft.Xna.Framework.Color.White, 2500, GameService.Content.DefaultFont18);
    }

    protected override void Unload()
    {
        this.MainPanel?.Children?.ToList().ForEach(c => c?.Dispose());
        this.MainPanel?.Children?.Clear();

        this.MainPanel?.Dispose();
        this.MainPanel = null;
    }
}