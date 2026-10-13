namespace WordSleuth;

public enum LetterFeedback
{
    Absent,
    Present,
    Correct
}

public sealed record Guess(string Word, IReadOnlyList<LetterFeedback> Feedback);

public sealed class GameState
{
    public const int WordLength = 5;
    public const int MaxGuesses = 6;

    private static readonly string[] Answers =
    [
        "APPLE", "BEACH", "BIRCH", "BRAIN", "BRAVE", "BRICK", "CHAIR", "CHARM",
        "CLOUD", "CRANE", "DREAM", "EARTH", "FIELD", "FLAME", "FRESH", "GHOST",
        "GRAPE", "GREEN", "HEART", "HOUSE", "LIGHT", "MANGO", "MOUSE", "NIGHT",
        "OCEAN", "PAINT", "PEACH", "PLANT", "PRIDE", "QUIET", "RIVER", "SHARE",
        "SHINE", "SLATE", "SMILE", "SOLAR", "SOUND", "SPARK", "STONE", "STORM",
        "SUGAR", "TABLE", "TIGER", "TRACE", "TRAIN", "WATER", "WHALE", "WORLD"
    ];

    private readonly List<Guess> guesses = [];
    private string answer;

    public GameState(string? answer = null)
    {
        this.answer = answer is null ? ChooseAnswer() : NormalizeAnswer(answer);
        Guesses = guesses.AsReadOnly();
    }

    public IReadOnlyList<Guess> Guesses { get; }
    public string CurrentGuess { get; private set; } = "";
    public string Message { get; private set; } = "Find the five-letter word in six guesses.";
    public bool IsWon { get; private set; }
    public bool IsFinished => IsWon || guesses.Count == MaxGuesses;

    public void TypeLetter(char letter)
    {
        if (IsFinished || CurrentGuess.Length == WordLength)
        {
            return;
        }

        if (!char.IsAsciiLetter(letter))
        {
            Message = "Use letters A-Z.";
            return;
        }

        CurrentGuess += char.ToUpperInvariant(letter);
        Message = "Enter to submit; Backspace to edit.";
    }

    public void Backspace()
    {
        if (!IsFinished && CurrentGuess.Length > 0)
        {
            CurrentGuess = CurrentGuess[..^1];
            Message = "Enter to submit; Backspace to edit.";
        }
    }

    public void Submit()
    {
        if (IsFinished)
        {
            return;
        }

        if (CurrentGuess.Length != WordLength)
        {
            Message = "Type five letters before submitting.";
            return;
        }

        var feedback = Score(CurrentGuess);
        guesses.Add(new Guess(CurrentGuess, Array.AsReadOnly(feedback)));
        IsWon = CurrentGuess == answer;
        CurrentGuess = "";
        Message = IsWon
            ? $"Solved in {guesses.Count} of {MaxGuesses}! Ctrl+R for a new game."
            : IsFinished
                ? $"The word was {answer}. Ctrl+R for a new game."
                : $"{MaxGuesses - guesses.Count} guesses left.";
    }

    public void Restart()
    {
        answer = ChooseAnswer();
        guesses.Clear();
        CurrentGuess = "";
        IsWon = false;
        Message = "Find the five-letter word in six guesses.";
    }

    private LetterFeedback[] Score(string word)
    {
        var feedback = new LetterFeedback[WordLength];
        var remaining = new int[26];

        // Reserve exact matches before assigning misplaced duplicate letters.
        for (var i = 0; i < WordLength; i++)
        {
            if (word[i] == answer[i])
            {
                feedback[i] = LetterFeedback.Correct;
            }
            else
            {
                remaining[answer[i] - 'A']++;
            }
        }

        for (var i = 0; i < WordLength; i++)
        {
            if (feedback[i] != LetterFeedback.Correct && remaining[word[i] - 'A'] > 0)
            {
                feedback[i] = LetterFeedback.Present;
                remaining[word[i] - 'A']--;
            }
        }

        return feedback;
    }

    private static string ChooseAnswer() => Answers[Random.Shared.Next(Answers.Length)];

    private static string NormalizeAnswer(string value)
    {
        if (value.Length != WordLength || !value.All(char.IsAsciiLetter))
        {
            throw new ArgumentException("The answer must contain exactly five ASCII letters.", nameof(value));
        }

        return value.ToUpperInvariant();
    }
}
