# TermSleuth

A small Wordle-like terminal game built with [Hex1b](https://hex1b.dev).
Find a five-letter word in six guesses. Each letter is its own FIGlet widget,
arranged horizontally in a guessing row.

## Run

Install the .NET 10 SDK and the [Aspire CLI](https://aspire.dev/get-started/install-cli/).
From the repository root:

```sh
aspire run
```

Open the `wordsleuth` resource's terminal in the Aspire dashboard. The root
`apphost.cs` is a file-based C# AppHost (no AppHost `.csproj`), using the pinned
Aspire daily build for `AddDotnetProject(...)` and experimental `WithTerminal(...)`.
The terminal starts at 80 columns by 60 rows to fit the FIGlet board.

You can also run the game directly:

```sh
dotnet run --project src/WordSleuth
```

## Play

Type A-Z to fill the current row, Backspace to erase, and Enter to submit.
Green `=` tiles are correct, yellow `~` tiles are in the wrong position,
and gray `-` tiles are absent. FIGlet letters and title markers stay white over
the colored tile backgrounds. Duplicate letters are scored only as many times
as they occur in the answer. Ctrl+R starts a new game; Ctrl+C quits.
All A-Z letters appear alphabetically below the grid. Unused letters have blue
backgrounds and `.` markers; submitted letters use their best result so far.
Starting a new game resets all letters to unused.
Page Up / Page Down scroll the board in shorter terminals. Use at least 70
columns; the input row and status remain below the scrollable board.

This initial version chooses randomly from a small built-in answer list and
accepts any five ASCII letters as a guess; it does not yet enforce a dictionary
or offer a shared daily puzzle.

## Develop

`Program.cs` only starts the terminal. `GameScreen` composes the screen and owns
input bindings; `GameState` owns guessing and feedback. Classes in `Widgets/`
have concise `Build` methods with explicit rendering inputs: `WordBoard` composes
`WordRow` and `LetterTile`, while `AlphabetGrid` composes `AlphabetRow` and
`AlphabetLetter`. Header, status, and help are separate components, and
`TileStyle` shares feedback colors and markers between both tile types.

```sh
dotnet test tests/WordSleuth.Tests
```

Tests cover game rules and real Hex1b input/rendering in headless terminals.
