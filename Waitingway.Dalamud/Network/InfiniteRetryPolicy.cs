using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace Waitingway.Dalamud.Network;

public class InfiniteRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        return TimeSpan.FromSeconds(5);
    }
}
