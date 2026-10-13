using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class WordRow
{
    public static Hex1bWidget Build(WidgetContext<VStackWidget> context, string word,
        IReadOnlyList<LetterFeedback>? feedback = null) =>
        context.HStack(h => Enumerable.Range(0, GameState.WordLength)
            .Select(column => LetterTile.Build(h, column < word.Length ? word[column] : null, feedback?[column]))
            .ToArray()).FixedHeight(LetterTile.Height);
}
