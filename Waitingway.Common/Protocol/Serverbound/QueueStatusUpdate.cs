namespace Waitingway.Common.Protocol.Serverbound;

public class QueueStatusUpdate : IPacket
{
    public uint QueuePosition { get; init; }

    public override string ToString()
    {
        return $"{nameof(QueuePosition)}: {QueuePosition}";
    }
}