using System.Text;

namespace SqlFalsifier.Core;

public enum SegmentKind
{
    /// <summary>Run of letters - the only thing that ever gets replaced.</summary>
    Word,
    /// <summary>Run of digits.</summary>
    Digits,
    /// <summary>Anything else (underscores, spaces, dashes...) kept verbatim.</summary>
    Separator,
}

public readonly record struct Segment(SegmentKind Kind, string Text);

/// <summary>
/// Breaks an identifier into the pieces that carry meaning, so that
/// <c>IdClient</c>, <c>Id_Client</c> and <c>CLIENT_ID</c> all expose the same
/// "Client" word and therefore land on the same fake name.
/// </summary>
public static class IdentifierSplitter
{
    /// <param name="splitCompound">
    /// When true, PascalCase / camelCase boundaries also split words
    /// (<c>DateFacture</c> -> <c>Date</c> + <c>Facture</c>).
    /// When false, only the preserved affixes are peeled off.
    /// </param>
    public static List<Segment> Split(string identifier, IReadOnlySet<string> preservedAffixes, bool splitCompound)
    {
        var raw = new List<Segment>();
        int i = 0;

        while (i < identifier.Length)
        {
            char c = identifier[i];
            int start = i;

            if (char.IsLetter(c))
            {
                while (i < identifier.Length && char.IsLetter(identifier[i])) i++;
                raw.Add(new Segment(SegmentKind.Word, identifier[start..i]));
            }
            else if (char.IsDigit(c))
            {
                while (i < identifier.Length && char.IsDigit(identifier[i])) i++;
                raw.Add(new Segment(SegmentKind.Digits, identifier[start..i]));
            }
            else
            {
                while (i < identifier.Length && !char.IsLetterOrDigit(identifier[i])) i++;
                raw.Add(new Segment(SegmentKind.Separator, identifier[start..i]));
            }
        }

        var result = new List<Segment>(raw.Count);
        foreach (var seg in raw)
        {
            if (seg.Kind != SegmentKind.Word)
            {
                result.Add(seg);
                continue;
            }

            var pieces = splitCompound ? SplitCamelCase(seg.Text) : new List<string> { seg.Text };
            foreach (var piece in pieces)
                PeelAffixes(piece, preservedAffixes, result);
        }

        return result;
    }

    /// <summary>
    /// <c>IdClient</c> -> [Id, Client]; <c>IDClient</c> -> [ID, Client];
    /// <c>nomclient</c> -> [nomclient] (no boundary to detect).
    /// </summary>
    private static List<string> SplitCamelCase(string word)
    {
        var parts = new List<string>();
        int start = 0;

        for (int i = 1; i < word.Length; i++)
        {
            bool boundary =
                // lower -> upper : "dateFacture"
                (char.IsLower(word[i - 1]) && char.IsUpper(word[i])) ||
                // UPPER -> Upperlower : "IDClient" splits before the "C"
                (i + 1 < word.Length && char.IsUpper(word[i - 1]) && char.IsUpper(word[i]) && char.IsLower(word[i + 1]));

            if (boundary)
            {
                parts.Add(word[start..i]);
                start = i;
            }
        }

        parts.Add(word[start..]);
        return parts;
    }

    /// <summary>
    /// Peels a preserved affix off the front and/or the back of a word even when
    /// no casing boundary marks it, e.g. <c>IDCLIENT</c> -> [ID, CLIENT].
    /// </summary>
    private static void PeelAffixes(string word, IReadOnlySet<string> affixes, List<Segment> output)
    {
        if (word.Length == 0) return;

        if (affixes.Contains(word))
        {
            output.Add(new Segment(SegmentKind.Word, word));
            return;
        }

        foreach (var affix in affixes)
        {
            if (word.Length > affix.Length && word.StartsWith(affix, StringComparison.OrdinalIgnoreCase))
            {
                output.Add(new Segment(SegmentKind.Word, word[..affix.Length]));
                PeelAffixes(word[affix.Length..], affixes, output);
                return;
            }
        }

        foreach (var affix in affixes)
        {
            if (word.Length > affix.Length && word.EndsWith(affix, StringComparison.OrdinalIgnoreCase))
            {
                PeelAffixes(word[..^affix.Length], affixes, output);
                output.Add(new Segment(SegmentKind.Word, word[^affix.Length..]));
                return;
            }
        }

        output.Add(new Segment(SegmentKind.Word, word));
    }

    /// <summary>Applies the casing style of <paramref name="original"/> to <paramref name="replacement"/>.</summary>
    public static string MatchCasing(string original, string replacement)
    {
        if (replacement.Length == 0) return replacement;

        bool hasUpper = original.Any(char.IsUpper);
        bool hasLower = original.Any(char.IsLower);

        if (hasUpper && !hasLower)
            return replacement.ToUpperInvariant();

        if (!hasUpper)
            return replacement.ToLowerInvariant();

        var sb = new StringBuilder(replacement.ToLowerInvariant());
        sb[0] = char.ToUpperInvariant(sb[0]);
        return sb.ToString();
    }

    public static string Join(IEnumerable<Segment> segments)
    {
        var sb = new StringBuilder();
        foreach (var s in segments) sb.Append(s.Text);
        return sb.ToString();
    }
}
