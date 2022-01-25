using Newtonsoft.Json;

namespace Waitingway.Backend.Database.Queue;

public class QueueEstimate
{
    public Guid ClientId { get; set; }
    public bool HasEstimate { get; set; }
    public TimeSpan? Estimate { get; set; }
    
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }

    public static QueueEstimate? FromJson(string jsonString)
    {
        return JsonConvert.DeserializeObject<QueueEstimate>(jsonString);
    }
}
