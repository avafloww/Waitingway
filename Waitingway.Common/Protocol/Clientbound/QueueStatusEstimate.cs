namespace Waitingway.Common.Protocol.Clientbound;

public class QueueStatusEstimate : IPacket
{
    public TimeSpan EstimatedTime { get; init; }
    public GuiText[] LocalisedMessages { get; init; }

    public override string ToString()
    {
        return $"{nameof(EstimatedTime)}: {EstimatedTime}, {nameof(LocalisedMessages)}: {LocalisedMessages}";
    }
}
