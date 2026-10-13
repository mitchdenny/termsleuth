using Hex1b;
using Hex1b.Input;
using Hex1b.Widgets;

namespace WordSleuth;

public sealed class GameScreen(GameState game, TimeProvider? timeProvider = null)
{
    private const int ContentWidth = WordRow.Width + 1;
    private const int ContentHeight = WordBoard.Height + 5;
    private readonly GameBackground background = new(timeProvider);

    public Hex1bWidget Build(RootContext context) => context.ZStack(z =>
        [
            background.Build(z, ContentWidth, ContentHeight, game.Guesses.Count),
            z.Align(Alignment.Center, z.VStack(v =>
            [
                v.Align(Alignment.Center, WordBoard.Build(v, game.Guesses, game.CurrentGuess, game.IsFinished, game.Submit))
                    .FixedHeight(WordBoard.Height),
                v.Text("").FixedHeight(1),
                v.Padding(4, 4, 0, 0, AlphabetGrid.Build(v, game.UsedLetters)).FixedHeight(2),
                v.Padding(4, 4, 0, 0, GuessStatus.Build(v,
                    game.IsFinished || game.HasInputError
                        ? game.Message : ""))
                    .FixedHeight(2)
            ]).FixedWidth(ContentWidth).FixedHeight(ContentHeight)).Fill()
        ]).Fill().InputBindings(bindings =>
    {
        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            var key = Enum.Parse<Hex1bKey>(letter.ToString());
            bindings.Key(key).Global().Action(_ => game.TypeLetter(letter), $"Type {letter}");
            bindings.Shift().Key(key).Global().Action(_ => game.TypeLetter(letter), $"Type {letter}");
        }

        bindings.Key(Hex1bKey.Backspace).Global().Action(_ => game.Backspace(), "Erase letter");
        bindings.Key(Hex1bKey.Enter).Global().Action(_ => game.Submit(), "Submit guess");
        bindings.Ctrl().Key(Hex1bKey.R).Global().Action(_ => game.Restart(), "New game");
        bindings.Ctrl().Key(Hex1bKey.C).Global().Action(ctx => ctx.RequestStop(), "Quit");
    });
}
