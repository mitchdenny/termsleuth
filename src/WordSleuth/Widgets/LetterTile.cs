using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class LetterTile
{
    private static readonly FigletFont Font = FigletFonts.Small;
    public static int Height => Font.Height + 2;
    private const int Width = 13;

    public static Hex1bWidget Build(WidgetContext<HStackWidget> context, char? letter, LetterFeedback? feedback)
    {
        var marker = feedback.HasValue ? TileStyle.Marker(feedback.Value) : letter?.ToString() ?? "_";
        var tile = context.Border(context.FigletText(letter?.ToString() ?? " ").Font(Font))
            .Title(marker).FixedWidth(Width);

        // The row sets the height so the border still measures its FIGlet child.
        return TileStyle.Build(context, tile, feedback);
    }
}
