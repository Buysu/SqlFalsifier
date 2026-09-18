using System.Text;

namespace SqlFalsifier.Core;

public enum SqlTokenKind
{
    Whitespace,
    LineComment,
    BlockComment,
    StringLiteral,
    Number,
    Identifier,
    QuotedIdentifier,
    Variable,
    Operator,
}

/// <summary>
/// A single lexical unit of the script. Everything that is not an identifier is kept
/// verbatim in <see cref="Raw"/>, so re-rendering the token stream reproduces the
/// original text character for character.
/// </summary>
public sealed class SqlToken
{
    public SqlTokenKind Kind { get; init; }

    /// <summary>Original text, used as-is for every non-identifier token.</summary>
    public string Raw { get; init; } = string.Empty;

    /// <summary>Sigils kept untouched in front of the name: <c>@</c>, <c>@@</c>, <c>#</c>, <c>##</c>.</summary>
    public string Prefix { get; init; } = string.Empty;

    /// <summary>The bare name, without quoting or prefix. This is the only part we rewrite.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Opening quote character for quoted identifiers: <c>[</c>, <c>"</c> or <c>`</c>.</summary>
    public char OpenQuote { get; init; }

    public bool IsSignificant => Kind is not (SqlTokenKind.Whitespace or SqlTokenKind.LineComment or SqlTokenKind.BlockComment);

    public bool IsNameLike => Kind is SqlTokenKind.Identifier or SqlTokenKind.QuotedIdentifier or SqlTokenKind.Variable;

    private static char CloseFor(char open) => open switch
    {
        '[' => ']',
        '"' => '"',
        '`' => '`',
        _ => open,
    };

    public string Render()
    {
        switch (Kind)
        {
            case SqlTokenKind.Identifier:
            case SqlTokenKind.Variable:
                return Prefix + Body;

            case SqlTokenKind.QuotedIdentifier:
            {
                char close = CloseFor(OpenQuote);
                var sb = new StringBuilder();
                sb.Append(Prefix).Append(OpenQuote);
                foreach (char c in Body)
                {
                    if (c == close) sb.Append(close);
                    sb.Append(c);
                }
                sb.Append(close);
                return sb.ToString();
            }

            default:
                return Raw;
        }
    }
}
