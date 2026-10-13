using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class WordBoard
{
    public static Hex1bWidget Build(WidgetContext<VStackWidget> context, IReadOnlyList<Guess> guesses,
        string currentGuess) =>
        context.VStack(board =>
            Enumerable.Range(0, GameState.MaxGuesses).Select(row =>
            {
                var submitted = row < guesses.Count ? guesses[row] : null;
                var word = submitted?.Word ?? (row == guesses.Count ? currentGuess : "");
                return WordRow.Build(board, word, submitted?.Feedback);
            }).ToArray())
            .FixedWidth(GameState.WordLength * LetterTile.Width)
            .FixedHeight(GameState.MaxGuesses * LetterTile.Height);
}
