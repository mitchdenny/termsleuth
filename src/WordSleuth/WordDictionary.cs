using System.Collections.Frozen;

namespace WordSleuth;

public static class WordDictionary
{
    public static IReadOnlyList<string> Words { get; } = Array.AsReadOnly(Load());
    private static readonly FrozenSet<string> Accepted = Words.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool Contains(string word) => Accepted.Contains(word);

    private static string[] Load()
    {
        using var stream = typeof(WordDictionary).Assembly.GetManifestResourceStream("WordSleuth.FiveLetterWords.txt")
            ?? throw new InvalidOperationException("The embedded five-letter English dictionary is missing.");
        using var reader = new StreamReader(stream);
        var words = new List<string>();
        while (reader.ReadLine() is { } word)
        {
            if (word.Length != GameState.WordLength || !word.All(char.IsAsciiLetterUpper))
            {
                throw new InvalidOperationException($"Invalid embedded dictionary entry: '{word}'.");
            }
            words.Add(word);
        }
        if (words.Count == 0 || words.Distinct(StringComparer.Ordinal).Count() != words.Count)
        {
            throw new InvalidOperationException("The embedded dictionary must be nonempty and contain unique words.");
        }
        return words.ToArray();
    }
}
