using Hex1b;
using Hex1b.Automation;
using Hex1b.Input;
using Hex1b.Theming;
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

    [Theory]
    [InlineData(60)]
    [InlineData(24)]
    public async Task FeedbackColorsTileBackgroundsAndUsedLettersWhileTextStaysWhite(int height)
    {
        var game = new GameState("APPLE");
        var screen = new GameScreen(game);
        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithHeadless(new TerminalCapabilities { SupportsTrueColor = true })
            .WithDimensions(80, height)
            .WithHex1bApp(screen.Build)
            .Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var running = terminal.RunAsync(timeout.Token);
        Hex1bColor?[] pending = [Hex1bColor.Black, Hex1bColor.Black, Hex1bColor.Black, Hex1bColor.Black, Hex1bColor.Black];
        Hex1bColor?[] scored =
        [
            Hex1bColor.FromRgb(83, 141, 78),
            Hex1bColor.FromRgb(181, 159, 59),
            Hex1bColor.FromRgb(58, 58, 60),
            Hex1bColor.FromRgb(181, 159, 59),
            Hex1bColor.FromRgb(58, 58, 60)
        ];

        try
        {
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => s.ContainsText("TermSleuth"), TimeSpan.FromSeconds(5))
                .Type("alley")
                .WaitUntil(s => FigletRowIsVisible(s, "ALLEY") && TileColorsMatch(s, 0, pending), TimeSpan.FromSeconds(5))
                .Enter()
                .WaitUntil(s => s.ContainsText("5 guesses left.") && FigletRowIsVisible(s, "ALLEY")
                    && TileColorsMatch(s, 0, scored) && TileColorsMatch(s, 1, pending)
                    && UsedLettersMatch(s, [scored[0], scored[3], scored[1], scored[4]]), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.R, Hex1bModifiers.Control)
                .WaitUntil(s => s.ContainsText("Find the five-letter") && s.ContainsText("Used letters: none yet")
                    && TileColorsMatch(s, 0, pending), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.C, Hex1bModifiers.Control)
                .Build()
                .ApplyAsync(terminal, timeout.Token);

            await running.WaitAsync(timeout.Token);
        }
        finally
        {
            await timeout.CancelAsync();
            await running;
        }
    }

    [Fact]
    public async Task AllUsedLettersFitBelowTheGridInAShortTerminal()
    {
        var game = new GameState("ZZZZZ");
        foreach (var word in new[] { "ABCDE", "FGHIJ", "KLMNO", "PQRST", "UVWXY", "ZABCD" })
        {
            foreach (var letter in word)
            {
                game.TypeLetter(letter);
            }
            game.Submit();
        }

        var screen = new GameScreen(game);
        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithHeadless().WithDimensions(80, 24).WithHex1bApp(screen.Build).Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var running = terminal.RunAsync(timeout.Token);
        try
        {
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => s.ContainsText(" A-  B-  C-  D-  E-  F-  G-  H-  I-  J-  K-  L-  M- ")
                    && s.ContainsText(" N-  O-  P-  Q-  R-  S-  T-  U-  V-  W-  X-  Y-  Z= ")
                    && s.GetCell(1, 18).Character == "A" && s.GetCell(49, 19).Character == "Z", TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.C, Hex1bModifiers.Control).Build().ApplyAsync(terminal, timeout.Token);
            await running.WaitAsync(timeout.Token);
        }
        finally
        {
            await timeout.CancelAsync();
            await running;
        }
    }

    private static bool UsedLettersMatch(Hex1bTerminalSnapshot snapshot, Hex1bColor?[] backgrounds)
    {
        const string expected = " A=  E~  L~  Y- ";
        var y = snapshot.Height - 5;
        if (!snapshot.ContainsText("Used letters"))
        {
            return false;
        }

        for (var x = 0; x < expected.Length; x++)
        {
            var cell = snapshot.GetCell(x, y);
            if (cell.Character != expected[x].ToString()
                || !Nullable.Equals<Hex1bColor>(cell.Foreground, Hex1bColor.White)
                || !Nullable.Equals(cell.Background, backgrounds[x / 4]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TileColorsMatch(Hex1bTerminalSnapshot snapshot, int row, Hex1bColor?[] backgrounds)
    {
        for (var column = 0; column < backgrounds.Length; column++)
        {
            for (var y = 2 + row * 7; y < 2 + (row + 1) * 7; y++)
            {
                for (var x = column * 13; x < (column + 1) * 13; x++)
                {
                    var cell = snapshot.GetCell(x, y);
                    if (!Nullable.Equals<Hex1bColor>(cell.Foreground, Hex1bColor.White)
                        || !Nullable.Equals(cell.Background, backgrounds[column]))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
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
