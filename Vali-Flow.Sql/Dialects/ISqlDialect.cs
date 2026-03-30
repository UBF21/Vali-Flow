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
}
