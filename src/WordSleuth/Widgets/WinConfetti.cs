using Hex1b;
using Hex1b.Surfaces;
using Hex1b.Theming;
using Hex1b.Widgets;

namespace WordSleuth;

public sealed class WinConfetti(TimeProvider timeProvider)
{
    private static readonly Hex1bColor[] Colors =
    [
        Hex1bColor.FromRgb(255, 85, 110), Hex1bColor.FromRgb(255, 210, 70),
        Hex1bColor.FromRgb(90, 230, 155), Hex1bColor.FromRgb(85, 185, 255),
        Hex1bColor.FromRgb(200, 120, 255), Hex1bColor.FromRgb(255, 155, 70)
    ];

    private readonly Random random = new();
    private readonly List<Particle> particles = [];
    private Hex1bColor?[,] settled = new Hex1bColor?[0, 0];
    private Hex1bColor?[,] pixels = new Hex1bColor?[0, 0];
    private int[] heights = [];
    private long previousTimestamp;
    private bool launched;

    public Hex1bWidget Build(WidgetContext<ZStackWidget> context, bool won, int contentHeight) =>
        context.Surface(layers => [layers.Layer(surface => Draw(surface, won, contentHeight))])
            .RedrawAfter(50).Fill();

    public void Reset()
    {
        launched = false;
        particles.Clear();
        Array.Clear(settled);
        Array.Clear(heights);
    }

    private void Draw(Surface surface, bool won, int contentHeight)
    {
        if (!won)
        {
            Reset();
            return;
        }

        if (surface.Width == 0 || surface.Height == 0)
        {
            return;
        }

        var width = surface.Width;
        var height = surface.Height * 2;
        if (settled.GetLength(0) != height || heights.Length != width)
        {
            Resize(width, height);
        }

        var now = timeProvider.GetTimestamp();
        if (!launched)
        {
            Launch(width, height);
            previousTimestamp = now;
            launched = true;
        }

        var elapsed = Math.Min(10, timeProvider.GetElapsedTime(previousTimestamp, now).TotalSeconds);
        previousTimestamp = now;
        while (elapsed > 0 && particles.Count > 0)
        {
            var step = Math.Min(elapsed, 1.0 / 60);
            Advance(width, height, step);
            elapsed -= step;
        }

        Array.Clear(pixels);
        foreach (var particle in particles)
        {
            var x = (int)Math.Round(particle.X);
            var y = (int)Math.Round(particle.Y);
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                pixels[y, x] = particle.Color;
            }
        }

        for (var y = 0; y < surface.Height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                // Confetti crosses the board, but leaves replay controls and the result readable.
                if (y >= (surface.Height - contentHeight) / 2 + WordBoard.Height + 1
                    && y < (surface.Height - contentHeight) / 2 + contentHeight
                    && x >= (width - 52) / 2 && x < (width - 52) / 2 + 52)
                {
                    continue;
                }
                var upper = pixels[y * 2, x] ?? settled[y * 2, x];
                var lower = pixels[y * 2 + 1, x] ?? settled[y * 2 + 1, x];
                if (upper.HasValue)
                {
                    surface[x, y] = new SurfaceCell("▀", upper, lower);
                }
                else if (lower.HasValue)
                {
                    surface[x, y] = new SurfaceCell("▄", lower, null);
                }
            }
        }
    }

    private void Launch(int width, int height)
    {
        var gravity = height * 1.2;
        for (var i = 0; i < Math.Clamp(width * 10, 600, 1600); i++)
        {
            var left = i % 2 == 0;
            particles.Add(new Particle
            {
                X = left ? 0 : width - 1,
                Y = height - 1,
                VelocityX = (left ? 1 : -1) * width * (0.2 + random.NextDouble() * 0.65),
                VelocityY = -Math.Sqrt(2 * gravity * height) * (0.65 + random.NextDouble() * 0.65),
                Color = Colors[random.Next(Colors.Length)]
            });
        }
    }

    private void Advance(int width, int height, double step)
    {
        for (var i = particles.Count - 1; i >= 0; i--)
        {
            var particle = particles[i];
            particle.VelocityY += height * 1.2 * step;
            particle.X += particle.VelocityX * step;
            particle.Y += particle.VelocityY * step;
            if (particle.X < 0 || particle.X > width - 1)
            {
                particle.X = Math.Clamp(particle.X, 0, width - 1);
                particle.VelocityX *= -0.6;
            }

            var column = (int)Math.Round(particle.X);
            if (particle.VelocityY > 0 && particle.Y >= height - 1 - heights[column])
            {
                Deposit(column, particle.Color);
                particles.RemoveAt(i);
            }
        }
    }

    private void Deposit(int column, Hex1bColor color)
    {
        // Let pieces slide toward lower neighbors rather than building vertical towers.
        while (true)
        {
            var left = Math.Max(0, column - 1);
            var right = Math.Min(heights.Length - 1, column + 1);
            var next = heights[left] == heights[right]
                ? random.Next(2) == 0 ? left : right
                : heights[left] < heights[right] ? left : right;
            if (heights[column] <= heights[next] + 2)
            {
                break;
            }
            column = next;
        }

        if (heights[column] < settled.GetLength(0))
        {
            settled[settled.GetLength(0) - 1 - heights[column], column] = color;
            heights[column]++;
        }
    }

    private void Resize(int width, int height)
    {
        var old = settled;
        var oldWidth = heights.Length;
        settled = new Hex1bColor?[height, width];
        pixels = new Hex1bColor?[height, width];
        heights = new int[width];
        if (oldWidth == 0)
        {
            return;
        }

        foreach (var particle in particles)
        {
            particle.X = Math.Clamp(particle.X * width / oldWidth, 0, width - 1);
            particle.Y *= height / (double)old.GetLength(0);
            particle.VelocityX *= width / (double)oldWidth;
            particle.VelocityY *= height / (double)old.GetLength(0);
        }
        for (var x = 0; x < oldWidth; x++)
        {
            for (var y = old.GetLength(0) - 1; y >= 0; y--)
            {
                if (old[y, x] is { } color)
                {
                    Deposit(Math.Min(width - 1, x * width / oldWidth), color);
                }
            }
        }
    }

    private sealed class Particle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public Hex1bColor Color { get; init; }
    }
}
