using Hex1b;
using Hex1b.Theming;
using Hex1b.Widgets;

namespace WordSleuth;

public static class AlphabetLetter
{
    public static Hex1bWidget Build(WidgetContext<HStackWidget> context, char letter, LetterFeedback? feedback)
    {
        var (foreground, background) = feedback switch
        {
            LetterFeedback.Correct => (Hex1bColor.FromRgb(145, 175, 140), Hex1bColor.FromRgb(32, 54, 30)),
            LetterFeedback.Present => (Hex1bColor.FromRgb(175, 160, 105), Hex1bColor.FromRgb(58, 50, 23)),
            LetterFeedback.Absent => (Hex1bColor.FromRgb(48, 48, 54), Hex1bColor.FromRgb(10, 10, 12)),
            _ => (Hex1bColor.FromRgb(135, 145, 160), Hex1bColor.FromRgb(22, 32, 44))
        };
        return TileStyle.Build(context, context.Text($" {letter}  ").FixedWidth(4), foreground, background);
    }
}
