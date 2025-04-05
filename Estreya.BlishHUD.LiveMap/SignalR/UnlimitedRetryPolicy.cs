namespace Estreya.BlishHUD.LiveMap.SignalR;

using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class UnlimitedRetryPolicy : IRetryPolicy
{
    private readonly TimeSpan _retryTime;

    public UnlimitedRetryPolicy(TimeSpan retryTime)
    {
        this._retryTime = retryTime;
    }

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        return this._retryTime;
    }
}
