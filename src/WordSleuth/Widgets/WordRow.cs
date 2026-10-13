using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class WordRow
{
    public const int TilesWidth = GameState.WordLength * LetterTile.Width + GameState.WordLength - 1;
    private const int SideWidth = EnterButton.Width + 1;
    public const int Width = TilesWidth + SideWidth * 2;
    public const int Height = LetterTile.Height + 1;

    public static Hex1bWidget Build(WidgetContext<VStackWidget> context, string word, Action submit,
        IReadOnlyList<LetterFeedback>? feedback = null, bool isActive = false) =>
        context.VStack(v =>
        [
            v.HStack(h =>
            [
                h.Text("").FixedWidth(SideWidth),
                .. Enumerable.Range(0, GameState.WordLength)
                    .Select(column => h.Padding(0, column < GameState.WordLength - 1 ? 1 : 0, 0, 0,
                        LetterTile.Build(h, column < word.Length ? word[column] : null, feedback?[column], isActive))),
                h.Padding(1, 0, 0, 0, EnterButton.Build(h, isActive && word.Length == GameState.WordLength, submit))
                    .FixedWidth(SideWidth)
            ]).FixedHeight(LetterTile.Height),
            v.Padding(SideWidth, SideWidth, 0, 0,
                ActiveCellIndicator.Build(v, isActive && word.Length < GameState.WordLength ? word.Length : null))
        ]).FixedWidth(Width).FixedHeight(Height);
}
