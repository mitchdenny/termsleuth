using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class GameHeader
{
    public static Hex1bWidget Build(WidgetContext<VStackWidget> context) => context.VStack(v =>
    [
        v.Text("TermSleuth"),
        v.Text("= correct (green) | ~ misplaced (yellow) | - absent (gray)")
    ]);
}
