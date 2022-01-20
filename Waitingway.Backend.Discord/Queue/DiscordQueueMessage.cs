using ProtoBuf;

namespace Waitingway.Backend.Discord.Queue;

[ProtoContract]
public class DiscordQueueMessage
{
    [ProtoMember(1)]
    public ulong PreviousMessageId { get; }

    public DiscordQueueMessage(ulong previousMessageId)
    {
        PreviousMessageId = previousMessageId;
    }

    public static implicit operator DiscordQueueMessage(ulong previousMessageId) => new(previousMessageId);

    public static implicit operator ulong(DiscordQueueMessage message) => message.PreviousMessageId;
}