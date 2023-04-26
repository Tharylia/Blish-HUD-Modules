namespace Estreya.BlishHUD.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class ServiceConfiguration
{
    public bool Enabled { get; set; }
    public bool AwaitLoading { get; set; } = true;
    public TimeSpan SaveInterval { get; set; } = Timeout.InfiniteTimeSpan;
}
