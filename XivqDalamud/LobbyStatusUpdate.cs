using System.Runtime.InteropServices;

namespace XIVq.Dalamud;

// length is at least 0x20 as of patch 6.0, but probably longer
[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public unsafe struct LobbyStatusUpdate
{
    [FieldOffset(0x18)] public uint statusCode;
    [FieldOffset(0x1C)] public uint queueLength;
}
