using Waitingway.Server.Models;

namespace Waitingway.Server;

public class ClientQueue
{
    private uint _queuePosition;

    public uint QueuePosition
    {
        get => _queuePosition;
        set
        {
            _queuePosition = value;
            LastUpdateReceived = DateTime.Now;
        }
    }

    public QueueSession DbSession { get; init; }
    public DateTime EntryTime { get; } = DateTime.Now;
    public DateTime LastUpdateReceived { get; set; } = DateTime.Now;
}