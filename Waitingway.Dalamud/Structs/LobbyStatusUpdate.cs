using System.Runtime.InteropServices;

namespace Waitingway.Dalamud.Structs;

// length is at least 0x20 as of patch 6.0, but probably longer
[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public struct LobbyStatusUpdate
{
    [FieldOffset(0x18)] public LobbyStatusCode statusCode;
    [FieldOffset(0x1C)] public int queueLength;
}
