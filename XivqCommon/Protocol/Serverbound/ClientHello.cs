namespace XIVq.Common.Protocol.Serverbound;

public class ClientHello : IPacket
{
    public ushort ProtocolVersion { get; init; }
    public string PluginVersion { get; init; }
    public string ClientId { get; init; }
    public string Language { get; init; }
}