namespace XIVq.Common.Protocol.Clientbound;

public class ServerGoodbye : IPacket
{
    public string? Message { get; init; }
}