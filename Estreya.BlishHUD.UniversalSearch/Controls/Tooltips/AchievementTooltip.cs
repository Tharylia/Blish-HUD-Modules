namespace Estreya.BlishHUD.UniversalSearch.Controls.Tooltips;
using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.UniversalSearch.Utils;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;

public class AchievementTooltip : Tooltip
{
    private const int MAX_WIDTH = 400;

    private readonly Achievement _achivement;

    public AchievementTooltip(Achievement achievement)
    {
        this._achivement = achievement;

        var traitTitle = new Label()
        {
            Text = this._achivement.Name,
            Font = Content.DefaultFont18,
            TextColor = ContentService.Colors.Chardonnay,
            AutoSizeHeight = true,
            AutoSizeWidth = true,
            Parent = this,
        };

        var traitDescription = new Label()
        {
            Text = Utils.StringUtil.SanitizeTraitDescription(this._achivement.Description),
            Font = Content.DefaultFont16,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Location = new Point(0, traitTitle.Bottom + 5),
            Parent = this,
        };

        LabelUtil.HandleMaxWidth(traitDescription, MAX_WIDTH);

        Control lastBit = traitDescription;
        if (this._achivement.Bits != null)
        {
            foreach (var bit in this._achivement.Bits)
            {
                lastBit = this.CreateBit(bit, lastBit);
            }
        }
    }

    private Control CreateBit(AchievementBit bit, Control lastBit)
    {
        var factDescription = new Label()
        {
            Text = this.GetTextForBit(bit),
            Font = Content.DefaultFont16,
            TextColor = new Microsoft.Xna.Framework.Color(161, 161, 161),
            Height = 32,
            VerticalAlignment = VerticalAlignment.Middle,
            AutoSizeWidth = true,
            Location = new Point(0, lastBit.Bottom + 5),
            Parent = this,
        };

        LabelUtil.HandleMaxWidth(
            factDescription,
            MAX_WIDTH,
            32,
            () =>
            {
                factDescription.AutoSizeHeight = true;
                factDescription.RecalculateLayout();
                //factImage.Location = new Point(0, factDescription.Location.Y + ((factDescription.Height / 2) - (factImage.Height / 2)));
            });

        return factDescription;
    }

    private string GetTextForBit(AchievementBit bit)
    {
        return bit switch
        {
            AchievementTextBit achievementTextBit => achievementTextBit.Text,
            AchievementSkinBit achievementSkinBit => achievementSkinBit.Id.ToString(),
            AchievementMinipetBit achievementMinipetBit => achievementMinipetBit.Id.ToString(),
            AchievementItemBit achievementItemBit => achievementItemBit.Id.ToString(),
            _ => null,
        };
    }
}
