namespace Estreya.BlishHUD.Shared.Services;

using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;

public class ServiceConfigurations
{
    public ServiceConfiguration BlishHUDAPI { get; } = new ServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = true
    };

    public APIServiceConfiguration Account { get; } = new APIServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = true,
        NeededPermissions = new List<TokenPermission> { TokenPermission.Account },
        UpdateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100))
    };

    public APIServiceConfiguration Mapchests { get; } = new APIServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = false,
        NeededPermissions = new List<TokenPermission>
        {
            TokenPermission.Account,
            TokenPermission.Progression
        },
        UpdateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100))
    };

    public APIServiceConfiguration Worldbosses { get; } = new APIServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = false,
        NeededPermissions = new List<TokenPermission>
        {
            TokenPermission.Account,
            TokenPermission.Progression
        },
        UpdateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100))
    };

    public APIServiceConfiguration PointOfInterests { get; } = new APIServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = false
    };

    public APIServiceConfiguration Skills { get; } = new APIServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = false
    };

    public APIServiceConfiguration PlayerTransactions { get; } = new APIServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = false,
        NeededPermissions = new List<TokenPermission>
        {
            TokenPermission.Account,
            TokenPermission.Tradingpost
        },
        UpdateInterval = TimeSpan.FromMinutes(2)
    };

    public APIServiceConfiguration Transactions { get; } = new APIServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = false,
        UpdateInterval = TimeSpan.FromMinutes(2)
    };

    public APIServiceConfiguration Items { get; } = new APIServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = false
    };

    public ServiceConfiguration ArcDPS { get; } = new ServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = false
    };

    public APIServiceConfiguration Achievements { get; } = new APIServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = false
    };

    public APIServiceConfiguration AccountAchievements { get; } = new APIServiceConfiguration
    {
        Enabled = false,
        AwaitLoading = false,
        NeededPermissions = new List<TokenPermission>
        {
            TokenPermission.Account,
            TokenPermission.Progression
        },
        UpdateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100))
    };
}