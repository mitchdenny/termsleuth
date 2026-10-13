using Hex1b;
using WordSleuth;

var game = new GameState();
var screen = new GameScreen(game);

await using var terminal = Hex1bTerminal.CreateBuilder()
    .WithHex1bApp(screen.Build)
    .Build();

await terminal.RunAsync();
