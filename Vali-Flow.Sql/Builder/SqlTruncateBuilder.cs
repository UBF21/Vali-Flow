using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;

namespace Vali_Flow.Sql.Builder;

/// <summary>
/// Fluent builder for SQL TRUNCATE TABLE statements.
/// Generates: TRUNCATE TABLE [schema].[table]
/// </summary>
/// <typeparam name="T">The entity type that maps to the target table.</typeparam>
public sealed class SqlTruncateBuilder<T>
{
    private readonly ISqlDialect _dialect;
    private string? _tableName;
    private string? _schema;
    private string? _tag;

    /// <summary>Creates a new TRUNCATE builder using the specified SQL dialect.</summary>
    public SqlTruncateBuilder(ISqlDialect dialect)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    /// <summary>Sets the table to truncate. If not called, uses <c>typeof(T).Name</c>.</summary>
    /// <param name="tableName">The name of the table to truncate.</param>
    /// <param name="schema">Optional schema qualifier (e.g. "dbo").</param>
    public SqlTruncateBuilder<T> Table(string tableName, string? schema = null)
    {
        _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        _schema = schema;
        return this;
    }

    /// <summary>Adds a SQL comment header for traceability.</summary>
    /// <param name="description">A short description embedded as <c>-- description</c> above the statement.</param>
    public SqlTruncateBuilder<T> Tag(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Tag description cannot be null or whitespace.", nameof(description));
        _tag = description;
        return this;
    }

    /// <summary>Builds and returns the <c>TRUNCATE TABLE</c> statement as a <see cref="SqlQueryResult"/>.</summary>
    /// <returns>A <see cref="SqlQueryResult"/> with the SQL string and an empty parameters dictionary.</returns>
    public SqlQueryResult Build()
    {
        if (!_dialect.SupportsTruncate)
            throw new InvalidOperationException(
                $"Dialect '{_dialect.DialectName}' does not support TRUNCATE TABLE. Use DELETE FROM instead.");

        string quotedTable = _dialect.QuoteTable(_tableName ?? typeof(T).Name);
        string tableSql = !string.IsNullOrEmpty(_schema)
            ? $"{_dialect.QuoteTable(_schema)}.{quotedTable}"
            : quotedTable;

        string sql = $"TRUNCATE TABLE {tableSql}";
        string finalSql = _tag != null ? $"-- {_tag}\n{sql}" : sql;

        return new SqlQueryResult(finalSql, new Dictionary<string, object>());
    }
}
