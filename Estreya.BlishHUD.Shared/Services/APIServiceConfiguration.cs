namespace Estreya.BlishHUD.Shared.Services;

using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class APIServiceConfiguration : ServiceConfiguration
{
    public List<TokenPermission> NeededPermissions { get; set; } = new List<TokenPermission>();

    public TimeSpan UpdateInterval { get; set; } = Timeout.InfiniteTimeSpan;
}
