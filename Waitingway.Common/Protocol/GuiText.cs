using System.Numerics;

namespace Waitingway.Common.Protocol;

public class GuiText
{
    public GuiTextColor Color { get; init; } = GuiTextColor.White;
    public string Text { get; init; }

    public override string ToString()
    {
        return $"{nameof(Color)}: {Color}, {nameof(Text)}: {Text}";
    }

    public Vector4 ColorAsVec()
    {
        switch (Color)
        {
            case GuiTextColor.Green:
                return new(0.117f, 1f, 0f, 1f);
            case GuiTextColor.Yellow:
                return new(1f, 1f, .4f, 1f);
            case GuiTextColor.Red:
                return new(1f, 0f, 0f, 1f);
            case GuiTextColor.Grey:
                return new(0.7f, 0.7f, 0.7f, 1f);
            case GuiTextColor.White:
            default:
                return new(1f, 1f, 1f, 1f);
        }
    }

    public enum GuiTextColor
    {
        White,
        Green,
        Yellow,
        Red,
        Grey
    }
}