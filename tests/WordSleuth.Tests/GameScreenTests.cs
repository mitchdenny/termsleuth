using Hex1b;
using Hex1b.Automation;
using Hex1b.Input;
using Hex1b.Widgets;
using WordSleuth;

namespace WordSleuth.Tests;

public class GameScreenTests
{
    [Theory]
    [InlineData(80, 60, "APPLE")]
    [InlineData(80, 24, "APPLE")]
    [InlineData(80, 60, "MWXYZ")]
    public async Task RealInputUpdatesTheRenderedBoardAndCanWinRestartAndQuit(int width, int height, string answer)
    {
        var game = new GameState(answer);
        var screen = new GameScreen(game);
        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(width, height)
            .WithHex1bApp(screen.Build)
            .Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var running = terminal.RunAsync(timeout.Token);

        try
        {
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => s.ContainsText("TermSleuth"), TimeSpan.FromSeconds(5))
                .Type(answer[..4].ToLowerInvariant() + "x").Backspace().Type(answer[4].ToString())
                .WaitUntil(s => s.ContainsText($"[{answer}]"), TimeSpan.FromSeconds(5))
                .WaitUntil(s => FigletRowIsVisible(s, answer), TimeSpan.FromSeconds(5))
                .Enter()
                .WaitUntil(s => s.ContainsText("Solved in 1"), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.R, Hex1bModifiers.Control)
                .WaitUntil(s => s.ContainsText("[_____]") && s.ContainsText("Find the five-letter"), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.C, Hex1bModifiers.Control)
                .Build()
                .ApplyAsync(terminal, timeout.Token);

            await running.WaitAsync(timeout.Token);
            Assert.Empty(game.Guesses);
            Assert.False(game.IsFinished);
        }
        finally
        {
            await timeout.CancelAsync();
            await running;
        }
    }

    [Fact]
    public void EveryLetterFitsInsideItsTile()
    {
        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            Assert.True(FigletFonts.Small.TryGetGlyph(letter, out var glyph));
            Assert.True(glyph.Width <= 11, $"{letter} is {glyph.Width} columns wide.");
        }
    }

    private static bool FigletRowIsVisible(Hex1bTerminalSnapshot snapshot, string word)
    {
        for (var column = 0; column < word.Length; column++)
        {
            if (!FigletFonts.Small.TryGetGlyph(word[column], out var glyph))
            {
                return false;
            }

            for (var y = 0; y < glyph.Height; y++)
            {
                var row = glyph.Rows[y].Replace(FigletFonts.Small.Hardblank, ' ');
                for (var x = 0; x < row.Length; x++)
                {
                    if (snapshot.GetCell(column * 13 + 1 + x, 3 + y).Character != row[x].ToString())
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
