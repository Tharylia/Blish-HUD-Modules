namespace Estreya.BlishHUD.Shared.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class StateConfigurations
{
    public APIStateConfiguration Account { get; set; } = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = true,
        NeededPermissions = new List<Gw2Sharp.WebApi.V2.Models.TokenPermission>() {  Gw2Sharp.WebApi.V2.Models.TokenPermission.Account},
        UpdateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100))
    };

    public APIStateConfiguration Mapchests { get; set; }  = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false,
        NeededPermissions = new List<Gw2Sharp.WebApi.V2.Models.TokenPermission>() { Gw2Sharp.WebApi.V2.Models.TokenPermission.Account, Gw2Sharp.WebApi.V2.Models.TokenPermission.Progression },
        UpdateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100))
    };

    public APIStateConfiguration Worldbosses { get; set; } = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false,
        NeededPermissions = new List<Gw2Sharp.WebApi.V2.Models.TokenPermission>() { Gw2Sharp.WebApi.V2.Models.TokenPermission.Account, Gw2Sharp.WebApi.V2.Models.TokenPermission.Progression },
        UpdateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100))
    };

    public APIStateConfiguration PointOfInterests { get; set; } = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false
    };

    public APIStateConfiguration Skills { get; set; } = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false
    };

    public APIStateConfiguration TradingPost { get; set; } = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false,
        NeededPermissions = new List<Gw2Sharp.WebApi.V2.Models.TokenPermission>() { Gw2Sharp.WebApi.V2.Models.TokenPermission.Account, Gw2Sharp.WebApi.V2.Models.TokenPermission.Tradingpost },
        UpdateInterval = TimeSpan.FromMinutes(2)
    };

    public StateConfiguration ArcDPS { get; set; } = new StateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false
    };
}
