using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class GameHelp
{
    public static Hex1bWidget Build(WidgetContext<VStackWidget> context) => context.VStack(v =>
    [
        v.Text("A-Z: type | Backspace: erase | Enter: submit"),
        v.Text("Ctrl+R: new game | Ctrl+C: quit | PgUp/PgDn: scroll")
    ]);
}
