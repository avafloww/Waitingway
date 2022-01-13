namespace Waitingway.Server.Client;

public class Client
{
    public string Id { get; init; }
    public bool InQueue => Queue != null;
    public ClientQueue? Queue { get; set; }
    public string PluginVersion { get; set; }
    public bool Active { get; set; }
}