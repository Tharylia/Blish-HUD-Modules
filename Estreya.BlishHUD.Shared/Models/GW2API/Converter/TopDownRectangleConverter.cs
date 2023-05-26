namespace Estreya.BlishHUD.Shared.Models.GW2API.Converter;

using Gw2Sharp.WebApi.V2.Models;

public class TopDownRectangleConverter : RectangleConverter
{
    public TopDownRectangleConverter() : base(RectangleDirectionType.TopDown) { }
}