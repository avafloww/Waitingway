namespace Waitingway.Common.Protocol.Serverbound;

public class QueueStatusUpdate : IPacket
{
    public uint QueuePosition { get; init; }
}