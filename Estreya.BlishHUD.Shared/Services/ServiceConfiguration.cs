namespace Estreya.BlishHUD.Shared.Services;

using System;
using System.Threading;

public class ServiceConfiguration
{
    public bool Enabled { get; set; }
    public bool AwaitLoading { get; set; } = true;
    public TimeSpan SaveInterval { get; set; } = Timeout.InfiniteTimeSpan;
}