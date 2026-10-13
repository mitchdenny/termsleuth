using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class WordBoard
{
    public const int Height = GameState.MaxGuesses * WordRow.Height;

    public static Hex1bWidget Build(WidgetContext<VStackWidget> context, IReadOnlyList<Guess> guesses,
        string currentGuess, bool isFinished, Action submit) =>
        context.VStack(board =>
            Enumerable.Range(0, GameState.MaxGuesses).Select(row =>
            {
                var submitted = row < guesses.Count ? guesses[row] : null;
                var word = submitted?.Word ?? (row == guesses.Count ? currentGuess : "");
                return WordRow.Build(board, word, submit, submitted?.Feedback, !isFinished && row == guesses.Count);
            }).ToArray())
            .FixedWidth(WordRow.Width)
            .FixedHeight(Height);
}
