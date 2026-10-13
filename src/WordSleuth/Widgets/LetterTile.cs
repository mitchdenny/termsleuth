using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class LetterTile
{
    public const int Height = 3;
    public const int Width = 5;

    public static Hex1bWidget Build(WidgetContext<HStackWidget> context, char? letter, LetterFeedback? feedback)
    {
        var text = letter.HasValue ? char.ToUpperInvariant(letter.Value).ToString() : " ";
        var tile = context.Padding(2, 2, 1, 1, context.Text(text))
            .FixedWidth(Width).FixedHeight(Height);
        return TileStyle.Build(context, tile, feedback);
    }
}
