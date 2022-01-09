namespace XIVq.Server;

public class Client
{
    public string Id { get; init; }
    public bool InQueue => Queue != null;
    public ClientQueue? Queue { get; set; }
}