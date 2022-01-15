using Waitingway.Backend.Database.Models;

namespace Waitingway.Backend.Server.Client;

public class Client
{
    public string Id { get; init; }
    public bool InQueue => Queue != null;
    public ClientQueue? Queue { get; set; }
    public string PluginVersion { get; set; }

    public static Client From(RecentlyActiveQueueSession ra)
    {
        return new Client
        {
            Id = ra.ClientId.ToString(),
            PluginVersion = ra.PluginVersion,
            Queue = new ClientQueue
            {
                DbSession = new QueueSession
                {
                    Id = ra.Id,
                    ClientId = ra.ClientId,
                    ClientSessionId = ra.ClientSessionId,
                    DataCenter = ra.DataCenter,
                    SessionType = ra.SessionType,
                    World = ra.World,
                    DutyContentId = ra.DutyContentId,
                    PluginVersion = ra.PluginVersion
                },
                QueuePosition = ra.QueuePosition ?? 0,
                LastUpdateReceived = ra.Time
            }
        };
    }
}