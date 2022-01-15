using Waitingway.Server.Client;

namespace Waitingway.Server.Models;

public class RecentlyActiveQueueSession
{
    /** QueueSession members */
    public int Id { get; set; }

    public Guid ClientId { get; set; }
    public string ClientSessionId { get; set; }
    public int DataCenter { get; set; }
    public QueueSession.Type SessionType { get; set; }
    public int? World { get; set; }
    public int? DutyContentId { get; set; }
    public string PluginVersion { get; set; }

    /** QueueSessionData members */
    public DateTime Time { get; set; }

    public QueueSessionData.DataType Type { get; set; }
    public uint? QueuePosition { get; set; }
    public TimeSpan? GameTimeEstimate { get; set; }
    public TimeSpan? OurTimeEstimate { get; set; }

    public Client.Client ToClient()
    {
        return new Client.Client
        {
            Id = ClientId.ToString(),
            PluginVersion = PluginVersion,
            Queue = new ClientQueue
            {
                DbSession = new QueueSession
                {
                    Id = Id,
                    ClientId = ClientId,
                    ClientSessionId = ClientSessionId,
                    DataCenter = DataCenter,
                    SessionType = SessionType,
                    World = World,
                    DutyContentId = DutyContentId,
                    PluginVersion = PluginVersion
                },
                QueuePosition = QueuePosition ?? 0,
                LastUpdateReceived = Time
            }
        };
    }
}