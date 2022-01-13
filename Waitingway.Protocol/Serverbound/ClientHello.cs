namespace Waitingway.Protocol.Serverbound;

public class ClientHello : IPacket
{
    public ushort ProtocolVersion { get; init; }
    public string PluginVersion { get; init; }
    public string ClientId { get; init; }
    public string Language { get; init; }

    public override string ToString()
    {
        return $"{nameof(ProtocolVersion)}: {ProtocolVersion}, {nameof(PluginVersion)}: {PluginVersion}, {nameof(ClientId)}: {ClientId}, {nameof(Language)}: {Language}";
    }
}