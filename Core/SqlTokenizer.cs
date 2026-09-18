using System.Text;

namespace SqlFalsifier.Core;

/// <summary>
/// Lightweight T-SQL lexer. It is deliberately not a parser: it only needs to tell
/// identifiers apart from literals, comments and operators so that names can be
/// swapped without touching anything else in the script.
/// </summary>
public static class SqlTokenizer
{
    public static List<SqlToken> Tokenize(string sql)
    {
        var tokens = new List<SqlToken>();
        int i = 0;

        while (i < sql.Length)
        {
            char c = sql[i];

            // ---- whitespace ------------------------------------------------------
            if (char.IsWhiteSpace(c))
            {
                int start = i;
                while (i < sql.Length && char.IsWhiteSpace(sql[i])) i++;
                tokens.Add(new SqlToken { Kind = SqlTokenKind.Whitespace, Raw = sql[start..i] });
                continue;
            }

            // ---- line comment ----------------------------------------------------
            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                int start = i;
                while (i < sql.Length && sql[i] != '\n') i++;
                tokens.Add(new SqlToken { Kind = SqlTokenKind.LineComment, Raw = sql[start..i] });
                continue;
            }

            // ---- block comment (nestable in T-SQL) -------------------------------
            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                int start = i;
                int depth = 0;
                while (i < sql.Length)
                {
                    if (sql[i] == '/' && i + 1 < sql.Length && sql[i + 1] == '*') { depth++; i += 2; }
                    else if (sql[i] == '*' && i + 1 < sql.Length && sql[i + 1] == '/')
                    {
                        depth--; i += 2;
                        if (depth == 0) break;
                    }
                    else i++;
                }
                tokens.Add(new SqlToken { Kind = SqlTokenKind.BlockComment, Raw = sql[start..i] });
                continue;
            }

            // ---- string literal, with optional N prefix --------------------------
            if (c == '\'' || ((c is 'N' or 'n') && i + 1 < sql.Length && sql[i + 1] == '\''))
            {
                int start = i;
                if (c != '\'') i++;              // skip the N
                i++;                             // skip the opening quote
                while (i < sql.Length)
                {
                    if (sql[i] == '\'')
                    {
                        if (i + 1 < sql.Length && sql[i + 1] == '\'') { i += 2; continue; }
                        i++;
                        break;
                    }
                    i++;
                }
                tokens.Add(new SqlToken { Kind = SqlTokenKind.StringLiteral, Raw = sql[start..i] });
                continue;
            }

            // ---- quoted identifier: [name], "name", `name` -----------------------
            if (c is '[' or '"' or '`')
            {
                char open = c;
                char close = open == '[' ? ']' : open;
                int start = i;
                i++;
                var body = new StringBuilder();
                bool closed = false;
                while (i < sql.Length)
                {
                    if (sql[i] == close)
                    {
                        if (i + 1 < sql.Length && sql[i + 1] == close) { body.Append(close); i += 2; continue; }
                        i++;
                        closed = true;
                        break;
                    }
                    body.Append(sql[i]);
                    i++;
                }

                if (closed)
                {
                    tokens.Add(new SqlToken
                    {
                        Kind = SqlTokenKind.QuotedIdentifier,
                        Raw = sql[start..i],
                        Body = body.ToString(),
                        OpenQuote = open,
                    });
                }
                else
                {
                    // Unterminated - leave the remainder untouched rather than corrupting it.
                    tokens.Add(new SqlToken { Kind = SqlTokenKind.Operator, Raw = sql[start..i] });
                }
                continue;
            }

            // ---- variables (@x, @@x) and temp tables (#x, ##x) -------------------
            if (c is '@' or '#')
            {
                int start = i;
                while (i < sql.Length && (sql[i] == '@' || sql[i] == '#')) i++;
                string prefix = sql[start..i];
                int nameStart = i;
                while (i < sql.Length && IsIdentifierPart(sql[i])) i++;

                if (i == nameStart)
                {
                    tokens.Add(new SqlToken { Kind = SqlTokenKind.Operator, Raw = prefix });
                }
                else
                {
                    tokens.Add(new SqlToken
                    {
                        Kind = SqlTokenKind.Variable,
                        Raw = sql[start..i],
                        Prefix = prefix,
                        Body = sql[nameStart..i],
                    });
                }
                continue;
            }

            // ---- number ----------------------------------------------------------
            if (char.IsDigit(c) || (c == '.' && i + 1 < sql.Length && char.IsDigit(sql[i + 1])))
            {
                int start = i;
                while (i < sql.Length && (char.IsDigit(sql[i]) || sql[i] == '.')) i++;
                if (i < sql.Length && (sql[i] is 'e' or 'E'))
                {
                    i++;
                    if (i < sql.Length && (sql[i] is '+' or '-')) i++;
                    while (i < sql.Length && char.IsDigit(sql[i])) i++;
                }
                tokens.Add(new SqlToken { Kind = SqlTokenKind.Number, Raw = sql[start..i] });
                continue;
            }

            // ---- bare identifier -------------------------------------------------
            if (IsIdentifierStart(c))
            {
                int start = i;
                while (i < sql.Length && IsIdentifierPart(sql[i])) i++;
                string text = sql[start..i];
                tokens.Add(new SqlToken
                {
                    Kind = SqlTokenKind.Identifier,
                    Raw = text,
                    Body = text,
                });
                continue;
            }

            // ---- anything else ---------------------------------------------------
            tokens.Add(new SqlToken { Kind = SqlTokenKind.Operator, Raw = sql[i].ToString() });
            i++;
        }

        return tokens;
    }

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c is '_' or '$';
}
