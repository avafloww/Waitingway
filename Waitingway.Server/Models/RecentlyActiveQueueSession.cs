namespace Waitingway.Server.Models;

public class RecentlyActiveQueueSession
{
    /** QueueSession members */
    public int Id { get; }
    public string ClientId { get; }
    public string ClientSessionId { get; }
    public int DataCenter { get; }
    public QueueSession.Type SessionType { get; }
    public int? World { get; set; }
    public int? DutyContentId { get; set; }
    public string PluginVersion { get; set; }
    
    /** QueueSessionData members */
    public DateTime Time { get; }
    public QueueSessionData.DataType Type { get; }
    public uint? QueuePosition { get; }
    public TimeSpan? GameTimeEstimate { get; }
    public TimeSpan? OurTimeEstimate { get; }
}