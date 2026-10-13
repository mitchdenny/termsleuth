using Hex1b;
using Hex1b.Theming;
using Hex1b.Widgets;

namespace WordSleuth;

internal static class TileStyle
{
    public static string Marker(LetterFeedback feedback) => feedback switch
    {
        LetterFeedback.Correct => "=",
        LetterFeedback.Present => "~",
        _ => "-"
    };

    public static Hex1bWidget Build<T>(WidgetContext<T> context, Hex1bWidget child, LetterFeedback? feedback,
        Hex1bColor? pendingBackground = null) where T : Hex1bWidget
    {
        var background = feedback switch
        {
            LetterFeedback.Correct => Hex1bColor.FromRgb(83, 141, 78),
            LetterFeedback.Present => Hex1bColor.FromRgb(181, 159, 59),
            LetterFeedback.Absent => Hex1bColor.FromRgb(58, 58, 60),
            _ => pendingBackground ?? Hex1bColor.Black
        };

        return context.EffectPanel(child, surface =>
        {
            for (var y = 0; y < surface.Height; y++)
            {
                for (var x = 0; x < surface.Width; x++)
                {
                    surface[x, y] = surface[x, y] with
                    {
                        Character = string.IsNullOrEmpty(surface[x, y].Character) ? " " : surface[x, y].Character,
                        Foreground = Hex1bColor.White,
                        Background = background
                    };
                }
            }
        });
    }
}
