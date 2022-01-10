using System.Security.Cryptography;
using System.Text;

namespace Waitingway.Common.Protocol.Serverbound;

public class LoginQueueEnter : IPacket
{
    public string SessionId { get; init; }
    public ushort DatacenterId { get; init; }
    public ushort WorldId { get; init; }

    public LoginQueueEnter()
    {
    }

    public LoginQueueEnter(string clientId, ulong characterId, ushort datacenterId, ushort worldId)
    {
        // Generate a deterministic session ID based on the client and character ID
        // This is only for tracking crashes/disconnects in queue as one session; we don't care about the actual character
        using (var sha = SHA256.Create())
        {
            var data = $"Waitingway|Client={clientId}|Character={characterId:X}";
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            SessionId = builder.ToString();
        }

        DatacenterId = datacenterId;
        WorldId = worldId;
    }

    public override string ToString()
    {
        return $"{nameof(SessionId)}: {SessionId}, {nameof(DatacenterId)}: {DatacenterId}, {nameof(WorldId)}: {WorldId}";
    }
}