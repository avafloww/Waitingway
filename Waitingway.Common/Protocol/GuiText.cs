using System.Numerics;

namespace Waitingway.Common.Protocol;

public class GuiText
{
    public Vector4? Color { get; init; }
    public string Text { get; init; }

    public override string ToString()
    {
        return $"{nameof(Color)}: {Color}, {nameof(Text)}: {Text}";
    }
}