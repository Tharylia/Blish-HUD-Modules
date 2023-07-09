namespace Estreya.BlishHUD.LookingForGroup.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Threading.Events;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class LFGEntry : Panel
{
    private readonly bool _isInGroup;

    public Models.LFGEntry Model { get; private set; }

    public event AsyncEventHandler JoinClicked;

    public LFGEntry(Models.LFGEntry model, bool isInGroup)
    {
        this.Model = model;
        this._isInGroup = isInGroup;
        this.ShowBorder = true;
    }

    public void Build()
    {
        this.Children?.Clear();


        var descriptionLbl = new Label()
        {
            Parent = this,
            Left = 5,
            Top = 10,
            Text = this.Model.Description,
            Height = this.ContentRegion.Height - 20,
            VerticalAlignment = VerticalAlignment.Top,
            Font = GameService.Content.DefaultFont18
        };

        var playerCountLbl = new Label()
        {
            Text = $"{this.Model.Players.Length}/{this.Model.MaxCount}",
            Parent = this,
            Width = 50,
            Top = 10,
            Font = GameService.Content.DefaultFont18
        };

        var playerCountLblWidth = (int)Math.Ceiling(playerCountLbl.Font.MeasureString(playerCountLbl.Text).Width);
        playerCountLbl.Width = playerCountLblWidth;
        playerCountLbl.Right = this.ContentRegion.Right - 15;

        var joinButton = new Button()
        {
            Parent = this,
            Text = "Join",
            Right = playerCountLbl.Right,
            Bottom = this.ContentRegion.Bottom - 20,
            Visible = !this._isInGroup
        };

        joinButton.Click += this.JoinButton_Click;

        if (_isInGroup)
        {
            var currentGroupLbl = new Label()
            {
                Parent = this,
                Top = joinButton.Top,
                Font = GameService.Content.DefaultFont18,
                Text = "Current group"
            };
            var currentGroupLblWidth = (int)Math.Ceiling(currentGroupLbl.Font.MeasureString(currentGroupLbl.Text).Width);
            currentGroupLbl.Width = currentGroupLblWidth;
            currentGroupLbl.Right = joinButton.Right;

            descriptionLbl.Width = currentGroupLbl.Left - 10;
        }
        else
        {
            descriptionLbl.Width = joinButton.Left - 10;
        }
    }

    private async void JoinButton_Click(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        var button = sender as Button;
        button.Enabled = false;
        try
        {
            await (this.JoinClicked?.Invoke(this) ?? Task.CompletedTask);
        }
        catch (Exception) { }
        finally
        {
            button.Enabled = true;
        }
    }

    protected override void DisposeControl()
    {
        base.DisposeControl();
    }
}
