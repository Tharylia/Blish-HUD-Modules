namespace Estreya.BlishHUD.Shared.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class StateConfiguration
{
    public bool Enabled { get; set; }
    public bool AwaitLoading { get; set; } = true;
    public TimeSpan SaveInterval { get; set; } = Timeout.InfiniteTimeSpan;
}
