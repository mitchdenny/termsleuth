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
    private Piece?[,] settled = new Piece?[0, 0];
    private Hex1bColor?[,] pixels = new Hex1bColor?[0, 0];
    private int[] heights = [];
    private long previousTimestamp;
    private bool launched;
    private double simulationTime;
    private double fanLiftBudget;
    private double fadeBudget;

    internal WinConfetti(TimeProvider timeProvider, Random random) : this(timeProvider)
    {
        this.random = random;
    }

    internal int RemainingPieces => particles.Count + heights.Sum();
    internal int AirbornePieces => particles.Count(p => p.Delay <= 0);
    internal int RecycledPieces { get; private set; }
    internal double ElapsedSeconds => simulationTime;
    internal int FadingPieces => particles.Count(p => p.Piece.FadeRemaining >= 0)
        + settled.Cast<Piece?>().Count(p => p?.FadeRemaining >= 0);

    public Hex1bWidget Build(WidgetContext<ZStackWidget> context, bool won) =>
        context.Surface(layers => [layers.Layer(surface => Draw(surface, won))])
            .RedrawAfter(50).Fill();

    public void Reset()
    {
        launched = false;
        simulationTime = 0;
        fanLiftBudget = 0;
        fadeBudget = 0;
        RecycledPieces = 0;
        particles.Clear();
        Array.Clear(settled);
        Array.Clear(heights);
    }

    private void Draw(Surface surface, bool won)
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
        while (elapsed > 0)
        {
            var step = Math.Min(elapsed, 1.0 / 60);
            Advance(width, height, step);
            elapsed -= step;
        }

        Array.Clear(pixels);
        foreach (var particle in particles)
        {
            if (particle.Delay > 0)
            {
                continue;
            }
            var x = (int)Math.Round(particle.X);
            var y = (int)Math.Round(particle.Y);
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                pixels[y, x] = particle.Piece.DisplayColor;
            }
        }

        for (var y = 0; y < surface.Height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var upper = pixels[y * 2, x] ?? settled[y * 2, x]?.DisplayColor;
                var lower = pixels[y * 2 + 1, x] ?? settled[y * 2 + 1, x]?.DisplayColor;
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
        var gravity = height * 0.55;
        var count = Math.Clamp(width * 10, 600, 1600);
        for (var i = 0; i < count; i++)
        {
            var left = i % 2 == 0;
            var fraction = i / (double)count;
            var delay = i < 2 ? 0 : fraction switch
            {
                < 0.55 => 0.02 + random.NextDouble() * 0.33,
                < 0.8 => 0.6 + random.NextDouble() * 0.2,
                < 0.92 => 1.2 + random.NextDouble() * 0.2,
                < 0.98 => 1.9 + random.NextDouble() * 0.2,
                _ => 2.7 + random.NextDouble() * 0.2
            };
            var angle = 0.42 + Math.Sin(delay * 4) * 0.06 + (random.NextDouble() - 0.5) * 0.12;
            var speed = Math.Sqrt(2 * gravity * height) * (1.1 + random.NextDouble() * 0.45);
            particles.Add(new Particle
            {
                X = left ? 0 : width - 1,
                Y = height - 1,
                VelocityX = (left ? 1 : -1) * speed * Math.Sin(angle) * width / height,
                VelocityY = -speed * Math.Cos(angle),
                Delay = delay,
                Drag = 0.35 + random.NextDouble() * 0.3,
                FlutterPhase = random.NextDouble() * Math.Tau,
                FlutterRate = 5 + random.NextDouble() * 7,
                Piece = new Piece { Color = Colors[random.Next(Colors.Length)] }
            });
        }
    }

    private void Advance(int width, int height, double step)
    {
        simulationTime += step;
        LiftFromPiles(width, height, step);
        FadePieces(step);
        for (var i = particles.Count - 1; i >= 0; i--)
        {
            var particle = particles[i];
            if (particle.Piece.FadeRemaining >= 0)
            {
                particle.Piece.FadeRemaining -= step;
                if (particle.Piece.FadeRemaining <= 0)
                {
                    particles.RemoveAt(i);
                    continue;
                }
            }
            var activeStep = step;
            if (particle.Delay > 0)
            {
                activeStep = Math.Max(0, step - particle.Delay);
                particle.Delay = Math.Max(0, particle.Delay - step);
                if (activeStep == 0) continue;
            }
            particle.Age += activeStep;
            particle.BrakeRemaining = Math.Max(0, particle.BrakeRemaining - activeStep);
            if (particle.Age > 0.35 && particle.BrakeRemaining == 0 && random.NextDouble() < activeStep * 0.65)
            {
                particle.BrakeRemaining = 0.1 + random.NextDouble() * 0.2;
                particle.BrakeDrag = 3 + random.NextDouble() * 5;
            }
            var drag = particle.Age < 0.2 ? 0.05 : particle.Drag;
            if (particle.BrakeRemaining > 0) drag += particle.BrakeDrag;
            var damping = Math.Exp(-drag * activeStep);
            particle.VelocityX = particle.VelocityX * damping
                + Math.Sin(particle.Age * particle.FlutterRate + particle.FlutterPhase) * width * 0.04 * activeStep;
            particle.VelocityY = particle.VelocityY * damping + height * 0.55 * activeStep;
            Blow(particle, width, height, activeStep);
            particle.X += particle.VelocityX * activeStep;
            particle.Y += particle.VelocityY * activeStep;
            if (particle.X < 0 || particle.X > width - 1)
            {
                particle.X = Math.Clamp(particle.X, 0, width - 1);
                particle.VelocityX *= -0.6;
            }

            var column = (int)Math.Round(particle.X);
            if (particle.VelocityY > 0 && particle.Y >= height - 1 - heights[column])
            {
                Deposit(column, particle.Piece);
                particles.RemoveAt(i);
            }
        }
    }

    private double Ambient => Math.Clamp((simulationTime - 3) / 2, 0, 1);

    private double FanPosition(int width, bool left)
    {
        var travel = simulationTime <= 3 ? 0
            : width * 0.22 * (1 - Math.Cos((simulationTime - 3) * (left ? 0.4 : 0.32)));
        return left ? travel : width - 1 - travel;
    }

    private double FanReach(bool left) =>
        (1 - Ambient) * (0.32 + 0.24 * Math.Max(0, Math.Sin(simulationTime * 0.45 + (left ? 0 : Math.PI))))
        + Ambient * 0.24;

    private double Influence(double x, double y, int width, int height, bool left)
    {
        var dx = Math.Abs(x - FanPosition(width, left)) / (width * FanReach(left));
        var dy = Math.Max(0, height - 1 - y) / (height * 0.4);
        var strength = Math.Clamp(1 - Math.Sqrt(dx * dx + dy * dy), 0, 1);
        return strength * strength;
    }

    private void Blow(Particle particle, int width, int height, double step)
    {
        var left = Influence(particle.X, particle.Y, width, height, true);
        var right = Influence(particle.X, particle.Y, width, height, false);
        var strength = left + right;
        if (strength == 0) return;
        var gust = 0.9 + (0.25 - Ambient * 0.13) * Math.Sin(simulationTime * (2.3 - Ambient * 1.5) + particle.FlutterPhase);
        var blend = 1 - Math.Exp(-strength * (5 - Ambient * 3) * step);
        var horizontal = (left - right) / strength * width * (0.65 - Ambient * 0.5) * gust;
        particle.VelocityX += (horizontal - particle.VelocityX) * blend;
        particle.VelocityY += (-height * (1.65 - Ambient * 0.9) * gust - particle.VelocityY) * blend;
    }

    private void LiftFromPiles(int width, int height, double step)
    {
        fanLiftBudget += width * (1.2 - Ambient * 0.98) * step;
        while (fanLiftBudget >= 1)
        {
            fanLiftBudget--;
            var left = random.Next(2) == 0;
            var reach = width * FanReach(left);
            var column = (int)Math.Clamp(Math.Round(FanPosition(width, left) + (random.NextDouble() * 2 - 1) * reach),
                0, width - 1);
            var y = height - heights[column];
            if (heights[column] == 0 || random.NextDouble() > Math.Sqrt(Influence(column, y, width, height, left)))
            {
                continue;
            }
            var piece = settled[y, column]!;
            settled[y, column] = null;
            heights[column]--;
            RecycledPieces++;
            particles.Add(new Particle
            {
                X = column,
                Y = y,
                VelocityX = (left ? 1 : -1) * width * (0.35 - Ambient * 0.27 + random.NextDouble() * 0.1),
                VelocityY = -height * (1.65 - Ambient * 0.8 + random.NextDouble() * 0.35),
                Drag = 0.35 + random.NextDouble() * 0.3,
                FlutterPhase = random.NextDouble() * Math.Tau,
                FlutterRate = 5 + random.NextDouble() * 7,
                Piece = piece
            });
        }
    }

    private void FadePieces(double step)
    {
        fadeBudget = Math.Min(2, fadeBudget + 1.5 * step);
        while (fadeBudget >= 1)
        {
            var airborne = particles.Count(p => p.Delay <= 0 && p.Piece.FadeRemaining < 0);
            var grounded = settled.Cast<Piece?>().Count(p => p?.FadeRemaining < 0);
            if (airborne + grounded == 0) break;
            var selected = random.Next(airborne + grounded);
            Piece piece;
            if (selected < airborne)
            {
                piece = particles.Where(p => p.Delay <= 0 && p.Piece.FadeRemaining < 0).ElementAt(selected).Piece;
            }
            else
            {
                piece = settled.Cast<Piece?>().Where(p => p?.FadeRemaining < 0).ElementAt(selected - airborne)!;
            }
            piece.FadeRemaining = Piece.FadeDuration;
            fadeBudget--;
        }

        for (var x = 0; x < heights.Length; x++)
        {
            var previousHeight = heights[x];
            var kept = 0;
            for (var i = 0; i < previousHeight; i++)
            {
                var piece = settled[settled.GetLength(0) - 1 - i, x]!;
                if (piece.FadeRemaining >= 0)
                {
                    piece.FadeRemaining -= step;
                    if (piece.FadeRemaining <= 0) continue;
                }
                settled[settled.GetLength(0) - 1 - kept++, x] = piece;
            }
            for (var i = kept; i < previousHeight; i++)
            {
                settled[settled.GetLength(0) - 1 - i, x] = null;
            }
            heights[x] = kept;
        }
    }

    private void Deposit(int column, Piece piece)
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
            settled[settled.GetLength(0) - 1 - heights[column], column] = piece;
            heights[column]++;
        }
    }

    private void Resize(int width, int height)
    {
        var old = settled;
        var oldWidth = heights.Length;
        settled = new Piece?[height, width];
        pixels = new Hex1bColor?[height, width];
        heights = new int[width];
        if (oldWidth == 0)
        {
            return;
        }

        foreach (var particle in particles)
        {
            if (particle.Delay > 0)
            {
                particle.X = particle.X < oldWidth / 2.0 ? 0 : width - 1;
                particle.Y = height - 1;
            }
            else
            {
                particle.X = Math.Clamp(particle.X * width / oldWidth, 0, width - 1);
                particle.Y *= height / (double)old.GetLength(0);
            }
            particle.VelocityX *= width / (double)oldWidth;
            particle.VelocityY *= height / (double)old.GetLength(0);
        }
        for (var x = 0; x < oldWidth; x++)
        {
            for (var y = old.GetLength(0) - 1; y >= 0; y--)
            {
                if (old[y, x] is { } piece)
                {
                    Deposit(Math.Min(width - 1, x * width / oldWidth), piece);
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
        public double Delay { get; set; }
        public double Age { get; set; }
        public double Drag { get; init; }
        public double FlutterPhase { get; init; }
        public double FlutterRate { get; init; }
        public double BrakeRemaining { get; set; }
        public double BrakeDrag { get; set; }
        public required Piece Piece { get; init; }
    }

    private sealed class Piece
    {
        public const double FadeDuration = 1.5;
        public required Hex1bColor Color { get; init; }
        public double FadeRemaining { get; set; } = -1;
        public Hex1bColor DisplayColor
        {
            get
            {
                var opacity = FadeRemaining < 0 ? 1 : Math.Clamp(FadeRemaining / FadeDuration, 0, 1);
                return Hex1bColor.FromRgb((byte)Math.Round(Color.R * opacity),
                    (byte)Math.Round(Color.G * opacity), (byte)Math.Round(Color.B * opacity));
            }
        }
    }
}
