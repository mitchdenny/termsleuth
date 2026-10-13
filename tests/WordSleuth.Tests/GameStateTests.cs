using WordSleuth;

namespace WordSleuth.Tests;

public class GameStateTests
{
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
    public void InvalidAnswersAreRejected(string answer)
    {
        Assert.Throws<ArgumentException>(() => new GameState(answer));
    }

    private static void Type(GameState game, string text)
    {
        foreach (var letter in text)
        {
            game.TypeLetter(letter);
        }
    }
}
