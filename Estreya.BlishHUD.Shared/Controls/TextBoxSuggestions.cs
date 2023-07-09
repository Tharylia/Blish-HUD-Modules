namespace Estreya.BlishHUD.Shared.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Extensions;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

public class TextBoxSuggestions : FlowPanel
{
    public enum CaseMatchingMode
    {
        IgnoreCase
    }

    public enum SuggestionMode
    {
        StartsWith,
        Contains
    }

    private Container _attachToParent;
    private TextBox _textBox;

    private bool UpdateFromCode;

    /// <summary>
    ///     Creates a TextBox with suggestions attached to it.
    ///     <para>The ZIndex of this control needs to be higher than other control for the suggestions to work properly.</para>
    /// </summary>
    public TextBoxSuggestions(TextBox textBox, Container attachToParent)
    {
        this._textBox = textBox;
        this._attachToParent = attachToParent;

        this.CanScroll = true;
        this.BackgroundColor = Color.Black;
        this.FlowDirection = ControlFlowDirection.SingleTopToBottom;

        this._textBox.TextChanged += this.TextBox_TextChanged;
        this._textBox.Resized += this._textBox_Resized;
        this._textBox.Moved += this._textBox_Moved;
        this._textBox.InputFocusChanged += this._textBox_InputFocusChanged;

        GameService.Input.Mouse.LeftMouseButtonPressed += this.Mouse_LeftMouseButtonPressed;
    }

    public int MaxHeight { get; set; } = 400;

    public string[] Suggestions { get; set; }
    public SuggestionMode Mode { get; set; } = SuggestionMode.StartsWith;

    public StringComparison StringComparison { get; set; } = StringComparison.InvariantCulture;

    private void _textBox_InputFocusChanged(object sender, ValueEventArgs<bool> e)
    {
        if (e.Value)
        {
            this.TextBox_TextChanged(this._textBox, EventArgs.Empty);
        }
    }

    private void _textBox_Moved(object sender, MovedEventArgs e)
    {
        this.UpdateSizeAndLocation();
    }

    private void _textBox_Resized(object sender, ResizedEventArgs e)
    {
        this.UpdateSizeAndLocation();
    }

    private void Mouse_LeftMouseButtonPressed(object sender, MouseEventArgs e)
    {
        if (!this.MouseOver)
        {
            this.RemoveSuggestionList();
        }
    }

    private void TextBox_TextChanged(object sender, EventArgs e)
    {
        if (this.Suggestions == null || this.Suggestions.Length == 0 || this.UpdateFromCode)
        {
            return;
        }

        TextBox textBox = sender as TextBox;

        string currentText = textBox.Text.Trim();

        List<string> suggestions = null;

        if (!string.IsNullOrWhiteSpace(currentText))
        {
            suggestions = this.Suggestions.Where(completionItem =>
            {
                return this.Mode switch
                {
                    SuggestionMode.StartsWith => completionItem.StartsWith(currentText, this.StringComparison),
                    SuggestionMode.Contains => completionItem.Contains(currentText, this.StringComparison),
                    _ => false
                };
            }).Take(50).ToList();
        }

        if (suggestions != null && suggestions.Count > 0)
        {
            this.BuildSuggestionList(suggestions);
        }
        else
        {
            this.RemoveSuggestionList();
        }
    }

    private void BuildSuggestionList(List<string> suggestions)
    {
        this.ClearChildren();
        this.Parent = this._attachToParent;

        foreach (string suggestion in suggestions)
        {
            Label label = new Label
            {
                Parent = this,
                Text = suggestion,
                Width = this.Width
            };

            label.Click += this.Label_Click;
        }

        int heightSum = this.Children.Sum(child => child.Height);
        this.Height = MathHelper.Clamp(heightSum, 0, this.MaxHeight);
    }

    private void RemoveSuggestionList()
    {
        this.Children.Select(child => child as Label).ToList().ForEach(label => label.Click -= this.Label_Click);
        this.ClearChildren();
        this.Parent = null;
    }

    private void Label_Click(object sender, MouseEventArgs e)
    {
        Label label = sender as Label;

        try
        {
            this.UpdateFromCode = true;

            this._textBox.Text = label.Text;

            this.RemoveSuggestionList();
        }
        finally
        {
            this.UpdateFromCode = false;
        }
    }

    protected override void DisposeControl()
    {
        this.RemoveSuggestionList();

        this._textBox.TextChanged -= this.TextBox_TextChanged;
        this._textBox.Resized -= this._textBox_Resized;
        this._textBox.Moved -= this._textBox_Moved;
        this._textBox.InputFocusChanged -= this._textBox_InputFocusChanged;
        GameService.Input.Mouse.LeftMouseButtonPressed -= this.Mouse_LeftMouseButtonPressed;

        this.Suggestions = null;
        this._attachToParent = null;
        this._textBox = null;

        base.DisposeControl();
    }

    private void UpdateSizeAndLocation()
    {
        this.Location = new Point(this._textBox.Left, this._textBox.Bottom);
        this.Width = this._textBox.Width;

        this.Children.ToList().ForEach(child => child.Width = this.Width);
    }
}