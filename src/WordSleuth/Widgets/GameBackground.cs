using Hex1b;
using Hex1b.Surfaces;
using Hex1b.Theming;
using Hex1b.Widgets;

namespace WordSleuth;

public sealed class GameBackground
{
    private readonly TimeProvider timeProvider;
    private readonly long startedAt;
    private Hex1bColor[,] bitmap = new Hex1bColor[0, 0];

    public GameBackground(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        startedAt = this.timeProvider.GetTimestamp();
    }

    public Hex1bWidget Build(WidgetContext<ZStackWidget> context, int contentWidth, int contentHeight, int guesses = 0) =>
        context.Surface(layers =>
        [
            layers.Layer(surface => Draw(surface, contentWidth, contentHeight, guesses))
        ]).RedrawAfter(50).Fill();

    private void Draw(Surface surface, int contentWidth, int contentHeight, int guesses)
    {
        var seconds = timeProvider.GetElapsedTime(startedAt).TotalSeconds;
        var pressure = Math.Clamp(guesses / (double)(GameState.MaxGuesses - 1), 0, 1);
        var speed = 1 + pressure * 2;
        var pixelHeight = surface.Height * 2;
        if (bitmap.GetLength(0) != pixelHeight || bitmap.GetLength(1) != surface.Width)
        {
            bitmap = new Hex1bColor[pixelHeight, surface.Width];
        }

        var horizontalMargin = Math.Max(1, (surface.Width - contentWidth) / 2.0 - 2);
        var verticalMargin = Math.Max(1, (pixelHeight - contentHeight * 2) / 2.0 - 2);

        for (var y = 0; y < pixelHeight; y++)
        {
            for (var x = 0; x < surface.Width; x++)
            {
                var dx = x + 0.5 - surface.Width / 2.0;
                var dy = y + 0.5 - pixelHeight / 2.0;
                var distanceX = Math.Max(0, Math.Abs(dx) - contentWidth / 2.0 - 2);
                var distanceY = Math.Max(0, Math.Abs(dy) - contentHeight - 2);
                var fade = Math.Clamp(Math.Max(distanceX / horizontalMargin, distanceY / verticalMargin), 0, 1);
                fade *= fade * (3 - 2 * fade);

                // Overlapping angular waves form drifting rays rather than a uniform vignette.
                var angle = Math.Atan2(dy, dx);
                var warp = pressure * 0.3 * Math.Sin(angle * 5 + seconds * speed);
                var rays = 0.55 + 0.25 * Math.Sin((angle + warp) * 7 + seconds * speed * 0.9)
                    + 0.2 * Math.Sin((angle - warp) * 11 - seconds * speed * 0.65);
                var ebb = 0.85 + 0.15 * Math.Sin(seconds * speed * 0.7 + angle * 3);
                var light = fade * rays * ebb;
                bitmap[y, x] = Hex1bColor.FromRgb((byte)Math.Round((24 + pressure * 72) * light),
                    (byte)Math.Round((44 - pressure * 16) * light), (byte)Math.Round((52 - pressure * 28) * light));
            }
        }

        for (var y = 0; y < surface.Height; y++)
        {
            for (var x = 0; x < surface.Width; x++)
            {
                surface[x, y] = new SurfaceCell("▀", bitmap[y * 2, x], bitmap[y * 2 + 1, x]);
            }
        }
    }
}
