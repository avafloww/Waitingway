using Newtonsoft.Json;
using Waitingway.Backend.Database.Models;

namespace Waitingway.Backend.Database.Queue;

public class ClientQueue
{
    private uint _queuePosition;

    public uint QueuePosition
    {
        get => _queuePosition;
        set
        {
            _queuePosition = value;
            LastUpdateReceived = DateTime.UtcNow;
        }
    }

    public Guid ClientId { get; init; }
    public QueueSession DbSession { get; init; }
    public DateTime LastUpdateReceived { get; set; } = DateTime.UtcNow;
    public QueueSessionData.QueueEndReason? EndReason { get; set; }
    
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }

    public static ClientQueue? FromJson(string jsonString)
    {
        return JsonConvert.DeserializeObject<ClientQueue>(jsonString);
    }
}