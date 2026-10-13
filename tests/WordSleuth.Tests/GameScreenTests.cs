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
    [InlineData(52, 22, "MWXYZ")]
    [InlineData(120, 40, "APPLE")]
    [InlineData(53, 23, "APPLE")]
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
                .WaitUntil(s => s.ContainsText("[_____]"), TimeSpan.FromSeconds(5))
                .Type(answer[..4].ToLowerInvariant() + "x").Backspace().Type(answer[4].ToString())
                .WaitUntil(s => s.ContainsText($"[{answer}]"), TimeSpan.FromSeconds(5))
                .WaitUntil(s => LetterRowIsVisible(s, answer), TimeSpan.FromSeconds(5))
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
                .WaitUntil(s => s.ContainsText("[_____]") && LettersMatch(s, []), TimeSpan.FromSeconds(5))
                .Type("alley")
                .WaitUntil(s => LetterRowIsVisible(s, "ALLEY") && TileColorsMatch(s, 0, pending)
                    && LettersMatch(s, []), TimeSpan.FromSeconds(5))
                .Enter()
                .WaitUntil(s => s.ContainsText("5 guesses left.") && LetterRowIsVisible(s, "ALLEY")
                    && TileColorsMatch(s, 0, scored) && TileColorsMatch(s, 1, pending)
                    && LettersMatch(s, new Dictionary<char, (string, Hex1bColor?)>
                    {
                        ['A'] = ("=", scored[0]),
                        ['E'] = ("~", scored[3]),
                        ['L'] = ("~", scored[1]),
                        ['Y'] = ("-", scored[4])
                    }), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.R, Hex1bModifiers.Control)
                .WaitUntil(s => s.ContainsText("Find the five-letter") && LettersMatch(s, [])
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
                    && s.GetCell(15, 19).Character == "A" && s.GetCell(63, 20).Character == "Z", TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.C, Hex1bModifiers.Control).Build().ApplyAsync(terminal, timeout.Token);
            await running.WaitAsync(timeout.Token);
        }
        finally
        {
            await timeout.CancelAsync();
            await running;
        }
    }

    private static bool LettersMatch(Hex1bTerminalSnapshot snapshot, Dictionary<char, (string Marker, Hex1bColor? Background)> usedLetters)
    {
        for (var index = 0; index < 26; index++)
        {
            var letter = (char)('A' + index);
            var (marker, background) = usedLetters.TryGetValue(letter, out var result)
                ? result
                : (".", Hex1bColor.FromRgb(45, 70, 95));
            var expected = $" {letter}{marker} ";
            var y = (snapshot.Height - 22) / 2 + 18 + index / 13;
            for (var offset = 0; offset < 4; offset++)
            {
                var cell = snapshot.GetCell((snapshot.Width - 52) / 2 + index % 13 * 4 + offset, y);
                if (cell.Character != expected[offset].ToString()
                    || !Nullable.Equals<Hex1bColor>(cell.Foreground, Hex1bColor.White)
                    || !Nullable.Equals(cell.Background, background))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool TileColorsMatch(Hex1bTerminalSnapshot snapshot, int row, Hex1bColor?[] backgrounds)
    {
        for (var column = 0; column < backgrounds.Length; column++)
        {
            var top = (snapshot.Height - 22) / 2 + row * 3;
            var left = (snapshot.Width - 52) / 2 + 13 + column * 5;
            for (var y = top; y < top + 3; y++)
            {
                for (var x = left; x < left + 5; x++)
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

    private static bool LetterRowIsVisible(Hex1bTerminalSnapshot snapshot, string word)
    {
        if (snapshot.ContainsText("TermSleuth") || snapshot.ContainsText("A-Z: type")
            || snapshot.ContainsText("PgUp/PgDn"))
        {
            return false;
        }

        for (var column = 0; column < word.Length; column++)
        {
            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 5; x++)
                {
                    var expected = x == 2 && y == 1 ? word[column].ToString() : " ";
                    var left = (snapshot.Width - 52) / 2 + 13 + column * 5;
                    var top = (snapshot.Height - 22) / 2;
                    if (snapshot.GetCell(left + x, top + y).Character != expected)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
