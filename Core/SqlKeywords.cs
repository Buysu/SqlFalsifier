namespace SqlFalsifier.Core;

/// <summary>
/// Words that must never be renamed: T-SQL reserved words, data types and the
/// most common built-in functions. Comparison is case-insensitive.
/// </summary>
public static class SqlKeywords
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // --- statements / clauses -------------------------------------------------
        "ADD", "ALL", "ALTER", "AND", "ANY", "AS", "ASC", "AUTHORIZATION", "BACKUP",
        "BEGIN", "BETWEEN", "BREAK", "BROWSE", "BULK", "BY", "CASCADE", "CASE",
        "CHECK", "CHECKPOINT", "CLOSE", "CLUSTERED", "COALESCE", "COLLATE", "COLUMN",
        "COMMIT", "COMPUTE", "CONSTRAINT", "CONTAINS", "CONTAINSTABLE", "CONTINUE",
        "CONVERT", "CREATE", "CROSS", "CURRENT", "CURRENT_DATE", "CURRENT_TIME",
        "CURRENT_TIMESTAMP", "CURRENT_USER", "CURSOR", "DATABASE", "DBCC",
        "DEALLOCATE", "DECLARE", "DEFAULT", "DELETE", "DENY", "DESC", "DISK",
        "DISTINCT", "DISTRIBUTED", "DOUBLE", "DROP", "DUMP", "ELSE", "END",
        "ERRLVL", "ESCAPE", "EXCEPT", "EXEC", "EXECUTE", "EXISTS", "EXIT",
        "EXTERNAL", "FETCH", "FILE", "FILLFACTOR", "FOR", "FOREIGN", "FREETEXT",
        "FREETEXTTABLE", "FROM", "FULL", "FUNCTION", "GOTO", "GRANT", "GROUP",
        "HAVING", "HOLDLOCK", "IDENTITY", "IDENTITYCOL", "IDENTITY_INSERT", "IF",
        "IN", "INDEX", "INNER", "INSERT", "INTERSECT", "INTO", "IS", "JOIN", "KEY",
        "KILL", "LEFT", "LIKE", "LINENO", "LOAD", "MERGE", "NATIONAL", "NOCHECK",
        "NONCLUSTERED", "NOT", "NULL", "NULLIF", "OF", "OFF", "OFFSETS", "ON",
        "OPEN", "OPENDATASOURCE", "OPENQUERY", "OPENROWSET", "OPENXML", "OPTION",
        "OR", "ORDER", "OUTER", "OVER", "PERCENT", "PIVOT", "PLAN", "PRECISION",
        "PRIMARY", "PRINT", "PROC", "PROCEDURE", "PUBLIC", "RAISERROR", "READ",
        "READTEXT", "RECONFIGURE", "REFERENCES", "REPLICATION", "RESTORE",
        "RESTRICT", "RETURN", "REVERT", "REVOKE", "RIGHT", "ROLLBACK", "ROWCOUNT",
        "ROWGUIDCOL", "RULE", "SAVE", "SCHEMA", "SECURITYAUDIT", "SELECT",
        "SEMANTICKEYPHRASETABLE", "SEMANTICSIMILARITYDETAILSTABLE",
        "SEMANTICSIMILARITYTABLE", "SESSION_USER", "SET", "SETUSER", "SHUTDOWN",
        "SOME", "STATISTICS", "SYSTEM_USER", "TABLE", "TABLESAMPLE", "TEXTSIZE",
        "THEN", "TO", "TOP", "TRAN", "TRANSACTION", "TRIGGER", "TRUNCATE",
        "TRY_CONVERT", "TSEQUAL", "UNION", "UNIQUE", "UNPIVOT", "UPDATE",
        "UPDATETEXT", "USE", "USER", "VALUES", "VARYING", "VIEW", "WAITFOR", "WHEN",
        "WHERE", "WHILE", "WITH", "WITHIN", "WRITETEXT",

        // --- windowing / misc -----------------------------------------------------
        "APPLY", "OFFSET", "ROWS", "RANGE", "PARTITION", "PRECEDING", "FOLLOWING",
        "UNBOUNDED", "FIRST", "LAST", "NEXT", "ONLY", "TIES", "OUTPUT", "INSERTED",
        "DELETED", "GO", "SCHEMABINDING", "ENCRYPTION", "VIEW_METADATA", "NOLOCK",
        "MAXDOP", "RECOMPILE", "IGNORE_DUP_KEY", "ONLINE", "PAD_INDEX", "SORT_IN_TEMPDB",
        "STATISTICS_NORECOMPUTE", "DROP_EXISTING", "ALLOW_ROW_LOCKS", "ALLOW_PAGE_LOCKS",
        "DATA_COMPRESSION", "FILESTREAM", "SPARSE", "MASKED", "PERSISTED", "CACHE",
        "SEQUENCE", "INCREMENT", "MINVALUE", "MAXVALUE", "CYCLE", "START", "AT",
        "TIME", "ZONE", "GENERATED", "ALWAYS", "PERIOD", "HIDDEN", "THROW", "TRY",
        "CATCH", "GOTO", "ELSEIF", "LIMIT", "RETURNS", "READONLY", "NOWAIT",

        // --- data types -----------------------------------------------------------
        "BIT", "TINYINT", "SMALLINT", "INT", "INTEGER", "BIGINT", "DECIMAL", "NUMERIC",
        "MONEY", "SMALLMONEY", "FLOAT", "REAL", "DATE", "DATETIME", "DATETIME2",
        "DATETIMEOFFSET", "SMALLDATETIME", "TIMESTAMP", "CHAR", "VARCHAR", "TEXT",
        "NCHAR", "NVARCHAR", "NTEXT", "BINARY", "VARBINARY", "IMAGE", "UNIQUEIDENTIFIER",
        "XML", "SQL_VARIANT", "HIERARCHYID", "GEOMETRY", "GEOGRAPHY", "JSON", "VECTOR",
        "MAX", "CURSOR",

        // --- built-in functions ---------------------------------------------------
        "ABS", "ACOS", "ASIN", "ATAN", "ATN2", "AVG", "CEILING", "CHARINDEX",
        "CHECKSUM", "CHOOSE", "CONCAT", "CONCAT_WS", "COS", "COT", "COUNT",
        "COUNT_BIG", "CUME_DIST", "DATALENGTH", "DATEADD", "DATEDIFF", "DATEFROMPARTS",
        "DATENAME", "DATEPART", "DAY", "DEGREES", "DENSE_RANK", "DIFFERENCE", "EOMONTH",
        "ERROR_LINE", "ERROR_MESSAGE", "ERROR_NUMBER", "ERROR_PROCEDURE",
        "ERROR_SEVERITY", "ERROR_STATE", "EXP", "FIRST_VALUE", "FLOOR", "FORMAT",
        "GETDATE", "GETUTCDATE", "GROUPING", "GROUPING_ID", "IIF", "ISDATE",
        "ISJSON", "ISNULL", "ISNUMERIC", "LAG", "LAST_VALUE", "LEAD", "LEN", "LOG",
        "LOG10", "LOWER", "LTRIM", "MIN", "MONTH", "NCHAR", "NEWID", "NEWSEQUENTIALID",
        "NTILE", "OBJECT_ID", "OBJECT_NAME", "PARSE", "PATINDEX", "PERCENT_RANK",
        "PERCENTILE_CONT", "PERCENTILE_DISC", "PI", "POWER", "QUOTENAME", "RADIANS",
        "RAND", "RANK", "REPLACE", "REPLICATE", "REVERSE", "ROUND", "ROW_NUMBER",
        "RTRIM", "SCOPE_IDENTITY", "SIGN", "SIN", "SPACE", "SQRT", "SQUARE", "STDEV",
        "STDEVP", "STR", "STRING_AGG", "STRING_ESCAPE", "STRING_SPLIT", "STUFF",
        "SUBSTRING", "SUM", "SWITCHOFFSET", "SYSDATETIME", "SYSDATETIMEOFFSET",
        "SYSUTCDATETIME", "TODATETIMEOFFSET", "TRIM", "TRY_CAST", "TRY_PARSE",
        "UNICODE", "UPPER", "VAR", "VARP", "YEAR", "CAST", "LEFT", "RIGHT",
        "JSON_VALUE", "JSON_QUERY", "JSON_MODIFY", "OPENJSON", "XACT_STATE",
        "ASCII", "SOUNDEX", "HASHBYTES", "COMPRESS", "DECOMPRESS", "SESSIONPROPERTY",
    };

    public static bool IsKeyword(string word) => All.Contains(word);
}
