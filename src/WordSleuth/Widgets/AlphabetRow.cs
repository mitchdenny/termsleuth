using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class AlphabetRow
{
    public static Hex1bWidget Build(WidgetContext<VStackWidget> context, IReadOnlyList<char> letters,
        IReadOnlyDictionary<char, LetterFeedback> usedLetters) =>
        context.HStack(h => letters.Select(letter =>
            AlphabetLetter.Build(h, letter, usedLetters.TryGetValue(letter, out var feedback) ? feedback : null))
            .ToArray()).FixedHeight(1);
}
