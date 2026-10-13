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
                var marker = feedback switch
                {
                    LetterFeedback.Correct => "=",
                    LetterFeedback.Present => "~",
                    LetterFeedback.Absent => "-",
                    _ => column < word.Length ? letter : "_"
                };
                var color = feedback switch
                {
                    LetterFeedback.Correct => Hex1bColor.Green,
                    LetterFeedback.Present => Hex1bColor.Yellow,
                    LetterFeedback.Absent => Hex1bColor.Gray,
                    _ => Hex1bColor.White
                };

                return h.ThemePanel(
                    theme => theme.Set(GlobalTheme.ForegroundColor, color),
                    h.Border(h.FigletText(letter).Font(Font)).Title(marker)
                        .FixedWidth(TileWidth));
            })
            .ToArray()).FixedHeight(RowHeight);
    }
}
