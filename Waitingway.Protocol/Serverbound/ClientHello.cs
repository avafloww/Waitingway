namespace Waitingway.Protocol.Serverbound;

public class ClientHello : IPacket
{
    public ushort ProtocolVersion { get; init; }
    public string PluginVersion { get; init; }
    public string GameVersion { get; init; }
    public string ClientId { get; init; }

    public override string ToString() =>
        $"{nameof(ProtocolVersion)}: {ProtocolVersion}, {nameof(PluginVersion)}: {PluginVersion}, {nameof(GameVersion)}: {GameVersion}, {nameof(ClientId)}: {ClientId}";
}
