using Hex1b;
using Hex1b.Input;
using Hex1b.Widgets;

namespace WordSleuth;

public static class WordBoard
{
    public static Hex1bWidget Build(WidgetContext<VStackWidget> context, IReadOnlyList<Guess> guesses,
        string currentGuess) =>
        context.VScrollPanel(panel => [panel.VStack(board =>
            Enumerable.Range(0, GameState.MaxGuesses).Select(row =>
            {
                var submitted = row < guesses.Count ? guesses[row] : null;
                var word = submitted?.Word ?? (row == guesses.Count ? currentGuess : "");
                return WordRow.Build(board, word, submitted?.Feedback);
            }).ToArray())])
            .InputBindings(bindings =>
            {
                bindings.Remove(ScrollPanelWidget.PageUpAction);
                bindings.Remove(ScrollPanelWidget.PageDownAction);
                bindings.Key(Hex1bKey.PageUp).Global().Triggers(ScrollPanelWidget.PageUpAction);
                bindings.Key(Hex1bKey.PageDown).Global().Triggers(ScrollPanelWidget.PageDownAction);
            })
            .FillHeight();
}
