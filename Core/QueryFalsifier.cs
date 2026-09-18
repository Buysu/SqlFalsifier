using System.Text;

namespace SqlFalsifier.Core;

/// <summary>
/// Rewrites the identifiers of a SQL script (SELECT, CREATE TABLE, CREATE VIEW, ...)
/// with stable fake names, leaving literals, comments, keywords and layout untouched.
/// </summary>
public sealed class QueryFalsifier
{
    private readonly FalsifyOptions _options;
    private FakeWordGenerator _generator;

    /// <summary>real word (lowercase) -> fake word (lowercase).</summary>
    private readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Original spelling of each mapped word, for display purposes.</summary>
    private readonly Dictionary<string, string> _originalSpelling = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>How many times each word was substituted during the last run.</summary>
    private Dictionary<string, int> _usage = new(StringComparer.OrdinalIgnoreCase);

    public QueryFalsifier(FalsifyOptions options)
    {
        _options = options;
        _generator = new FakeWordGenerator(options.Seed);
    }

    public IReadOnlyDictionary<string, string> Map => _map;

    public IReadOnlyDictionary<string, int> Usage => _usage;

    public string DisplayNameFor(string key) => _originalSpelling.TryGetValue(key, out var s) ? s : key;

    public void Clear()
    {
        _map.Clear();
        _originalSpelling.Clear();
        _generator = new FakeWordGenerator(_options.Seed);
    }

    /// <summary>Injects (or overrides) a pair, e.g. when the user edits the mapping grid.</summary>
    public void SetMapping(string real, string fake)
    {
        string key = real.ToLowerInvariant();
        _map[key] = fake;
        _originalSpelling.TryAdd(key, real);
        _generator.Reserve(fake);
    }

    public string Falsify(string sql) => Rewrite(sql, MapWord);

    /// <summary>Turns a previously falsified script back into the original one.</summary>
    public string Restore(string sql)
    {
        var reverse = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (real, fake) in _map)
            reverse[fake] = _originalSpelling.TryGetValue(real, out var spelling) ? spelling : real;

        return Rewrite(sql, word => reverse.TryGetValue(word, out var real) ? real : word);
    }

    private string Rewrite(string sql, Func<string, string> wordTransform)
    {
        _usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var tokens = SqlTokenizer.Tokenize(sql);

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!token.IsNameLike) continue;
            if (!ShouldRename(tokens, i)) continue;

            token.Body = TransformIdentifier(token.Body, wordTransform);
        }

        var sb = new StringBuilder(sql.Length);
        foreach (var token in tokens) sb.Append(token.Render());
        return sb.ToString();
    }

    private bool ShouldRename(List<SqlToken> tokens, int index)
    {
        var token = tokens[index];

        if (_options.PreservedNames.Contains(token.Body)) return false;

        if (token.Kind == SqlTokenKind.Variable)
        {
            // @@ROWCOUNT and friends are built-ins, not user names.
            if (!_options.RenameVariables || token.Prefix.Length > 1) return false;
            return true;
        }

        if (token.Kind == SqlTokenKind.QuotedIdentifier) return true;

        var previous = PreviousSignificant(tokens, index);
        bool isMemberAccess = previous is { Kind: SqlTokenKind.Operator, Raw: "." };

        // A keyword is only a name when it sits behind a dot, as in t.[Date] or t.Date.
        if (!isMemberAccess && SqlKeywords.IsKeyword(token.Body)) return false;

        if (!_options.RenameFunctionNames && !isMemberAccess)
        {
            var next = NextSignificant(tokens, index);
            if (next is { Kind: SqlTokenKind.Operator, Raw: "(" }) return false;
        }

        return true;
    }

    private static SqlToken? PreviousSignificant(List<SqlToken> tokens, int index)
    {
        for (int i = index - 1; i >= 0; i--)
            if (tokens[i].IsSignificant) return tokens[i];
        return null;
    }

    private static SqlToken? NextSignificant(List<SqlToken> tokens, int index)
    {
        for (int i = index + 1; i < tokens.Count; i++)
            if (tokens[i].IsSignificant) return tokens[i];
        return null;
    }

    private string TransformIdentifier(string identifier, Func<string, string> wordTransform)
    {
        var segments = IdentifierSplitter.Split(identifier, _options.PreservedWords, _options.SplitCompoundNames);
        var sb = new StringBuilder(identifier.Length);

        foreach (var segment in segments)
        {
            if (segment.Kind != SegmentKind.Word || _options.PreservedWords.Contains(segment.Text))
            {
                sb.Append(segment.Text);
                continue;
            }

            string key = segment.Text.ToLowerInvariant();
            _usage[key] = _usage.GetValueOrDefault(key) + 1;

            string replacement = wordTransform(key);
            sb.Append(IdentifierSplitter.MatchCasing(segment.Text, replacement));
        }

        return sb.ToString();
    }

    private string MapWord(string lowerWord)
    {
        if (_map.TryGetValue(lowerWord, out var existing)) return existing;

        string fake = _generator.Next(lowerWord.Length);
        _map[lowerWord] = fake;
        return fake;
    }

    /// <summary>Records the nicest spelling seen for each word so the grid is readable.</summary>
    public void RememberSpelling(string sql)
    {
        foreach (var token in SqlTokenizer.Tokenize(sql))
        {
            if (!token.IsNameLike) continue;
            foreach (var segment in IdentifierSplitter.Split(token.Body, _options.PreservedWords, _options.SplitCompoundNames))
            {
                if (segment.Kind != SegmentKind.Word) continue;
                string key = segment.Text.ToLowerInvariant();
                if (!_originalSpelling.TryGetValue(key, out var current) || Prettier(segment.Text, current))
                    _originalSpelling[key] = segment.Text;
            }
        }
    }

    // "Client" reads better than "CLIENT" or "client" in the mapping grid.
    private static bool Prettier(string candidate, string current) =>
        char.IsUpper(candidate[0]) && candidate.Skip(1).Any(char.IsLower) &&
        !(char.IsUpper(current[0]) && current.Skip(1).Any(char.IsLower));
}
