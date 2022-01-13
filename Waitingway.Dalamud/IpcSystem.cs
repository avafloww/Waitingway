using System;
using Dalamud.Plugin.Ipc;

namespace Waitingway.Dalamud;

public class IpcSystem : IDisposable
{
    private Plugin Plugin { get; }

    private readonly ICallGateProvider<bool> _cgIsInQueue;
    private readonly ICallGateProvider<int> _cgGetQueueType;
    private readonly ICallGateProvider<int> _cgGetQueuePosition;
    private readonly ICallGateProvider<TimeSpan?> _cgGetQueueEstimate;

    public IpcSystem(Plugin plugin)
    {
        Plugin = plugin;

        _cgIsInQueue = plugin.PluginInterface.GetIpcProvider<bool>("Waitingway.IsInQueue");
        _cgGetQueueType = plugin.PluginInterface.GetIpcProvider<int>("Waitingway.GetQueueType");
        _cgGetQueuePosition = plugin.PluginInterface.GetIpcProvider<int>("Waitingway.GetQueuePosition");
        _cgGetQueueEstimate = plugin.PluginInterface.GetIpcProvider<TimeSpan?>("Waitingway.GetQueueEstimate");

        _cgIsInQueue.RegisterFunc(Ipc_IsInQueue);
        _cgGetQueueType.RegisterFunc(Ipc_GetQueueType);
        _cgGetQueuePosition.RegisterFunc(Ipc_GetQueuePosition);
        _cgGetQueueEstimate.RegisterFunc(Ipc_GetQueueEstimate);
    }

    private bool Ipc_IsInQueue() => Plugin.Client.InQueue;
    private int Ipc_GetQueueType() => (int) Plugin.Client.QueueType;
    private int Ipc_GetQueuePosition() => Plugin.Client.QueuePosition;
    private TimeSpan? Ipc_GetQueueEstimate() => Plugin.Client.QueueEstimate;

    public void Dispose()
    {
        _cgIsInQueue.UnregisterFunc();
        _cgGetQueueType.UnregisterFunc();
        _cgGetQueuePosition.UnregisterFunc();
        _cgGetQueueEstimate.UnregisterFunc();
    }
}