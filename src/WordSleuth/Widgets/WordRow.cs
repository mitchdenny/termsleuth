using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class WordRow
{
    public const int Width = GameState.WordLength * LetterTile.Width + GameState.WordLength - 1;
    public const int Height = LetterTile.Height + 1;

    public static Hex1bWidget Build(WidgetContext<VStackWidget> context, string word,
        IReadOnlyList<LetterFeedback>? feedback = null, bool isActive = false) =>
        context.VStack(v =>
        [
            v.HStack(h => Enumerable.Range(0, GameState.WordLength)
                .Select(column => h.Padding(0, column < GameState.WordLength - 1 ? 1 : 0, 0, 0,
                    LetterTile.Build(h, column < word.Length ? word[column] : null, feedback?[column], isActive)))
                .ToArray()).FixedHeight(LetterTile.Height),
            ActiveCellIndicator.Build(v, isActive && word.Length < GameState.WordLength ? word.Length : null)
        ]).FixedHeight(Height);
}
