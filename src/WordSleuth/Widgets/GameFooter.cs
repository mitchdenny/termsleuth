using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class GameFooter
{
    public const int Height = BeveledButton.Height;

    public static Hex1bWidget Build(WidgetContext<VStackWidget> context, bool finished, bool won,
        IReadOnlyDictionary<char, LetterFeedback> usedLetters, Action restart) =>
        finished
            ? context.Padding(18, 18, 0, 0, BeveledButton.Build(context, "PLAY AGAIN ↵",
                won ? ButtonPalette.Green : ButtonPalette.MutedGreen, restart)).FixedHeight(Height)
            : context.VStack(v =>
            [
                AlphabetGrid.Build(v, usedLetters).FixedHeight(2),
                v.Text("").FixedHeight(1)
            ]).FixedHeight(Height);
}
