using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class AlphabetGrid
{
    public static Hex1bWidget Build(WidgetContext<VStackWidget> context,
        IReadOnlyDictionary<char, LetterFeedback> usedLetters) =>
        context.VStack(v =>
        [
            v.Text("Letters (blue: unused)"),
            .. "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Chunk(13).Select(letters => AlphabetRow.Build(v, letters, usedLetters))
        ]);
}
