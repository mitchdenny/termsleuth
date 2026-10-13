# TermSleuth

A small Wordle-like terminal game built with [Hex1b](https://hex1b.dev).
Find a five-letter word in six guesses. The game is centered in the terminal.
Each tile shows a capital letter with two cells of horizontal padding on each
side and one cell above and below. Tiles and guessing rows have one-cell gaps.
Moving blue-green rays light the terminal edges, fading to black around the
game. As guesses run out, the rays turn red, brighten, and become faster and
more turbulent. Restarting restores the calm blue-green background.
The active guessing row has dark gray tiles, including its empty cells. A red
upper-half-block underline marks the next empty cell and follows typing and
Backspace. It disappears when the row is full or the game is finished.
A three-row blue `X remaining` prompt points toward the active row with a
two-column angled tip, using Powerline diagonals U+E0B8/U+E0BC and the narrow
triangular block U+1FB6C at its center. Use a terminal font with Powerline Extra
and Symbols for Legacy Computing support.
It follows the row after submission and disappears when the game ends.
Routine instructions and the textual guess counter are omitted; errors and
the final result still appear below the alphabet or replay button.
A beige, beveled `SUBMIT ↵` button appears beside a full active row. Click it or
press Enter to submit. Equal invisible space on both sides keeps the tiles
centered and stationary as the button appears and disappears. The label has
one cell of beige padding between each end and the bevel.
Winning fires a large burst of colorful half-block confetti inward from points
at the bottom-left and bottom-right corners, with varied trajectories and
velocities. It flows in front of the grid, falls under gravity, and settles
into persistent uneven piles along the bottom. Replay controls and the result
remain unobscured.

## Run

Install the .NET 10 SDK and the [Aspire CLI](https://aspire.dev/get-started/install-cli/).
From the repository root:

```sh
aspire run
```

Open the `wordsleuth` resource's terminal in the Aspire dashboard. The root
`apphost.cs` is a file-based C# AppHost (no AppHost `.csproj`), using the pinned
Aspire daily build for `AddDotnetProject(...)` and experimental `WithTerminal(...)`.
The terminal starts at 80 columns by 60 rows.

You can also run the game directly:

```sh
dotnet run --project src/WordSleuth
```

## Play

Type A-Z to fill the current row, Backspace to erase, and Enter to submit.
Green tiles are correct, yellow tiles are in the wrong position,
and gray tiles are absent. Letters stay white over
the colored tile backgrounds. Duplicate letters are scored only as many times
as they occur in the answer. Ctrl+R starts a new game; Ctrl+C quits.
F9 submits the answer to preview a win from any unfinished game.
After a win or loss, the alphabet is replaced by a beveled `PLAY AGAIN ↵`
button: bright green for a win, muted green for a loss. Click it or press Enter
to start again. Restart clears the confetti, including any settled piles.
All A-Z letters appear alphabetically one blank row below the grid, without
feedback symbols. Their colors are darker and more muted than the guessing
tiles: unused letters are blue-gray, correct letters green, and misplaced
letters yellow. Letters ruled out by submitted guesses are strongly faded.
Submitted letters use their best result so far.
Starting a new game resets all letters to unused.
Use at least 62 columns and 30 rows to fit the board, footer, and status.

This initial version chooses randomly from a small built-in answer list and
accepts any five ASCII letters as a guess; it does not yet enforce a dictionary
or offer a shared daily puzzle.

## Develop

`Program.cs` only starts the terminal. `GameScreen` composes the screen and owns
input bindings; `GameState` owns guessing and feedback. Classes in `Widgets/`
have concise `Build` methods with explicit rendering inputs: `WordBoard` composes
`WordRow`, `LetterTile`, `ActiveCellIndicator`, and the custom surfaces
`RemainingGuesses` and `EnterButton`,
while `AlphabetGrid` composes `AlphabetRow` and
`AlphabetLetter`. `GameFooter` switches between the alphabet and replay button;
`BeveledButton` shares button geometry and palettes. `GuessStatus` renders
errors and final results, and
`TileStyle` applies full-cell colors, with separate palettes for the guessing
tiles and muted alphabet.
`GameBackground` uses surface layers to animate the backdrop behind the widgets.
It reuses an in-memory bitmap with two pixel rows per terminal row, projecting
each pair through an upper-half block's foreground and background colors.
The rays drift and pulse at 20 frames per second, including while input is idle,
with color, intensity, speed, and angular turbulence driven by submitted guesses.
`WinConfetti` renders a transparent foreground surface, integrating gravity in
small time steps and allowing landed pieces to slide into neighboring piles.
Particle positions and piles adapt to terminal resizing.

```sh
dotnet test tests/WordSleuth.Tests
```

Tests cover game rules and real Hex1b input/rendering in headless terminals,
including preview wins, replay input, button colors, foreground confetti,
upward trajectories, settled piles, resizing, and reset using a deterministic clock.
