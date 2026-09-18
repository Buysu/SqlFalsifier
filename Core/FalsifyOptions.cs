namespace SqlFalsifier.Core;

public sealed class FalsifyOptions
{
    public int Seed { get; set; } = 20260806;

    /// <summary>Split PascalCase / snake_case names and map each word individually.</summary>
    public bool SplitCompoundNames { get; set; } = true;

    /// <summary>
    /// Words kept verbatim wherever they appear inside an identifier. This is what makes
    /// <c>IdClient</c> become <c>IdBazor</c> instead of a single opaque blob.
    /// </summary>
    public HashSet<string> PreservedWords { get; set; } =
        new(StringComparer.OrdinalIgnoreCase) { "Id", "Fk", "Pk", "Uk" };

    /// <summary>Whole identifiers never renamed - schemas, linked servers, etc.</summary>
    public HashSet<string> PreservedNames { get; set; } =
        new(StringComparer.OrdinalIgnoreCase) { "dbo", "sys", "INFORMATION_SCHEMA", "guest" };

    /// <summary>Rename identifiers directly followed by <c>(</c> (user-defined functions).</summary>
    public bool RenameFunctionNames { get; set; }

    /// <summary>Rename <c>@variables</c> and <c>#temp</c> tables (<c>@@system</c> vars are never touched).</summary>
    public bool RenameVariables { get; set; } = true;

    public static HashSet<string> ParseWordList(string csv) =>
        new(csv.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
}
