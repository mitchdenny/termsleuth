using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class GuessStatus
{
    public static Hex1bWidget Build(WidgetContext<VStackWidget> context, int guessCount, string currentGuess,
        string message) => context.VStack(v =>
    [
        v.Text($"Guess {Math.Min(guessCount + 1, GameState.MaxGuesses)}/{GameState.MaxGuesses}: [{currentGuess.PadRight(GameState.WordLength, '_')}]"),
        v.Text(message)
    ]);
}
