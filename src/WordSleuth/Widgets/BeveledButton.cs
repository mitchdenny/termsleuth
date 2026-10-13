using Hex1b;
using Hex1b.Surfaces;
using Hex1b.Theming;
using Hex1b.Widgets;

namespace WordSleuth;

public readonly record struct ButtonPalette(Hex1bColor Face, Hex1bColor Highlight, Hex1bColor Shadow, Hex1bColor Ink)
{
    public static ButtonPalette Beige { get; } = new(
        Hex1bColor.FromRgb(204, 185, 150), Hex1bColor.FromRgb(242, 224, 189),
        Hex1bColor.FromRgb(130, 111, 80), Hex1bColor.FromRgb(56, 46, 32));

    public static ButtonPalette Green { get; } = new(
        Hex1bColor.FromRgb(72, 166, 90), Hex1bColor.FromRgb(137, 224, 148),
        Hex1bColor.FromRgb(28, 93, 42), Hex1bColor.FromRgb(12, 39, 18));

    public static ButtonPalette MutedGreen { get; } = new(
        Hex1bColor.FromRgb(56, 83, 62), Hex1bColor.FromRgb(89, 118, 96),
        Hex1bColor.FromRgb(28, 46, 33), Hex1bColor.FromRgb(174, 192, 179));
}

public static class BeveledButton
{
    public const int Height = 3;
    public static int Width(string label) => label.Length + 4;

    public static Hex1bWidget Build<T>(WidgetContext<T> context, string label, ButtonPalette palette, Action activate)
        where T : Hex1bWidget =>
        context.Interactable(button => button.Surface(layers =>
            [layers.Layer(surface => Draw(surface, label, palette))])
            .FixedWidth(Width(label)).FixedHeight(Height))
            .OnClick(_ => activate()).FixedWidth(Width(label)).FixedHeight(Height);

    private static void Draw(Surface surface, string label, ButtonPalette palette)
    {
        for (var y = 0; y < surface.Height; y++)
        {
            for (var x = 0; x < surface.Width; x++)
            {
                var upper = y == 0 || x == 0 ? palette.Highlight
                    : x == surface.Width - 1 ? palette.Shadow : palette.Face;
                var lower = y == Height - 1 || x == surface.Width - 1 ? palette.Shadow
                    : x == 0 ? palette.Highlight : palette.Face;
                surface[x, y] = y == 1 && x >= 2 && x < 2 + label.Length
                    ? new SurfaceCell(label[x - 2].ToString(), palette.Ink, palette.Face)
                    : new SurfaceCell("▀", upper, lower);
            }
        }
    }
}
