namespace Waitingway.Protocol.Clientbound;

public class ServerGoodbye : IPacket
{
    public string? Message { get; init; }

    public override string ToString()
    {
        return $"{nameof(Message)}: {Message}";
    }
}