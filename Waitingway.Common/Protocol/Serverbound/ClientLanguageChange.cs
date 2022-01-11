namespace Waitingway.Common.Protocol.Serverbound;

public class ClientLanguageChange : IPacket
{
    public string Language { get; set; }

    public override string ToString()
    {
        return $"{nameof(Language)}: {Language}";
    }
}