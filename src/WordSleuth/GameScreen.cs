using Hex1b;
using Hex1b.Input;
using Hex1b.Theming;
using Hex1b.Widgets;

namespace WordSleuth;

public sealed class GameScreen(GameState game)
{
    private static readonly FigletFont Font = FigletFonts.Small;
    private const int TileWidth = 13;
    private static int RowHeight => Font.Height + 2;

    public Hex1bWidget Build(RootContext context) => context.VStack(v =>
    [
        v.Text("TermSleuth"),
        v.Text("= correct (green) | ~ misplaced (yellow) | - absent (gray)"),
        v.VScrollPanel(panel => [panel.VStack(board =>
            Enumerable.Range(0, GameState.MaxGuesses)
                .Select(row => BuildRow(board, row))
                .ToArray())])
            .InputBindings(bindings =>
            {
                bindings.Remove(ScrollPanelWidget.PageUpAction);
                bindings.Remove(ScrollPanelWidget.PageDownAction);
                bindings.Key(Hex1bKey.PageUp).Global().Triggers(ScrollPanelWidget.PageUpAction);
                bindings.Key(Hex1bKey.PageDown).Global().Triggers(ScrollPanelWidget.PageDownAction);
            })
            .FillHeight(),
        BuildLetters(v),
        v.Text($"Guess {Math.Min(game.Guesses.Count + 1, GameState.MaxGuesses)}/{GameState.MaxGuesses}: [{game.CurrentGuess.PadRight(GameState.WordLength, '_')}]"),
        v.Text(game.Message),
        v.Text("A-Z: type | Backspace: erase | Enter: submit"),
        v.Text("Ctrl+R: new game | Ctrl+C: quit | PgUp/PgDn: scroll")
    ]).InputBindings(bindings =>
    {
        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            var key = Enum.Parse<Hex1bKey>(letter.ToString());
            bindings.Key(key).Global().Action(_ => game.TypeLetter(letter), $"Type {letter}");
            bindings.Shift().Key(key).Global().Action(_ => game.TypeLetter(letter), $"Type {letter}");
        }

        bindings.Key(Hex1bKey.Backspace).Global().Action(_ => game.Backspace(), "Erase letter");
        bindings.Key(Hex1bKey.Enter).Global().Action(_ => game.Submit(), "Submit guess");
        bindings.Ctrl().Key(Hex1bKey.R).Global().Action(_ => game.Restart(), "New game");
        bindings.Ctrl().Key(Hex1bKey.C).Global().Action(ctx => ctx.RequestStop(), "Quit");
    });

    private Hex1bWidget BuildRow(WidgetContext<VStackWidget> context, int row)
    {
        var submitted = row < game.Guesses.Count ? game.Guesses[row] : null;
        var word = submitted?.Word ?? (row == game.Guesses.Count ? game.CurrentGuess : "");

        return context.HStack(h => Enumerable.Range(0, GameState.WordLength)
            .Select(column =>
            {
                var letter = column < word.Length ? word[column].ToString() : " ";
                var feedback = submitted?.Feedback[column];
                var marker = feedback.HasValue
                    ? FeedbackMarker(feedback.Value)
                    : column < word.Length ? letter : "_";
                return ColorTile(h,
                    h.Border(h.FigletText(letter).Font(Font)).Title(marker)
                        .FixedWidth(TileWidth),
                    feedback);
            })
            .ToArray()).FixedHeight(RowHeight);
    }

    private Hex1bWidget BuildLetters(WidgetContext<VStackWidget> context)
    {
        return context.VStack(v =>
        [
            v.Text("Letters (blue: unused)"),
            .. "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Chunk(13).Select(chunk => v.HStack(h => chunk
                .Select(letter =>
                {
                    LetterFeedback? feedback = game.UsedLetters.TryGetValue(letter, out var result) ? result : null;
                    var marker = feedback.HasValue ? FeedbackMarker(feedback.Value) : ".";
                    return ColorTile(h, h.Text($" {letter}{marker} ").FixedWidth(4), feedback,
                        Hex1bColor.FromRgb(45, 70, 95));
                })
                .ToArray()).FixedHeight(1))
        ]);
    }

    private static string FeedbackMarker(LetterFeedback feedback) => feedback switch
    {
        LetterFeedback.Correct => "=",
        LetterFeedback.Present => "~",
        _ => "-"
    };

    private static Hex1bWidget ColorTile<T>(WidgetContext<T> context, Hex1bWidget child, LetterFeedback? feedback,
        Hex1bColor? pendingBackground = null)
        where T : Hex1bWidget
    {
        var background = feedback switch
        {
            LetterFeedback.Correct => Hex1bColor.FromRgb(83, 141, 78),
            LetterFeedback.Present => Hex1bColor.FromRgb(181, 159, 59),
            LetterFeedback.Absent => Hex1bColor.FromRgb(58, 58, 60),
            _ => pendingBackground ?? Hex1bColor.Default
        };

        return context.EffectPanel(child, surface =>
        {
            for (var y = 0; y < surface.Height; y++)
            {
                for (var x = 0; x < surface.Width; x++)
                {
                    surface[x, y] = surface[x, y] with
                    {
                        Foreground = Hex1bColor.White,
                        Background = background
                    };
                }
            }
        });
    }
}
