namespace Estreya.BlishHUD.Shared.Models.GW2API.Converter;

using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TopDownRectangleConverter : RectangleConverter
{
    public TopDownRectangleConverter() : base(RectangleDirectionType.TopDown) { }
}
