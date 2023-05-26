namespace Estreya.BlishHUD.Shared.Models.GW2API.Converter;

using Gw2Sharp.WebApi.V2.Models;

public class BottomUpRectangleConverter : RectangleConverter
{
    public BottomUpRectangleConverter() : base(RectangleDirectionType.BottomUp) { }
}