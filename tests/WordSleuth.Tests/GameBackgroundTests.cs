using Hex1b;
using Hex1b.Automation;
using Hex1b.Input;
using Hex1b.Theming;
using WordSleuth;

namespace WordSleuth.Tests;

public class GameBackgroundTests
{
    [Theory]
    [InlineData(80, 60)]
    [InlineData(120, 40)]
    [InlineData(80, 30)]
    [InlineData(142, 52)]
    public async Task HalfBlockRaysAnimateWhileIdleAndStayBlackAroundTheGame(int width, int height)
    {
        var clock = new AnimationClock();
        var screen = new GameScreen(new GameState("APPLE"), clock);
        var presentation = new HeadlessPresentationAdapter(width, height,
            new TerminalCapabilities { SupportsTrueColor = true });
        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithPresentation(presentation).WithHex1bApp(screen.Build).Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var running = terminal.RunAsync(timeout.Token);
        (Hex1bColor? Upper, Hex1bColor? Lower)[] initial = [];

        try
        {
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s =>
                {
                    if (!s.ContainsText("[_____]") || s.GetCell(0, 0).Character != "▀")
                    {
                        return false;
                    }

                    AssertBlackMargin(s);
                    initial = EdgePixels(s);
                    Assert.True(initial.Distinct().Count() > 10, "The edge lighting should be nonuniform.");
                    Assert.Contains(initial, pixel => !Nullable.Equals(pixel.Upper, pixel.Lower));
                    Assert.Contains(initial, pixel => pixel.Upper?.B >= 32 || pixel.Lower?.B >= 32);
                    foreach (var (upper, lower) in initial)
                    {
                        AssertDarkBlue(upper);
                        AssertDarkBlue(lower);
                    }
                    return true;
                }, TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            clock.Advance(TimeSpan.FromSeconds(1));
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s => EdgePixels(s).Zip(initial).Count(pair =>
                    Math.Abs((pair.First.Upper?.B ?? 0) - (pair.Second.Upper?.B ?? 0)) >= 3
                    || Math.Abs((pair.First.Lower?.B ?? 0) - (pair.Second.Lower?.B ?? 0)) >= 3)
                    >= initial.Length / 4, TimeSpan.FromSeconds(5))
                .WaitUntil(s =>
                {
                    AssertBlackMargin(s);
                    return s.ContainsText("[_____]");
                }, TimeSpan.FromSeconds(5))
                .Build().ApplyAsync(terminal, timeout.Token);

            presentation.TriggerResize(width + 12, height + 4);
            await new Hex1bTerminalInputSequenceBuilder()
                .WaitUntil(s =>
                {
                    if (s.Width != width + 12 || s.Height != height + 4 || !s.ContainsText("[_____]"))
                    {
                        return false;
                    }
                    return BlackMarginMatches(s) && s.GetCell(0, 0).Character == "▀";
                }, TimeSpan.FromSeconds(5))
                .Type("apple").Enter()
                .WaitUntil(s => s.ContainsText("Solved in 1"), TimeSpan.FromSeconds(5))
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

    private static (Hex1bColor? Upper, Hex1bColor? Lower)[] EdgePixels(Hex1bTerminalSnapshot snapshot) =>
        Enumerable.Range(0, snapshot.Width).Select(x => snapshot.GetCell(x, 0))
            .Concat(Enumerable.Range(0, snapshot.Height).Select(y => snapshot.GetCell(0, y)))
            .Select(cell => (cell.Foreground, cell.Background)).ToArray();

    private static void AssertDarkBlue(Hex1bColor? color)
    {
        Assert.True(color.HasValue);
        var value = color.Value;
        Assert.InRange(value.R, (byte)0, (byte)32);
        Assert.InRange(value.G, value.R, (byte)38);
        Assert.InRange(value.B, value.G, (byte)52);
    }

    private static void AssertBlackMargin(Hex1bTerminalSnapshot snapshot) => Assert.True(BlackMarginMatches(snapshot));

    private static bool BlackMarginMatches(Hex1bTerminalSnapshot snapshot)
    {
        var left = (snapshot.Width - 54) / 2;
        var top = (snapshot.Height - 28) / 2;
        for (var y = top; y < top + 28; y++)
        {
            var cell = snapshot.GetCell(left - 1, y);
            if (!Nullable.Equals<Hex1bColor>(cell.Foreground, Hex1bColor.Black)
                || !Nullable.Equals<Hex1bColor>(cell.Background, Hex1bColor.Black))
            {
                return false;
            }
        }
        return true;
    }

    private sealed class AnimationClock : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref timestamp);
        public void Advance(TimeSpan elapsed) => Interlocked.Add(ref timestamp, elapsed.Ticks);
    }
}
