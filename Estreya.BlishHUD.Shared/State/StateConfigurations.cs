namespace Estreya.BlishHUD.Shared.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class StateConfigurations
{
    public StateConfiguration BlishHUDAPI { get; } = new StateConfiguration()
    {
        Enabled = false,
        AwaitLoading = true
    };

    public APIStateConfiguration Account { get; } = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = true,
        NeededPermissions = new List<Gw2Sharp.WebApi.V2.Models.TokenPermission>() {  Gw2Sharp.WebApi.V2.Models.TokenPermission.Account},
        UpdateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100))
    };

    public APIStateConfiguration Mapchests { get; }  = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false,
        NeededPermissions = new List<Gw2Sharp.WebApi.V2.Models.TokenPermission>() { Gw2Sharp.WebApi.V2.Models.TokenPermission.Account, Gw2Sharp.WebApi.V2.Models.TokenPermission.Progression },
        UpdateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100))
    };

    public APIStateConfiguration Worldbosses { get; } = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false,
        NeededPermissions = new List<Gw2Sharp.WebApi.V2.Models.TokenPermission>() { Gw2Sharp.WebApi.V2.Models.TokenPermission.Account, Gw2Sharp.WebApi.V2.Models.TokenPermission.Progression },
        UpdateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100))
    };

    public APIStateConfiguration PointOfInterests { get; } = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false
    };

    public APIStateConfiguration Skills { get; } = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false
    };

    public APIStateConfiguration TradingPost { get; } = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false,
        NeededPermissions = new List<Gw2Sharp.WebApi.V2.Models.TokenPermission>() { Gw2Sharp.WebApi.V2.Models.TokenPermission.Account, Gw2Sharp.WebApi.V2.Models.TokenPermission.Tradingpost },
        UpdateInterval = TimeSpan.FromMinutes(2)
    };

    public APIStateConfiguration Items { get; } = new APIStateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false
    };

    public StateConfiguration ArcDPS { get; } = new StateConfiguration()
    {
        Enabled = false,
        AwaitLoading = false
    };
}
