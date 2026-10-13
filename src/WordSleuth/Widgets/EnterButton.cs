using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class EnterButton
{
    public const int Width = 12;
    private const string Label = "SUBMIT ↵";

    public static Hex1bWidget Build(WidgetContext<HStackWidget> context, bool visible, Action submit)
    {
        if (!visible)
        {
            return context.Text("").FixedWidth(Width).FixedHeight(LetterTile.Height);
        }

        return BeveledButton.Build(context, Label, ButtonPalette.Beige, submit);
    }
}
