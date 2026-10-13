using Hex1b;
using Hex1b.Theming;
using Hex1b.Widgets;

namespace WordSleuth;

public static class ActiveCellIndicator
{
    public static Hex1bWidget Build(WidgetContext<VStackWidget> context, int? column)
    {
        var text = column.HasValue
            ? new string(' ', column.Value * (LetterTile.Width + 1)) + new string('▀', LetterTile.Width)
            : "";
        return context.ThemePanel(theme => theme
                .Set(GlobalTheme.ForegroundColor, Hex1bColor.FromRgb(230, 60, 60))
                .Set(GlobalTheme.BackgroundColor, Hex1bColor.Black),
            v => [v.Text(text.PadRight(WordRow.TilesWidth))]).FixedHeight(1);
    }
}
