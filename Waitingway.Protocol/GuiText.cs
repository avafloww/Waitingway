using System.Numerics;

namespace Waitingway.Protocol;

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
        return Color switch
        {
            GuiTextColor.Green => new(0.117f, 1f, 0f, 1f),
            GuiTextColor.Yellow => new(1f, 1f, .4f, 1f),
            GuiTextColor.Red => new(1f, 0f, 0f, 1f),
            GuiTextColor.Grey => new(0.7f, 0.7f, 0.7f, 1f),
            _ => new(1f, 1f, 1f, 1f),
        };
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