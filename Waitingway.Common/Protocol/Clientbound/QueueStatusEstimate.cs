namespace Waitingway.Common.Protocol.Clientbound;

public class QueueStatusEstimate : IPacket
{
    public TimeSpan EstimatedTime { get; init; }
    public GuiText[] LocalisedMessages { get; init; }
}
