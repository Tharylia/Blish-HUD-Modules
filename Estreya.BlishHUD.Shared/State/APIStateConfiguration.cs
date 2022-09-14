namespace Estreya.BlishHUD.Shared.State;

using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class APIStateConfiguration : StateConfiguration
{
    public List<TokenPermission> NeededPermissions { get; init; } = new List<TokenPermission>();

    public TimeSpan UpdateInterval { get; set; } = Timeout.InfiniteTimeSpan;
}
