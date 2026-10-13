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
    [InlineData(80, 30, "APPLE")]
    [InlineData(80, 60, "MWXYZ")]
    [InlineData(62, 30, "MWXYZ")]
    [InlineData(120, 40, "APPLE")]
    [InlineData(63, 31, "APPLE")]
    public async Task RealInputUpdatesTheRenderedBoardAndCanWinRestartAndQuit(int width, int height, string answer)
    {
        var game = new GameState(answer);
        var screen = new GameScreen(game);
        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithHeadless(new TerminalCapabilities { SupportsTrueColor = true })
            .WithDimensions(width, height)
            .WithHex1bApp(screen.Build)
            .Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var running = terminal.RunAsync(timeout.Token);

        try
        {
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => s.ContainsText("6 remaining"), TimeSpan.FromSeconds(5))
                .Type(answer[..4].ToLowerInvariant() + "x").Backspace().Type(answer[4].ToString())
                .WaitUntil(s => LetterRowIsVisible(s, answer), TimeSpan.FromSeconds(5))
                .Enter()
                .WaitUntil(s => s.ContainsText("Solved in 1") && RemainingPromptsMatch(s, null), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.R, Hex1bModifiers.Control)
                .WaitUntil(s => s.ContainsText("6 remaining"), TimeSpan.FromSeconds(5))
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
    [InlineData(30)]
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
        var activeColor = Hex1bColor.FromRgb(24, 24, 28);
        Hex1bColor?[] pending = [activeColor, activeColor, activeColor, activeColor, activeColor];
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
                .WaitUntil(s => s.ContainsText("6 remaining") && LettersMatch(s, []), TimeSpan.FromSeconds(5))
                .Type("alley")
                .WaitUntil(s => LetterRowIsVisible(s, "ALLEY") && TileColorsMatch(s, 0, pending)
                    && LettersMatch(s, []), TimeSpan.FromSeconds(5))
                .Enter()
                .WaitUntil(s => s.ContainsText("5 remaining") && LetterRowIsVisible(s, "ALLEY") && BoardGapsMatch(s, 1, 0)
                    && TileColorsMatch(s, 0, scored) && TileColorsMatch(s, 1, pending)
                    && LettersMatch(s, new Dictionary<char, LetterFeedback>
                    {
                        ['A'] = LetterFeedback.Correct,
                        ['E'] = LetterFeedback.Present,
                        ['L'] = LetterFeedback.Present,
                        ['Y'] = LetterFeedback.Absent
                    }), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.R, Hex1bModifiers.Control)
                .WaitUntil(s => s.ContainsText("6 remaining") && LettersMatch(s, [])
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
    public async Task TwentyFiveUsedLettersFitBelowTheGridBeforeTheFinalGuess()
    {
        var game = new GameState("ZZZZZ");
        foreach (var word in new[] { "ABCDE", "FGHIJ", "KLMNO", "PQRST", "UVWXY" })
        {
            foreach (var letter in word)
            {
                game.TypeLetter(letter);
            }
            game.Submit();
        }

        var screen = new GameScreen(game);
        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithHeadless(new TerminalCapabilities { SupportsTrueColor = true })
            .WithDimensions(80, 30).WithHex1bApp(screen.Build).Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var running = terminal.RunAsync(timeout.Token);
        try
        {
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => LettersMatch(s, "ABCDEFGHIJKLMNOPQRSTUVWXY".ToDictionary(
                    letter => letter, _ => LetterFeedback.Absent))
                    && s.GetCell(15, 25).Character == "A" && s.GetCell(63, 26).Character == "Z", TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.C, Hex1bModifiers.Control).Build().ApplyAsync(terminal, timeout.Token);
            await running.WaitAsync(timeout.Token);
        }
        finally
        {
            await timeout.CancelAsync();
            await running;
        }
    }

    [Theory]
    [InlineData(80, 60)]
    [InlineData(80, 30)]
    [InlineData(62, 30)]
    public async Task ActiveTilesAndHalfBlockCursorFollowTypingSubmissionAndRestart(int width, int height)
    {
        var game = new GameState("ZZZZZ");
        var screen = new GameScreen(game);
        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithHeadless(new TerminalCapabilities { SupportsTrueColor = true })
            .WithDimensions(width, height).WithHex1bApp(screen.Build).Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var running = terminal.RunAsync(timeout.Token);
        var black = Enumerable.Repeat<Hex1bColor?>(Hex1bColor.Black, 5).ToArray();
        var green = Enumerable.Repeat<Hex1bColor?>(Hex1bColor.FromRgb(83, 141, 78), 5).ToArray();

        try
        {
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => ActiveRowMatches(s, 0, 0) && TileColorsMatch(s, 1, black), TimeSpan.FromSeconds(5))
                .Type("ab")
                .WaitUntil(s => LetterRowIsVisible(s, "AB") && ActiveRowMatches(s, 0, 2), TimeSpan.FromSeconds(5))
                .Backspace()
                .WaitUntil(s => LetterRowIsVisible(s, "A") && ActiveRowMatches(s, 0, 1), TimeSpan.FromSeconds(5))
                .Type("bcde")
                .WaitUntil(s => LetterRowIsVisible(s, "ABCDE") && ActiveRowMatches(s, 0, null), TimeSpan.FromSeconds(5))
                .Enter()
                .WaitUntil(s => ActiveRowMatches(s, 1, 0), TimeSpan.FromSeconds(5))
                .Type("fghij").Enter().Type("klmno").Enter().Type("pqrst").Enter().Type("uvwxy").Enter()
                .WaitUntil(s => ActiveRowMatches(s, 5, 0), TimeSpan.FromSeconds(5))
                .Type("zzzzz")
                .WaitUntil(s => LetterRowIsVisible(s, "ZZZZZ", 5) && ActiveRowMatches(s, 5, null)
                    && EnterButtonsMatch(s, 5), TimeSpan.FromSeconds(5))
                .Enter()
                .WaitUntil(s => s.ContainsText("Solved in 6") && TileColorsMatch(s, 5, green)
                    && RemainingPromptsMatch(s, null), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.R, Hex1bModifiers.Control)
                .WaitUntil(s => s.ContainsText("6 remaining") && ActiveRowMatches(s, 0, 0)
                    && TileColorsMatch(s, 1, black), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.C, Hex1bModifiers.Control)
                .Build().ApplyAsync(terminal, timeout.Token);
            await running.WaitAsync(timeout.Token);
        }
        finally
        {
            await timeout.CancelAsync();
            await running;
        }
    }

    private static bool ActiveRowMatches(Hex1bTerminalSnapshot snapshot, int row, int? column) =>
        TileColorsMatch(snapshot, row, Enumerable.Repeat<Hex1bColor?>(Hex1bColor.FromRgb(24, 24, 28), 5).ToArray())
        && BoardGapsMatch(snapshot, row, column);

    [Theory]
    [InlineData(80, 60, true)]
    [InlineData(62, 30, true)]
    [InlineData(63, 31, false)]
    [InlineData(120, 40, false)]
    public async Task BeveledEnterButtonAppearsWithoutMovingTheGridAndSubmits(int width, int height, bool click)
    {
        var game = new GameState("APPLE");
        var screen = new GameScreen(game);
        await using var terminal = Hex1bTerminal.CreateBuilder().WithMouse()
            .WithHeadless(new TerminalCapabilities { SupportsTrueColor = true, SupportsMouse = true })
            .WithDimensions(width, height).WithHex1bApp(screen.Build).Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var running = terminal.RunAsync(timeout.Token);
        var buttonX = (width - 52) / 2 + 41;
        var buttonY = (height - 30) / 2 + 1;

        try
        {
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => ActiveRowMatches(s, 0, 0) && EnterButtonsMatch(s, null)
                    && LettersMatch(s, []), TimeSpan.FromSeconds(5))
                .ClickAt(buttonX + 4, buttonY, MouseButton.Left)
                .Type("alle")
                .WaitUntil(s => ActiveRowMatches(s, 0, 4) && LetterRowIsVisible(s, "ALLE")
                    && EnterButtonsMatch(s, null), TimeSpan.FromSeconds(5))
                .Type("y")
                .WaitUntil(s => LetterRowIsVisible(s, "ALLEY") && EnterButtonsMatch(s, 0), TimeSpan.FromSeconds(5))
                .Backspace()
                .WaitUntil(s => LetterRowIsVisible(s, "ALLE") && ActiveRowMatches(s, 0, 4)
                    && EnterButtonsMatch(s, null), TimeSpan.FromSeconds(5))
                .ClickAt(buttonX + 4, buttonY, MouseButton.Left)
                .WaitUntil(s => s.ContainsText("6 remaining")
                    && EnterButtonsMatch(s, null), TimeSpan.FromSeconds(5))
                .Type("y")
                .WaitUntil(s => LetterRowIsVisible(s, "ALLEY") && EnterButtonsMatch(s, 0), TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            var submit = new Hex1bTerminalInputSequenceBuilder();
            if (click)
            {
                submit.ClickAt(buttonX + 4, buttonY, MouseButton.Left);
            }
            else
            {
                submit.Enter();
            }
            await submit
                .WaitUntil(s => s.ContainsText("5 remaining") && LetterRowIsVisible(s, "ALLEY")
                    && ActiveRowMatches(s, 1, 0) && EnterButtonsMatch(s, null), TimeSpan.FromSeconds(5))
                .Type("apple")
                .WaitUntil(s => LetterRowIsVisible(s, "APPLE", 1) && EnterButtonsMatch(s, 1), TimeSpan.FromSeconds(5))
                .Enter()
                .WaitUntil(s => s.ContainsText("Solved in 2") && EnterButtonsMatch(s, null), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.R, Hex1bModifiers.Control)
                .WaitUntil(s => s.ContainsText("6 remaining") && ActiveRowMatches(s, 0, 0)
                    && EnterButtonsMatch(s, null), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.C, Hex1bModifiers.Control)
                .Build().ApplyAsync(terminal, timeout.Token);
            await running.WaitAsync(timeout.Token);
        }
        finally
        {
            await timeout.CancelAsync();
            await running;
        }
    }

    private static bool EnterButtonsMatch(Hex1bTerminalSnapshot snapshot, int? visibleRow)
    {
        if (snapshot.ContainsText("PLAY AGAIN"))
        {
            return !snapshot.ContainsText("SUBMIT");
        }
        var face = Hex1bColor.FromRgb(204, 185, 150);
        var highlight = Hex1bColor.FromRgb(242, 224, 189);
        var shadow = Hex1bColor.FromRgb(130, 111, 80);
        var ink = Hex1bColor.FromRgb(56, 46, 32);
        string[] characters = ["▀▀▀▀▀▀▀▀▀▀▀▀", "▀▀SUBMIT ↵▀▀", "▀▀▀▀▀▀▀▀▀▀▀▀"];
        var left = (snapshot.Width - 52) / 2 + 41;
        var top = (snapshot.Height - 30) / 2;
        for (var row = 0; row < 6; row++)
        {
            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 12; x++)
                {
                    var cell = snapshot.GetCell(left + x, top + row * 4 + y);
                    if (row != visibleRow)
                    {
                        if (!Nullable.Equals<Hex1bColor>(cell.Background, Hex1bColor.Black)
                            || (cell.Character != " " && !Nullable.Equals<Hex1bColor>(cell.Foreground, Hex1bColor.Black)))
                        {
                            return false;
                        }
                        continue;
                    }

                    var foreground = y == 0 || x == 0 ? highlight : x == 11 ? shadow : face;
                    var background = y == 2 || x == 11 ? shadow : x == 0 ? highlight : face;
                    if (y == 1 && x is >= 2 and <= 9)
                    {
                        foreground = ink;
                        background = face;
                    }
                    if (cell.Character != characters[y][x].ToString()
                        || !Nullable.Equals<Hex1bColor>(cell.Foreground, foreground)
                        || !Nullable.Equals<Hex1bColor>(cell.Background, background))
                    {
                        return false;
                    }
                }
                for (var x = 12; x < 15; x++)
                {
                    var cell = snapshot.GetCell(left + x, top + row * 4 + y);
                    if (!Nullable.Equals<Hex1bColor>(cell.Background, Hex1bColor.Black)
                        || (cell.Character != " " && !Nullable.Equals<Hex1bColor>(cell.Foreground, Hex1bColor.Black)))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private static bool LettersMatch(Hex1bTerminalSnapshot snapshot, Dictionary<char, LetterFeedback> usedLetters)
    {
        var left = (snapshot.Width - 52) / 2;
        var top = (snapshot.Height - 30) / 2;
        for (var x = left - 1; x < left + 53; x++)
        {
            var cell = snapshot.GetCell(x, top + 24);
            if (!Nullable.Equals<Hex1bColor>(cell.Background, Hex1bColor.Black)
                || (cell.Character != " " && !Nullable.Equals<Hex1bColor>(cell.Foreground, Hex1bColor.Black)))
            {
                return false;
            }
        }

        for (var index = 0; index < 26; index++)
        {
            var letter = (char)('A' + index);
            var feedback = usedLetters.TryGetValue(letter, out var result)
                ? result
                : (LetterFeedback?)null;
            var (foreground, background) = feedback switch
            {
                LetterFeedback.Correct => (Hex1bColor.FromRgb(145, 175, 140), Hex1bColor.FromRgb(32, 54, 30)),
                LetterFeedback.Present => (Hex1bColor.FromRgb(175, 160, 105), Hex1bColor.FromRgb(58, 50, 23)),
                LetterFeedback.Absent => (Hex1bColor.FromRgb(48, 48, 54), Hex1bColor.FromRgb(10, 10, 12)),
                _ => (Hex1bColor.FromRgb(135, 145, 160), Hex1bColor.FromRgb(22, 32, 44))
            };
            var expected = $" {letter}  ";
            var y = top + 25 + index / 13;
            for (var offset = 0; offset < 4; offset++)
            {
                var cell = snapshot.GetCell((snapshot.Width - 52) / 2 + index % 13 * 4 + offset, y);
                if (cell.Character != expected[offset].ToString()
                    || !Nullable.Equals<Hex1bColor>(cell.Foreground, foreground)
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
            var top = (snapshot.Height - 30) / 2 + row * 4;
            var left = (snapshot.Width - 52) / 2 + 11 + column * 6;
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

    private static bool LetterRowIsVisible(Hex1bTerminalSnapshot snapshot, string word, int row = 0)
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
                    var left = (snapshot.Width - 52) / 2 + 11 + column * 6;
                    var top = (snapshot.Height - 30) / 2 + row * 4;
                    if (snapshot.GetCell(left + x, top + y).Character != expected)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool BoardGapsMatch(Hex1bTerminalSnapshot snapshot, int? activeRow, int? activeColumn)
    {
        var left = (snapshot.Width - 52) / 2 + 11;
        var top = (snapshot.Height - 30) / 2;
        for (var y = 0; y < 24; y++)
        {
            for (var x = 0; x < 29; x++)
            {
                if (x % 6 != 5 && y % 4 != 3)
                {
                    continue;
                }

                var cell = snapshot.GetCell(left + x, top + y);
                var isCursor = y % 4 == 3 && y / 4 == activeRow && x / 6 == activeColumn && x % 6 < 5;
                if (isCursor)
                {
                    if (cell.Character != "▀" || !Nullable.Equals<Hex1bColor>(cell.Foreground, Hex1bColor.FromRgb(230, 60, 60))
                        || !Nullable.Equals<Hex1bColor>(cell.Background, Hex1bColor.Black))
                    {
                        return false;
                    }
                    continue;
                }
                if (!Nullable.Equals<Hex1bColor>(cell.Background, Hex1bColor.Black)
                    || (cell.Character != " " && !Nullable.Equals<Hex1bColor>(cell.Foreground, Hex1bColor.Black)))
                {
                    return false;
                }
            }
        }
        return RemainingPromptsMatch(snapshot, activeRow);
    }

    private static bool RemainingPromptsMatch(Hex1bTerminalSnapshot snapshot, int? activeRow)
    {
        if (snapshot.ContainsText("Guess ") || snapshot.ContainsText("Find the five-letter")
            || snapshot.ContainsText("Enter to submit; Backspace"))
        {
            return false;
        }

        if (activeRow is null)
        {
            return !snapshot.ContainsText("remaining");
        }

        var left = (snapshot.Width - 62) / 2;
        var top = (snapshot.Height - 30) / 2;
        var blue = Hex1bColor.FromRgb(35, 77, 125);
        for (var row = 0; row < 6; row++)
        {
            var label = $"{6 - row} remaining ";
            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 15; x++)
                {
                    var cell = snapshot.GetCell(left + x, top + row * 4 + y);
                    if (row != activeRow)
                    {
                        if (!Nullable.Equals<Hex1bColor>(cell.Background, Hex1bColor.Black)
                            || (cell.Character != " " && !Nullable.Equals<Hex1bColor>(cell.Foreground, Hex1bColor.Black)))
                        {
                            return false;
                        }
                        continue;
                    }

                    var character = x < 13 ? y == 1 && x > 0 ? label[x - 1].ToString() : " "
                        : x == 13 ? y == 0 ? "\uE0B8" : y == 2 ? "\uE0BC" : " "
                        : y == 1 ? "\U0001FB6C" : " ";
                    var foreground = x < 13 ? Hex1bColor.White : blue;
                    var background = x < 13 || x == 13 && y == 1 ? blue : Hex1bColor.Black;
                    if (cell.Character != character || !Nullable.Equals<Hex1bColor>(cell.Foreground, foreground)
                        || !Nullable.Equals<Hex1bColor>(cell.Background, background))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
}
