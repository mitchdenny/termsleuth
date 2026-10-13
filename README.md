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
Routine instructions, the textual guess counter, and the redundant win message
are omitted; input errors and the answer after a loss still appear below the footer.
A beige, beveled `SUBMIT ↵` button appears beside a full active row. Click it or
press Enter to submit. Equal invisible space on both sides keeps the tiles
centered and stationary as the button appears and disappears. The label has
one cell of beige padding between each end and the bevel.
Winning fires paced streams of colorful half-block confetti inward from points
at the bottom-left and bottom-right corners: a large opening outburst followed
by progressively smaller pulses over about three seconds. Narrow launch cones
and varied speeds keep the jets coherent rather than forming a rectangular cloud.
Paper-like flutter, lighter gravity, and occasional abrupt increases in drag
randomly slow individual pieces after launch. Confetti flows in front of the
grid and replay button and falls into uneven piles along the bottom.
After the opening bursts, the fans become much weaker and slowly sweep back
and forth along the bottom with gentle fluctuations. They lift existing pieces
from the piles for subtle ongoing movement, without creating new confetti.
Random pieces fade out over 1.5 seconds, with a budget of 1.5 pieces per second.

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

Answers are chosen uniformly from the same embedded English dictionary used
to validate guesses. Unknown words stay in the active row and do not consume
an attempt. This version does not offer a shared daily puzzle.

## Dictionary

`src/WordSleuth/Words/FiveLetterWords.txt` contains 15,921 unique, uppercase
five-letter ASCII words, filtered from the public-domain
[dwyl/english-words](https://github.com/dwyl/english-words) `words_alpha.txt`
at commit `20f5cc9b3f0ccc8ce45d814c532b7c2031bba31c`.
The source is a broad word list, including uncommon and archaic words, rather
than a complete or curated Wordle answer list.
Its [Unlicense](src/WordSleuth/Words/LICENSE.txt) permits redistribution and
embedding. The filtered list is compiled into the binary and loaded once;
playing needs neither a network connection nor an external dictionary file.

To reproduce the filtered resource:

```sh
curl --fail --silent --show-error \
  https://raw.githubusercontent.com/dwyl/english-words/20f5cc9b3f0ccc8ce45d814c532b7c2031bba31c/words_alpha.txt |
  LC_ALL=C awk '{sub(/\r$/, ""); if (length($0) == 5 && $0 ~ /^[a-z]+$/) print toupper($0)}' |
  LC_ALL=C sort -u > src/WordSleuth/Words/FiveLetterWords.txt
```

The filtered file's SHA-256 is
`899a1956b40d2446b310a2ac4225aee0562d011534d28416bb1668353a1a011a`.

## Develop

`Program.cs` only starts the terminal. `GameScreen` composes the screen and owns
input bindings; `GameState` owns guessing and feedback. Classes in `Widgets/`
have concise `Build` methods with explicit rendering inputs: `WordBoard` composes
`WordRow`, `LetterTile`, `ActiveCellIndicator`, and the custom surfaces
`RemainingGuesses` and `EnterButton`,
while `AlphabetGrid` composes `AlphabetRow` and
`AlphabetLetter`. `GameFooter` switches between the alphabet and replay button;
`BeveledButton` shares button geometry and palettes. `GuessStatus` renders
errors and loss results, and
`TileStyle` applies full-cell colors, with separate palettes for the guessing
tiles and muted alphabet.
`GameBackground` uses surface layers to animate the backdrop behind the widgets.
It reuses an in-memory bitmap with two pixel rows per terminal row, projecting
each pair through an upper-half block's foreground and background colors.
The rays drift and pulse at 20 frames per second, including while input is idle,
with color, intensity, speed, and angular turbulence driven by submitted guesses.
`WinConfetti` renders a transparent foreground surface, integrating gravity in
small time steps, scheduling tapered bursts, applying flutter and drag, and
allowing landed pieces to slide into neighboring piles. Localized airflow
transitions from corner jets to quieter moving fans; fading is tracked across
airborne and piled pieces so recycling cannot reset or accelerate the fade.
Particle positions and piles adapt to terminal resizing.

```sh
dotnet test tests/WordSleuth.Tests
```

Tests cover game rules and real Hex1b input/rendering in headless terminals,
including preview wins, replay input, button colors, foreground confetti,
upward trajectories, trailing pulses, piles, quiet fan-driven recirculation,
fade budgets, resizing, and reset using a deterministic clock.
