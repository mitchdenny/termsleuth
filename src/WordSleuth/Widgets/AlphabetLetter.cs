using Hex1b;
using Hex1b.Theming;
using Hex1b.Widgets;

namespace WordSleuth;

public static class AlphabetLetter
{
    public static Hex1bWidget Build(WidgetContext<HStackWidget> context, char letter, LetterFeedback? feedback)
    {
        var marker = feedback.HasValue ? TileStyle.Marker(feedback.Value) : ".";
        return TileStyle.Build(context, context.Text($" {letter}{marker} ").FixedWidth(4), feedback,
            Hex1bColor.FromRgb(45, 70, 95));
    }
}
