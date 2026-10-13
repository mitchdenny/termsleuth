using Hex1b;
using Hex1b.Widgets;

namespace WordSleuth;

public static class GuessStatus
{
    public static Hex1bWidget Build(WidgetContext<VStackWidget> context, string message) => context.VStack(v =>
    [
        v.Text(message).FixedHeight(1),
        v.Text("").FixedHeight(1)
    ]);
}
