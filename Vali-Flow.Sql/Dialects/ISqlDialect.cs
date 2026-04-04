using Vali_Flow.Sql.Builder;

namespace Vali_Flow.Sql.Dialects;

/// <summary>
/// Defines SQL syntax differences between database engines.
/// Implement this interface to support a specific SQL dialect.
/// </summary>
public interface ISqlDialect
{
    /// <summary>The parameter prefix used by this dialect (e.g. "@" for SQL Server, "@" for PostgreSQL).</summary>
    string ParameterPrefix { get; }

    /// <summary>Wraps a column identifier in dialect-specific quotes (e.g. [col], "col", `col`).</summary>
    string QuoteIdentifier(string name);

    /// <summary>
    /// Wraps a table name in dialect-specific quotes.
    /// Defaults to <see cref="QuoteIdentifier"/> — override when table and column quoting differ.
    /// </summary>
    string QuoteTable(string tableName) => QuoteIdentifier(tableName);

    /// <summary>Generates an IS NULL check for the given column expression.</summary>
    string NullCheck(string columnSql);

    /// <summary>Generates an IS NOT NULL check for the given column expression.</summary>
    string NotNullCheck(string columnSql);

    /// <summary>The LIKE operator keyword (e.g. "LIKE" or "ILIKE" for PostgreSQL).</summary>
    string LikeOperator { get; }

    /// <summary>The case-insensitive LIKE expression. Some dialects use ILIKE, others wrap in LOWER().</summary>
    string ILikeExpression(string columnSql, string parameterName);

    /// <summary>SQL literal for boolean true.</summary>
    string TrueValue { get; }

    /// <summary>SQL literal for boolean false.</summary>
    string FalseValue { get; }

    /// <summary>ORDER BY ascending keyword. Default: "ASC".</summary>
    string OrderByAscending => "ASC";

    /// <summary>ORDER BY descending keyword. Default: "DESC".</summary>
    string OrderByDescending => "DESC";

    /// <summary>
    /// Returns the TOP fragment to embed inside SELECT (e.g. "TOP 20 " for SQL Server when there is no OFFSET).
    /// Returns empty string for dialects that use LIMIT at the end.
    /// </summary>
    string SelectTop(int? take) => string.Empty;

    /// <summary>
    /// Returns the LIMIT / OFFSET clause to append after ORDER BY.
    /// SQL Server (with skip): "OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY"
    /// MySQL / PostgreSQL / SQLite: "LIMIT {take} OFFSET {skip}"
    /// </summary>
    string LimitOffset(int? take, int? skip) => string.Empty;

    /// <summary>
    /// Returns a SQL expression for extracting a named date part from a column expression.
    /// <paramref name="part"/> is one of: YEAR, MONTH, DAY, HOUR, MINUTE, SECOND.
    /// </summary>
    string DatePartExpression(string columnSql, string part);

    // ── TRUNCATE ──────────────────────────────────────────────────────────────

    /// <summary>Returns true if the dialect supports TRUNCATE TABLE. SQLite does not.</summary>
    bool SupportsTruncate => true;

    // ── MERGE ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// True when the dialect supports the SQL MERGE statement
    /// (MERGE INTO target USING source ON ... WHEN MATCHED ... WHEN NOT MATCHED ...).
    /// Only SQL Server returns true.
    /// </summary>
    bool SupportsMerge => false;

    // ── DML output / returning ────────────────────────────────────────────────

    /// <summary>
    /// True when the dialect supports SQL Server-style inline <c>OUTPUT</c> clauses
    /// (<c>OUTPUT INSERTED.*</c>, <c>OUTPUT DELETED.*</c>) placed in the middle of DML statements.
    /// Only <see cref="SqlServerDialect"/> returns true.
    /// </summary>
    bool SupportsInlineOutput => false;

    /// <summary>
    /// True when the dialect supports the ANSI/PostgreSQL <c>RETURNING</c> clause appended at
    /// the end of INSERT / UPDATE / DELETE statements.
    /// PostgreSQL and SQLite return true. MySQL returns true for INSERT only (8.0.21+).
    /// SQL Server does not support it.
    /// </summary>
    bool SupportsReturning => false;

    /// <summary>
    /// The dialect name, used in exception messages when an unsupported DML feature is requested.
    /// </summary>
    string DialectName { get; }

    // ── INSERT conflict resolution ────────────────────────────────────────────

    /// <summary>
    /// Prefix word(s) inserted between INSERT and INTO for conflict resolution
    /// (e.g. "OR IGNORE", "OR REPLACE" for SQLite, "IGNORE" for MySQL).
    /// Empty string if not applicable.
    /// </summary>
    string InsertConflictPrefix => string.Empty;

    /// <summary>
    /// True when the dialect supports the <c>ON CONFLICT (...) DO NOTHING</c> /
    /// <c>ON CONFLICT (...) DO UPDATE SET</c> syntax (PostgreSQL and SQLite).
    /// </summary>
    bool SupportsOnConflict => false;

    /// <summary>
    /// True when the dialect supports the MySQL <c>ON DUPLICATE KEY UPDATE</c> syntax.
    /// </summary>
    bool SupportsOnDuplicateKey => false;

    // ── String / Math / Date function expressions ─────────────────────────────

    /// <summary>
    /// Returns a SUBSTRING expression. C# passes a 1-based start already adjusted.
    /// SQLite overrides to emit SUBSTR instead of SUBSTRING.
    /// </summary>
    string SubstringExpression(string column, string start, string? length = null)
        => length != null ? $"SUBSTRING({column}, {start}, {length})" : $"SUBSTRING({column}, {start})";

    /// <summary>
    /// Returns a whitespace TRIM expression.
    /// Default (SQL Server): LTRIM(RTRIM(column)).  Others: TRIM(column).
    /// </summary>
    string TrimExpression(string column) => $"LTRIM(RTRIM({column}))";

    /// <summary>
    /// Returns the string-length expression.
    /// Default (SQL Server): LEN(column).  Others: LENGTH(column).
    /// </summary>
    string StringLengthExpression(string column) => $"LEN({column})";

    /// <summary>
    /// Returns a 0-based index-of expression.
    /// Default (SQL Server): CHARINDEX(search, column) - 1.
    /// PostgreSQL: POSITION(search IN column) - 1.
    /// MySQL: LOCATE(search, column) - 1.
    /// SQLite: INSTR(column, search) - 1.
    /// </summary>
    string IndexOfExpression(string searchParam, string column)
        => $"CHARINDEX({searchParam}, {column}) - 1";

    /// <summary>
    /// Returns a date-add expression for the given part (DAY/MONTH/YEAR).
    /// <paramref name="amount"/> is a pre-evaluated integer literal (e.g. "5").
    /// Default (SQL Server): DATEADD(part, amount, column).
    /// </summary>
    string DateAddExpression(string datePart, string amount, string column)
        => $"DATEADD({datePart}, {amount}, {column})";

    /// <summary>
    /// Returns the ceiling expression.
    /// Default (SQL Server): CEILING(column).  Others: CEIL(column).
    /// </summary>
    string CeilingExpression(string column) => $"CEILING({column})";

    /// <summary>
    /// Returns expression that evaluates to true when column is NULL or contains only whitespace.
    /// Default (SQL Server / SQLite): ({column} IS NULL OR LTRIM(RTRIM({column})) = '').
    /// PostgreSQL / MySQL override to use TRIM.
    /// </summary>
    string IsNullOrWhitespaceExpression(string column)
        => $"({column} IS NULL OR LTRIM(RTRIM({column})) = '')";

    // ── Utility expressions ───────────────────────────────────────────────────

    /// <summary>Returns CAST expression: CAST(columnSql AS typeName).</summary>
    string CastExpression(string columnSql, string typeName)
        => $"CAST({columnSql} AS {typeName})";

    /// <summary>Returns COALESCE expression: COALESCE(columnSql, fallbackSql).</summary>
    string CoalesceExpression(string columnSql, string fallbackSql)
        => $"COALESCE({columnSql}, {fallbackSql})";

    /// <summary>
    /// Returns string concatenation expression.
    /// SQL Server: a + b + c | PostgreSQL/SQLite: a || b || c | MySQL: CONCAT(a,b,c)
    /// </summary>
    string ConcatExpression(params string[] parts) => string.Join(" || ", parts);

    /// <summary>
    /// Escapes special LIKE characters in a user-supplied value.
    /// Default: replaces % with \%, _ with \_.
    /// SQL Server also escapes [ with \[.
    /// </summary>
    string EscapeLikeValue(string value)
        => value.Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>SQL expression for the current timestamp. GETDATE() vs NOW() vs CURRENT_TIMESTAMP.</summary>
    string CurrentTimestamp { get; }

    // ── NULLS ordering ────────────────────────────────────────────────────────

    /// <summary>True when the dialect natively supports NULLS FIRST / NULLS LAST in ORDER BY.</summary>
    bool SupportsNullsOrdering => false;

    /// <summary>Returns "NULLS FIRST", "NULLS LAST", or "" (when not supported or Default).</summary>
    string NullsOrderClause(NullsOrder nulls) => string.Empty;

    // ── Row locking ───────────────────────────────────────────────────────────

    /// <summary>The locking clause appended at end of SELECT for exclusive row locking.</summary>
    string ForUpdateClause => string.Empty;

    /// <summary>The locking clause appended at end of SELECT for shared row locking.</summary>
    string ForShareClause => string.Empty;

    // ── IS DISTINCT FROM ─────────────────────────────────────────────────────

    /// <summary>True when the dialect natively supports IS DISTINCT FROM / IS NOT DISTINCT FROM.</summary>
    bool SupportsIsDistinctFrom => false;

    /// <summary>
    /// Returns IS DISTINCT FROM expression.
    /// PostgreSQL/SQLite: "col IS DISTINCT FROM param"
    /// SQL Server/MySQL fallback: "(col &lt;&gt; param OR col IS NULL)"
    /// </summary>
    string IsDistinctFromExpression(string colSql, string paramSql)
        => $"({colSql} <> {paramSql} OR {colSql} IS NULL)";

    /// <summary>
    /// Returns IS NOT DISTINCT FROM expression.
    /// PostgreSQL/SQLite: "col IS NOT DISTINCT FROM param"
    /// SQL Server/MySQL fallback: "(col = param OR (col IS NULL AND param IS NULL))"
    /// </summary>
    string IsNotDistinctFromExpression(string colSql, string paramSql)
        => $"({colSql} = {paramSql} OR ({colSql} IS NULL AND {paramSql} IS NULL))";

    // ── Recursive CTE ────────────────────────────────────────────────────────

    /// <summary>
    /// The keyword inserted after WITH for recursive CTEs.
    /// SQL Server: "" (no keyword needed)
    /// PostgreSQL, SQLite, MySQL 8.0+: "RECURSIVE"
    /// </summary>
    string RecursiveCteKeyword => string.Empty;

    // ── UPDATE FROM / JOIN ────────────────────────────────────────────────────

    /// <summary>
    /// True when the dialect supports UPDATE…FROM or UPDATE…JOIN syntax.
    /// SQL Server: <c>UPDATE t SET … FROM source [JOIN …]</c>
    /// PostgreSQL: <c>UPDATE t SET … FROM source WHERE …</c>
    /// MySQL: <c>UPDATE t JOIN source ON … SET …</c>
    /// SQLite and Oracle do not support this syntax.
    /// </summary>
    bool SupportsUpdateFrom => false;

    /// <summary>
    /// When true the JOIN clause must appear before the SET clause (MySQL syntax).
    /// When false (default) the FROM clause appears after SET (SQL Server / PostgreSQL).
    /// </summary>
    bool UpdateJoinBeforeSet => false;

    /// <summary>
    /// Returns the FROM fragment to append after SET in UPDATE statements.
    /// Default: <c>FROM {tableSql}</c> (SQL Server / PostgreSQL).
    /// MySQL overrides this to return empty string because it places JOIN before SET.
    /// </summary>
    string UpdateFromClause(string tableSql) => $"FROM {tableSql}";
}
