namespace Waitingway.Protocol;

public class WaitingwayProtocol
{
   // Protocol version 1: 2022-01-16: FFXIV Global 6.05 / Dalamud API level 5 / Waitingway.Dalamud 1.0.0
   // - Initial version
   // Protocol version 2: 2022-04-14: FFXIV Global 6.1 / Dalamud API level 6 / Waitingway.Dalamud 1.2.3
   // - Add game version to ClientHello
   // - Remove ClientGoodbye, ClientLanguageChange
   // - QueueStatusEstimate -> QueueDisplayUpdate
   public const ushort Version = 2;
}
