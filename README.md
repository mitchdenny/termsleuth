# TermSleuth

A small Wordle-like terminal game built with [Hex1b](https://hex1b.dev).
Find a five-letter word in six guesses. The game is centered in the terminal.
Each tile shows a capital letter with two cells of horizontal padding on each
side and one cell above and below.

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
All A-Z letters appear alphabetically below the grid. Unused letters have blue
backgrounds and `.` markers; submitted letters use their best result so far.
Starting a new game resets all letters to unused.
Use at least 52 columns and 22 rows to fit the board, alphabet, and status.

This initial version chooses randomly from a small built-in answer list and
accepts any five ASCII letters as a guess; it does not yet enforce a dictionary
or offer a shared daily puzzle.

## Develop

`Program.cs` only starts the terminal. `GameScreen` composes the screen and owns
input bindings; `GameState` owns guessing and feedback. Classes in `Widgets/`
have concise `Build` methods with explicit rendering inputs: `WordBoard` composes
`WordRow` and `LetterTile`, while `AlphabetGrid` composes `AlphabetRow` and
`AlphabetLetter`. `GuessStatus` renders the current guess and status, and
`TileStyle` shares feedback colors and markers between both tile types.

```sh
dotnet test tests/WordSleuth.Tests
```

Tests cover game rules and real Hex1b input/rendering in headless terminals.
