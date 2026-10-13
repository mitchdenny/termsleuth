using Hex1b;
using Hex1b.Surfaces;
using Hex1b.Theming;
using Hex1b.Widgets;

namespace WordSleuth;

public static class EnterButton
{
    public const int Width = 11;
    private const string Label = "ENTER ↵";
    private const int LabelLeft = 2;
    private static readonly Hex1bColor Face = Hex1bColor.FromRgb(204, 185, 150);
    private static readonly Hex1bColor Highlight = Hex1bColor.FromRgb(242, 224, 189);
    private static readonly Hex1bColor Shadow = Hex1bColor.FromRgb(130, 111, 80);
    private static readonly Hex1bColor Ink = Hex1bColor.FromRgb(56, 46, 32);

    public static Hex1bWidget Build(WidgetContext<HStackWidget> context, bool visible, Action submit)
    {
        if (!visible)
        {
            return context.Text("").FixedWidth(Width).FixedHeight(LetterTile.Height);
        }

        return context.Interactable(button => button.Surface(layers => [layers.Layer(Draw)])
                .FixedWidth(Width).FixedHeight(LetterTile.Height))
            .OnClick(_ => submit()).FixedWidth(Width).FixedHeight(LetterTile.Height);
    }

    private static void Draw(Surface surface)
    {
        for (var y = 0; y < surface.Height; y++)
        {
            for (var x = 0; x < surface.Width; x++)
            {
                var upper = y == 0 || x == 0 ? Highlight : x == Width - 1 ? Shadow : Face;
                var lower = y == LetterTile.Height - 1 || x == Width - 1 ? Shadow : x == 0 ? Highlight : Face;
                surface[x, y] = y == 1 && x >= LabelLeft && x < LabelLeft + Label.Length
                    ? new SurfaceCell(Label[x - LabelLeft].ToString(), Ink, Face)
                    : new SurfaceCell("▀", upper, lower);
            }
        }
    }
}
