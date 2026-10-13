using Hex1b;
using Hex1b.Surfaces;
using Hex1b.Theming;
using Hex1b.Widgets;

namespace WordSleuth;

public static class RemainingGuesses
{
    public const int Width = 14;
    private static readonly Hex1bColor Blue = Hex1bColor.FromRgb(35, 77, 125);

    public static Hex1bWidget Build(WidgetContext<HStackWidget> context, int? remaining)
    {
        if (!remaining.HasValue)
        {
            return context.Text("").FixedWidth(Width).FixedHeight(LetterTile.Height);
        }

        return context.Surface(layers => [layers.Layer(surface => Draw(surface, remaining.Value))])
            .FixedWidth(Width).FixedHeight(LetterTile.Height);
    }

    private static void Draw(Surface surface, int remaining)
    {
        var label = $"{remaining}  remaining";
        for (var y = 0; y < LetterTile.Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                surface[x, y] = y != 1
                    ? new SurfaceCell(" ", Hex1bColor.Black, Hex1bColor.Black)
                    : x < Width - 1
                        ? new SurfaceCell(x > 0 ? label[x - 1].ToString() : " ", Hex1bColor.White, Blue)
                        : new SurfaceCell("\uE0B0", Blue, Hex1bColor.Black);
            }
        }
    }
}
