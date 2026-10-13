using Hex1b;
using Hex1b.Automation;
using Hex1b.Input;
using Hex1b.Theming;
using WordSleuth;

namespace WordSleuth.Tests;

public class WinExperienceTests
{
    [Fact]
    public async Task FansRecycleExistingPiecesWhileOneAndAHalfPerSecondFadeAndDisappear()
    {
        var clock = new AnimationClock();
        var confetti = new WinConfetti(clock, new Random(42));
        var won = true;
        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithHeadless(new TerminalCapabilities { SupportsTrueColor = true })
            .WithDimensions(80, 40)
            .WithHex1bApp(context => context.ZStack(z => [confetti.Build(z, won)]).Fill()).Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var running = terminal.RunAsync(timeout.Token);
        var visibleFade = false;
        try
        {
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(_ => confetti.RemainingPieces == 800, TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);
            for (var seconds = 1; seconds <= 30; seconds++)
            {
                clock.Advance(TimeSpan.FromSeconds(1));
                await new Hex1bTerminalInputSequenceBuilder()
                    .WaitUntil(s =>
                    {
                        if (confetti.ElapsedSeconds < seconds - 0.000001) return false;
                        visibleFade |= Enumerable.Range(0, s.Height).Any(y =>
                            Enumerable.Range(0, s.Width).Any(x =>
                            {
                                var cell = s.GetCell(x, y);
                                return (cell.Character is "▀" or "▄")
                                    && (IsFaded(cell.Foreground) || IsFaded(cell.Background));
                            }));
                        return true;
                    }, TimeSpan.FromSeconds(5)).Build().ApplyAsync(terminal, timeout.Token);
                var selected = 800 - confetti.RemainingPieces + confetti.FadingPieces;
                Assert.InRange(selected, (int)Math.Floor(seconds * 1.5) - 1, (int)Math.Floor(seconds * 1.5));
                Assert.InRange(confetti.FadingPieces, 1, 3);
                if (seconds == 1) Assert.True(confetti.AirbornePieces > 400);
                if (seconds >= 10) Assert.True(confetti.AirbornePieces < 100, "Ambient fans should be much quieter than the opening blast.");
            }
            Assert.True(visibleFade);
            Assert.True(confetti.RecycledPieces > 0);
            Assert.InRange(800 - confetti.RemainingPieces, 41, 44);
            won = false;
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(_ => confetti.RemainingPieces == 0 && confetti.FadingPieces == 0
                    && confetti.RecycledPieces == 0 && confetti.ElapsedSeconds == 0, TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);
        }
        finally
        {
            await timeout.CancelAsync();
            await running;
        }
    }

    private static bool IsFaded(Hex1bColor? color) =>
        color is { } value && value.R + value.G + value.B > 0 && !IsConfetti(value);

    [Fact]
    public async Task CornerStreamsHaveAnOpeningOutburstAndSeparatedTrailingPulses()
    {
        var clock = new AnimationClock();
        var screen = new GameScreen(new GameState("APPLE"), clock);
        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithHeadless(new TerminalCapabilities { SupportsTrueColor = true })
            .WithDimensions(120, 60).WithHex1bApp(screen.Build).Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var running = terminal.RunAsync(timeout.Token);
        try
        {
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => s.ContainsText("6 remaining"), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.F9)
                .WaitUntil(s => ReplayMatches(s, true) && ConfettiPixels(s).Count == 2, TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            clock.Advance(TimeSpan.FromMilliseconds(250));
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => ConfettiPixels(s, includeBoard: true).Count > 40, TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            clock.Advance(TimeSpan.FromMilliseconds(250));
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => CornerPixels(s) == 0, TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            clock.Advance(TimeSpan.FromMilliseconds(200));
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => CornerPixels(s) > 5, TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            clock.Advance(TimeSpan.FromMilliseconds(250));
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => CornerPixels(s) == 0, TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            clock.Advance(TimeSpan.FromMilliseconds(350));
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => CornerPixels(s) > 2, TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.R, Hex1bModifiers.Control)
                .WaitUntil(s => s.ContainsText("6 remaining") && ConfettiPixels(s).Count == 0, TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.C, Hex1bModifiers.Control).Build().ApplyAsync(terminal, timeout.Token);
            await running.WaitAsync(timeout.Token);
        }
        finally
        {
            await timeout.CancelAsync();
            await running;
        }
    }

    private static int CornerPixels(Hex1bTerminalSnapshot snapshot) =>
        ConfettiPixels(snapshot).Keys.Count(p => p.Y >= snapshot.Height - 5
            && (p.X < 15 || p.X >= snapshot.Width - 15));

    [Theory]
    [InlineData(80, 60, true, true)]
    [InlineData(62, 30, true, false)]
    [InlineData(120, 40, false, true)]
    [InlineData(62, 30, false, false)]
    public async Task EndStatesReplaceTheAlphabetWithBeveledReplayAndSupportMouseAndEnter(
        int width, int height, bool won, bool click)
    {
        var game = new GameState("APPLE");
        if (!won)
        {
            for (var i = 0; i < 5; i++)
            {
                foreach (var letter in "WORLD") game.TypeLetter(letter);
                game.Submit();
            }
        }

        var clock = new AnimationClock();
        var screen = new GameScreen(game, clock);
        await using var terminal = Hex1bTerminal.CreateBuilder().WithMouse()
            .WithHeadless(new TerminalCapabilities { SupportsTrueColor = true, SupportsMouse = true })
            .WithDimensions(width, height).WithHex1bApp(screen.Build).Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var running = terminal.RunAsync(timeout.Token);
        try
        {
            var input = new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => s.ContainsText(won ? "6 remaining" : "1 remaining")
                    && s.ContainsText(" A   B   C"), TimeSpan.FromSeconds(5));
            if (won) input.Type("ab").Key(Hex1bKey.F9);
            else input.Type("world").Enter();

            await input.WaitUntil(s => ReplayMatches(s, won)
                    && (won ? !s.ContainsText("Solved") : s.ContainsText("The word was APPLE"))
                    && !s.ContainsText(" A   B   C") && !s.ContainsText("remaining")
                    && !s.ContainsText("SUBMIT"), TimeSpan.FromSeconds(5))
                .Type("world").Backspace().Key(Hex1bKey.F9)
                .WaitUntil(s => ReplayMatches(s, won), TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);
            Assert.Equal(won, game.IsWon);
            Assert.True(game.IsFinished);
            Assert.Empty(game.CurrentGuess);
            Assert.Equal(won ? 1 : 6, game.Guesses.Count);

            if (won && click)
            {
                clock.Advance(TimeSpan.FromSeconds(1));
                await new Hex1bTerminalInputSequenceBuilder()
                    .WaitUntil(s => ConfettiPixels(s, includeBoard: true).Count > 25, TimeSpan.FromSeconds(5))
                    .Build().ApplyAsync(terminal, timeout.Token);
            }

            var replay = new Hex1bTerminalInputSequenceBuilder();
            if (click)
            {
                replay.ClickAt(width / 2, (height - 30) / 2 + 26, MouseButton.Left);
            }
            else replay.Enter();
            await replay.WaitUntil(s => s.ContainsText("6 remaining") && s.ContainsText(" A   B   C")
                    && !s.ContainsText("PLAY AGAIN") && !s.ContainsText("Solved"), TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.C, Hex1bModifiers.Control).Build().ApplyAsync(terminal, timeout.Token);
            await running.WaitAsync(timeout.Token);
            Assert.False(game.IsFinished);
            Assert.Empty(game.Guesses);
            Assert.Empty(game.UsedLetters);
        }
        finally
        {
            await timeout.CancelAsync();
            await running;
        }
    }

    [Fact]
    public async Task ConfettiStreamsCrossTheGridFallIntoPilesAndRecirculateUntilReplay()
    {
        var clock = new AnimationClock();
        var screen = new GameScreen(new GameState("APPLE"), clock);
        var presentation = new HeadlessPresentationAdapter(120, 60,
            new TerminalCapabilities { SupportsTrueColor = true });
        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithPresentation(presentation).WithHex1bApp(screen.Build).Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var running = terminal.RunAsync(timeout.Token);
        Dictionary<(int X, int Y, bool Upper), Hex1bColor> piles = [];
        try
        {
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => s.ContainsText("6 remaining") && ConfettiPixels(s).Count == 0, TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.F9)
                .WaitUntil(s => ReplayMatches(s, true) && ConfettiPixels(s).Count == 2
                    && ConfettiPixels(s).Keys.All(p => p.Y == 59 && (p.X == 0 || p.X == 119)), TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            clock.Advance(TimeSpan.FromSeconds(1));
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => ConfettiPixels(s).Count > 100
                    && ConfettiPixels(s).Keys.Any(p => p.Y < 40)
                    && ConfettiAcrossTiles(s) > 20,
                    TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            clock.Advance(TimeSpan.FromSeconds(8));
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s =>
                {
                    piles = BottomPixels(s);
                    return piles.Count > 150 && ConfettiPixels(s).Keys.Any(p => p.Y < 52);
                }, TimeSpan.FromSeconds(5)).Build().ApplyAsync(terminal, timeout.Token);
            Assert.True(piles.Values.Distinct().Count() >= 5);
            Assert.True(piles.Keys.GroupBy(p => p.X).Select(g => g.Count()).Distinct().Count() >= 3);
            Assert.True(piles.Keys.Any(p => p.Upper) && piles.Keys.Any(p => !p.Upper));

            clock.Advance(TimeSpan.FromSeconds(2));
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => BottomPixels(s).Count > 100 && !SamePixels(piles, BottomPixels(s))
                    && ConfettiPixels(s).Keys.Count(p => p.Y < 52) > 5, TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            presentation.TriggerResize(100, 50);
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => s.Width == 100 && s.Height == 50
                    && BottomPixels(s).Count > 75 && ConfettiPixels(s).Keys.Any(p => p.Y < 42),
                    TimeSpan.FromSeconds(5))
                .Enter()
                .WaitUntil(s => s.ContainsText("6 remaining") && ConfettiPixels(s).Count == 0, TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.F9)
                .WaitUntil(s => ReplayMatches(s, true) && ConfettiPixels(s).Count == 2
                    && ConfettiPixels(s).Keys.All(p => p.Y == 49 && (p.X == 0 || p.X == 99)), TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            presentation.TriggerResize(110, 55);
            clock.Advance(TimeSpan.FromMilliseconds(400));
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => s.Width == 110 && s.Height == 55 && ReplayMatches(s, true)
                    && ConfettiPixels(s, includeBoard: true).Count > 100, TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.R, Hex1bModifiers.Control)
                .WaitUntil(s => s.ContainsText("6 remaining") && ConfettiPixels(s).Count == 0, TimeSpan.FromSeconds(5))
                .Key(Hex1bKey.C, Hex1bModifiers.Control).Build().ApplyAsync(terminal, timeout.Token);
            await running.WaitAsync(timeout.Token);
        }
        finally
        {
            await timeout.CancelAsync();
            await running;
        }
    }

    private static bool ReplayMatches(Hex1bTerminalSnapshot snapshot, bool won)
    {
        const string label = "PLAY AGAIN ↵";
        var face = won ? Hex1bColor.FromRgb(72, 166, 90) : Hex1bColor.FromRgb(56, 83, 62);
        var highlight = won ? Hex1bColor.FromRgb(137, 224, 148) : Hex1bColor.FromRgb(89, 118, 96);
        var shadow = won ? Hex1bColor.FromRgb(28, 93, 42) : Hex1bColor.FromRgb(28, 46, 33);
        var ink = won ? Hex1bColor.FromRgb(12, 39, 18) : Hex1bColor.FromRgb(174, 192, 179);
        var left = (snapshot.Width - 16) / 2;
        var top = (snapshot.Height - 30) / 2 + 25;
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                var cell = snapshot.GetCell(left + x, top + y);
                var text = y == 1 && x is >= 2 and < 14;
                var foreground = text ? ink : y == 0 || x == 0 ? highlight : x == 15 ? shadow : face;
                var background = y == 2 || x == 15 ? shadow : x == 0 ? highlight : face;
                if (cell.Character != (text ? label[x - 2].ToString() : "▀")
                    || !Nullable.Equals<Hex1bColor>(cell.Foreground, foreground)
                    || !Nullable.Equals<Hex1bColor>(cell.Background, background))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static Dictionary<(int X, int Y, bool Upper), Hex1bColor> ConfettiPixels(
        Hex1bTerminalSnapshot snapshot, bool includeBoard = false)
    {
        var result = new Dictionary<(int X, int Y, bool Upper), Hex1bColor>();
        for (var y = 0; y < snapshot.Height; y++)
        {
            for (var x = 0; x < snapshot.Width; x++)
            {
                if (!includeBoard && x >= (snapshot.Width - 62) / 2 && x < (snapshot.Width - 62) / 2 + 62
                    && y >= (snapshot.Height - 30) / 2 && y < (snapshot.Height - 30) / 2 + 30)
                {
                    continue;
                }
                var cell = snapshot.GetCell(x, y);
                if (cell.Foreground is { } upper && IsConfetti(upper))
                    result[(x, y, cell.Character != "▄")] = upper;
                if (cell.Background is { } lower && IsConfetti(lower))
                    result[(x, y, false)] = lower;
            }
        }
        return result;
    }

    private static bool IsConfetti(Hex1bColor color) => (color.R, color.G, color.B) is
        (255, 85, 110) or (255, 210, 70) or (90, 230, 155)
        or (85, 185, 255) or (200, 120, 255) or (255, 155, 70);

    private static int ConfettiAcrossTiles(Hex1bTerminalSnapshot snapshot)
    {
        var left = (snapshot.Width - 52) / 2 + 11;
        var top = (snapshot.Height - 30) / 2;
        var count = 0;
        for (var y = top; y < top + 24; y++)
        {
            for (var x = left; x < left + 29; x++)
            {
                if ((x - left) % 6 == 5 || (y - top) % 4 == 3) continue;
                var cell = snapshot.GetCell(x, y);
                if (cell.Foreground is { } foreground && IsConfetti(foreground)) count++;
                if (cell.Background is { } background && IsConfetti(background)) count++;
            }
        }
        return count;
    }

    private static bool SamePixels(Dictionary<(int, int, bool), Hex1bColor> first,
        Dictionary<(int, int, bool), Hex1bColor> second) =>
        first.Count == second.Count && first.All(p => second.TryGetValue(p.Key, out var color) && color.Equals(p.Value));

    private static Dictionary<(int X, int Y, bool Upper), Hex1bColor> BottomPixels(Hex1bTerminalSnapshot snapshot) =>
        ConfettiPixels(snapshot).Where(p => p.Key.Y >= snapshot.Height - 8).ToDictionary();

    private sealed class AnimationClock : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref timestamp);
        public void Advance(TimeSpan elapsed) => Interlocked.Add(ref timestamp, elapsed.Ticks);
    }
}
