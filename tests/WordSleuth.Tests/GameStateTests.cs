using WordSleuth;

namespace WordSleuth.Tests;

public class GameStateTests
{
    [Fact]
    public void WinPreviewSubmitsTheAnswerThroughNormalScoringAndPreservesPreviousGuesses()
    {
        var game = new GameState("APPLE");
        Type(game, "WORLD");
        game.Submit();
        Type(game, "ab");
        game.WinForTesting();
        Assert.True(game.IsWon);
        Assert.Equal(2, game.Guesses.Count);
        Assert.Equal("WORLD", game.Guesses[0].Word);
        Assert.Equal("APPLE", game.Guesses[1].Word);
        Assert.All(game.Guesses[1].Feedback, feedback => Assert.Equal(LetterFeedback.Correct, feedback));
        Assert.Empty(game.CurrentGuess);
        Assert.False(game.HasInputError);
        game.WinForTesting();
        Assert.Equal(2, game.Guesses.Count);
        game.Restart();
        Assert.False(game.IsWon);
    }

    [Fact]
    public void TypingNormalizesLettersCapsLengthAndSupportsBackspace()
    {
        var game = new GameState("APPLE");
        game.Backspace();
        game.TypeLetter('1');
        Assert.Empty(game.CurrentGuess);
        Type(game, "apples");
        Assert.Equal("APPLE", game.CurrentGuess);
        game.Backspace();
        Assert.Equal("APPL", game.CurrentGuess);
    }

    [Fact]
    public void IncompleteGuessDoesNotUseAnAttempt()
    {
        var game = new GameState("APPLE");
        Type(game, "app");
        game.Submit();
        Assert.Empty(game.Guesses);
        Assert.Equal("APP", game.CurrentGuess);
        Assert.Equal("Type five letters before submitting.", game.Message);
    }

    [Theory]
    [InlineData("APPLE", "ALLEY", "C P A P A")]
    [InlineData("APPLE", "PAPAL", "P P C A P")]
    [InlineData("APPLE", "PUPPY", "P A C A A")]
    [InlineData("LEVEL", "LEECH", "C C P A A")]
    public void DuplicateLettersAreScoredOnlyAsOftenAsTheyAppear(string answer, string word, string expected)
    {
        var game = new GameState(answer);
        Type(game, word);
        game.Submit();
        Assert.Equal(expected.Split(' ').Select(code => code switch
        {
            "C" => LetterFeedback.Correct,
            "P" => LetterFeedback.Present,
            _ => LetterFeedback.Absent
        }), game.Guesses[0].Feedback);
        Assert.Empty(game.CurrentGuess);
    }

    [Fact]
    public void WinningStopsInputAndRestartClearsTheBoard()
    {
        var game = new GameState("apple");
        Type(game, "apple");
        game.Submit();
        Assert.True(game.IsWon);
        Assert.True(game.IsFinished);
        Assert.All(game.Guesses[0].Feedback, feedback => Assert.Equal(LetterFeedback.Correct, feedback));
        Type(game, "world");
        game.Backspace();
        game.Submit();
        Assert.Single(game.Guesses);
        Assert.Empty(game.CurrentGuess);

        game.Restart();
        Assert.False(game.IsFinished);
        Assert.False(game.IsWon);
        Assert.Empty(game.Guesses);
        Type(game, "world");
        Assert.Equal("WORLD", game.CurrentGuess);
    }

    [Fact]
    public void SixthMissRevealsAnswerAndStopsInput()
    {
        var game = new GameState("APPLE");
        for (var attempt = 0; attempt < GameState.MaxGuesses; attempt++)
        {
            Type(game, "WORLD");
            game.Submit();
        }

        Assert.True(game.IsFinished);
        Assert.False(game.IsWon);
        Assert.Contains("APPLE", game.Message);
        Type(game, "APPLE");
        game.Submit();
        Assert.Equal(GameState.MaxGuesses, game.Guesses.Count);
        Assert.Empty(game.CurrentGuess);
    }

    [Fact]
    public void CorrectSixthGuessStillWins()
    {
        var game = new GameState("APPLE");
        for (var attempt = 0; attempt < GameState.MaxGuesses - 1; attempt++)
        {
            Type(game, "WORLD");
            game.Submit();
        }

        Type(game, "APPLE");
        game.Submit();
        Assert.True(game.IsWon);
        Assert.Contains("Solved in 6", game.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("TOOLONG")]
    [InlineData("AB12E")]
    [InlineData("CAFÉX")]
    [InlineData("ZZZZZ")]
    public void InvalidAnswersAreRejected(string answer)
    {
        Assert.Throws<ArgumentException>(() => new GameState(answer));
    }

    [Fact]
    public void UsedLettersTrackSubmittedGuessesAndKeepTheirBestFeedback()
    {
        var game = new GameState("APPLE");
        Type(game, "ALLEY");
        Assert.Empty(game.UsedLetters);
        game.Submit();
        Assert.Equal(4, game.UsedLetters.Count);
        Assert.Equal(LetterFeedback.Correct, game.UsedLetters['A']);
        Assert.Equal(LetterFeedback.Present, game.UsedLetters['L']);
        Assert.Equal(LetterFeedback.Present, game.UsedLetters['E']);
        Assert.Equal(LetterFeedback.Absent, game.UsedLetters['Y']);

        Type(game, "PAPAL");
        game.Submit();
        Assert.Equal(5, game.UsedLetters.Count);
        Assert.Equal(LetterFeedback.Correct, game.UsedLetters['A']);
        Assert.Equal(LetterFeedback.Correct, game.UsedLetters['P']);

        Type(game, "APPLE");
        game.Submit();
        Assert.Equal(LetterFeedback.Correct, game.UsedLetters['L']);
        Assert.Equal(LetterFeedback.Correct, game.UsedLetters['E']);
        Assert.Equal(LetterFeedback.Absent, game.UsedLetters['Y']);

        game.Restart();
        Assert.Empty(game.UsedLetters);
    }

    [Fact]
    public void EmbeddedDictionaryContainsUniqueFiveLetterAsciiWords()
    {
        Assert.Equal(15921, WordDictionary.Words.Count);
        Assert.Equal(WordDictionary.Words.Count, WordDictionary.Words.Distinct().Count());
        Assert.All(WordDictionary.Words, word =>
        {
            Assert.Equal(5, word.Length);
            Assert.All(word, letter => Assert.True(char.IsAsciiLetterUpper(letter)));
            Assert.True(WordDictionary.Contains(word.ToLowerInvariant()));
        });
        Assert.True(WordDictionary.Contains("APPLE"));
        Assert.False(WordDictionary.Contains("ZZZZZ"));
        Assert.False(WordDictionary.Contains("APPLES"));
    }

    [Fact]
    public void RandomAnswersAndRestartsUseTheAcceptedDictionary()
    {
        var game = new GameState();
        var answers = new HashSet<string>();
        for (var i = 0; i < 100; i++)
        {
            game.WinForTesting();
            Assert.True(game.IsWon);
            var word = Assert.Single(game.Guesses).Word;
            Assert.True(WordDictionary.Contains(word));
            answers.Add(word);
            game.Restart();
        }
        Assert.True(answers.Count > 1);
    }

    [Fact]
    public void UnknownWordsPreserveInputAndAttemptsAndEditingClearsTheError()
    {
        var game = new GameState("APPLE");
        Type(game, "WORLD");
        game.Submit();
        Type(game, "zzzzz");
        game.Submit();
        Assert.True(game.HasInputError);
        Assert.Equal("Not in the English dictionary.", game.Message);
        Assert.Equal("ZZZZZ", game.CurrentGuess);
        Assert.Single(game.Guesses);
        Assert.False(game.IsFinished);
        Assert.False(game.UsedLetters.ContainsKey('Z'));
        game.Backspace();
        Assert.False(game.HasInputError);
        Assert.Equal("ZZZZ", game.CurrentGuess);
        for (var i = 0; i < 4; i++) game.Backspace();
        Type(game, "CRANE");
        game.Submit();
        Assert.False(game.HasInputError);
        Assert.Equal(2, game.Guesses.Count);
        Assert.Empty(game.CurrentGuess);
    }

    private static void Type(GameState game, string text)
    {
        foreach (var letter in text)
        {
            game.TypeLetter(letter);
        }
    }
}
