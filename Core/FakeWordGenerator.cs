using System.Text;

namespace SqlFalsifier.Core;

/// <summary>
/// Produces pronounceable, meaningless lowercase words. Deterministic for a given
/// seed so the same script always yields the same output.
/// </summary>
public sealed class FakeWordGenerator
{
    private static readonly string[] Onsets =
    [
        "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "r", "s", "t", "v", "z",
        "br", "cr", "dr", "fl", "gl", "gr", "kl", "pl", "pr", "sl", "sn", "st", "tr", "vr",
    ];

    private static readonly string[] Nuclei = ["a", "e", "i", "o", "u", "ai", "au", "ou", "ei", "ia"];

    private static readonly string[] Codas = ["", "", "", "n", "r", "l", "s", "m", "k", "t"];

    private readonly Random _random;
    private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);

    public FakeWordGenerator(int seed) => _random = new Random(seed);

    /// <summary>Reserves a word so the generator never produces it again.</summary>
    public void Reserve(string word) => _used.Add(word);

    /// <summary>
    /// Generates a fresh word whose length roughly matches <paramref name="originalLength"/>,
    /// so the falsified script keeps a familiar shape.
    /// </summary>
    public string Next(int originalLength)
    {
        int syllables = originalLength switch
        {
            <= 2 => 1,
            <= 5 => 2,
            <= 9 => 3,
            _ => 4,
        };

        for (int attempt = 0; attempt < 5000; attempt++)
        {
            string candidate = Build(syllables);
            if (SqlKeywords.IsKeyword(candidate) || !_used.Add(candidate)) continue;
            return candidate;
        }

        // Practically unreachable: fall back to a numbered word.
        string fallback = Build(syllables) + _used.Count.ToString();
        _used.Add(fallback);
        return fallback;
    }

    private string Build(int syllables)
    {
        var sb = new StringBuilder();
        for (int s = 0; s < syllables; s++)
        {
            sb.Append(Onsets[_random.Next(Onsets.Length)]);
            sb.Append(Nuclei[_random.Next(Nuclei.Length)]);
            if (s == syllables - 1) sb.Append(Codas[_random.Next(Codas.Length)]);
        }

        // A single-letter original still deserves a short but readable stand-in.
        return sb.ToString();
    }
}
