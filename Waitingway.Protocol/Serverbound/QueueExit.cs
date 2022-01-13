namespace Waitingway.Protocol.Serverbound;

public class QueueExit : IPacket
{
    public QueueExitReason Reason { get; init; }

    public override string ToString()
    {
        return $"{nameof(Reason)}: {Reason}";
    }

    public enum QueueExitReason
    {
        Unknown = 0,
        Success = 1,
        Error = 2,
        UserCancellation = 3
    }
}
