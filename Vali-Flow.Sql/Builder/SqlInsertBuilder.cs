using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;

namespace Vali_Flow.Sql.Builder;

/// <summary>
/// Fluent builder for a parameterized SQL INSERT statement.
/// Supports single-row and multi-row inserts, OUTPUT INSERTED (SQL Server),
/// RETURNING (PostgreSQL / SQLite), and conflict-resolution clauses (UPSERT).
/// </summary>
/// <typeparam name="T">The entity type whose properties map to columns.</typeparam>
/// <example>
/// <code>
/// // Single row
/// var result = new SqlInsertBuilder&lt;User&gt;(new SqlServerDialect())
///     .Into("Users")
///     .Set(x => x.Name, "Alice")
///     .Set(x => x.Age, 30)
///     .Build();
/// // INSERT INTO [Users] ([Name], [Age]) VALUES (@pi0, @pi1)
///
/// // Multi-row
/// var result = new SqlInsertBuilder&lt;User&gt;(new SqliteDialect())
///     .Into("Users")
///     .Set(x => x.Name, "Alice").Set(x => x.Age, 30)
///     .NextRow()
///     .Set(x => x.Name, "Bob").Set(x => x.Age, 25)
///     .Build();
/// // INSERT INTO "Users" ("Name", "Age") VALUES (@pi0, @pi1), (@pi2, @pi3)
/// </code>
/// </example>
public sealed class SqlInsertBuilder<T> where T : class
{
    private readonly ISqlDialect _dialect;
    private string? _tableName;
    private string? _schema;

    // Each inner list is one row; first row is seeded at construction.
    private readonly List<List<(string Column, object? Value)>> _rows = new() { new() };
    private List<(string Column, object? Value)> CurrentRow => _rows[^1];

    private bool _outputInserted;
    private bool _returning;
    private readonly List<string> _returningColumns = new();
    private string? _tag;
    private int _paramIndex;

    // ── Conflict-resolution state ─────────────────────────────────────────────

    private bool _insertIgnore;
    private bool _orIgnore;
    private bool _orReplace;
    private bool _onConflictDoNothing;
    private readonly List<string> _conflictKeyColumns = new();
    private readonly List<(string Column, object? Value)> _conflictUpdateAssignments = new();
    private bool _onDuplicateKeyUpdate;
    private readonly List<(string Column, object? Value)> _duplicateKeyAssignments = new();

    // ── INSERT … SELECT state ─────────────────────────────────────────────────

    private SqlQueryResult? _selectQuery;
    private List<string>? _selectColumns;

    /// <summary>Creates a new INSERT builder using the specified SQL dialect.</summary>
    public SqlInsertBuilder(ISqlDialect dialect)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    // ── Table ─────────────────────────────────────────────────────────────────

    /// <summary>Sets the target table name and optional schema.</summary>
    public SqlInsertBuilder<T> Into(string tableName, string? schema = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
        _tableName = tableName;
        _schema = schema;
        return this;
    }

    // ── Column assignments ────────────────────────────────────────────────────

    /// <summary>
    /// Maps a typed column to its insert value for the current row.
    /// <c>.Set(x => x.Name, "Alice")</c> → <c>[Name] = @pi0</c>
    /// </summary>
    public SqlInsertBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        CurrentRow.Add((ExpressionHelper.GetMemberName(column), value));
        return this;
    }

    // ── Multi-row ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts a new row. Subsequent <see cref="Set{TValue}"/> calls populate the new row.
    /// The column order must match the first row.
    /// </summary>
    /// <example>
    /// <code>
    /// builder
    ///     .Set(x => x.Name, "Alice").Set(x => x.Age, 30)
    ///     .NextRow()
    ///     .Set(x => x.Name, "Bob").Set(x => x.Age, 25);
    /// // VALUES (@pi0, @pi1), (@pi2, @pi3)
    /// </code>
    /// </example>
    public SqlInsertBuilder<T> NextRow()
    {
        if (CurrentRow.Count == 0)
            throw new InvalidOperationException("Cannot start a new row before adding at least one SET assignment to the current row.");
        _rows.Add(new List<(string Column, object? Value)>());
        return this;
    }

    // ── SQL Server OUTPUT ─────────────────────────────────────────────────────

    /// <summary>
    /// Appends <c>OUTPUT INSERTED.*</c> before VALUES (SQL Server only).
    /// Useful for retrieving auto-generated identity or computed columns after insert.
    /// </summary>
    public SqlInsertBuilder<T> OutputInserted()
    {
        _outputInserted = true;
        return this;
    }

    // ── RETURNING (PostgreSQL / SQLite) ───────────────────────────────────────

    /// <summary>
    /// Appends <c>RETURNING *</c> (or the specified columns) after VALUES.
    /// Supported by PostgreSQL and SQLite 3.35+. Not valid for SQL Server — use <see cref="OutputInserted"/> instead.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Returning()                       // RETURNING *
    /// builder.Returning(x => x.Id, x => x.Name) // RETURNING "Id", "Name"
    /// </code>
    /// </example>
    public SqlInsertBuilder<T> Returning(params Expression<Func<T, object>>[] columns)
    {
        _returning = true;
        if (columns != null)
            foreach (var col in columns)
                _returningColumns.Add(ExpressionHelper.GetMemberName(col));
        return this;
    }

    // ── Tag ───────────────────────────────────────────────────────────────────

    /// <summary>Labels the query for console tracing and SQL comment.</summary>
    public SqlInsertBuilder<T> Tag(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Tag description cannot be null or whitespace.", nameof(description));
        _tag = description;
        return this;
    }

    // ── Conflict resolution ───────────────────────────────────────────────────

    /// <summary>
    /// SQLite: emits <c>INSERT OR IGNORE INTO ...</c>.
    /// Requires a dialect that supports <c>InsertConflictPrefix</c> or <c>SupportsOnConflict</c>.
    /// </summary>
    public SqlInsertBuilder<T> OrIgnore()
    {
        if (!_dialect.SupportsOnConflict && string.IsNullOrEmpty(_dialect.InsertConflictPrefix))
            throw new InvalidOperationException(
                $"OrIgnore() is not supported by dialect '{_dialect.DialectName}'.");
        _orIgnore = true;
        return this;
    }

    /// <summary>
    /// SQLite: emits <c>INSERT OR REPLACE INTO ...</c>.
    /// Requires a dialect that supports <c>InsertConflictPrefix</c> or <c>SupportsOnConflict</c>.
    /// </summary>
    public SqlInsertBuilder<T> OrReplace()
    {
        if (!_dialect.SupportsOnConflict && string.IsNullOrEmpty(_dialect.InsertConflictPrefix))
            throw new InvalidOperationException(
                $"OrReplace() is not supported by dialect '{_dialect.DialectName}'.");
        _orReplace = true;
        return this;
    }

    /// <summary>
    /// PostgreSQL / SQLite: appends <c>ON CONFLICT DO NOTHING</c> after VALUES.
    /// </summary>
    public SqlInsertBuilder<T> OnConflictDoNothing()
    {
        if (!_dialect.SupportsOnConflict)
            throw new InvalidOperationException(
                $"OnConflictDoNothing() is not supported by dialect '{_dialect.DialectName}'.");
        _onConflictDoNothing = true;
        return this;
    }

    /// <summary>
    /// PostgreSQL / SQLite: appends <c>ON CONFLICT (col1, col2) DO UPDATE SET col = @pu…</c> after VALUES.
    /// </summary>
    /// <param name="conflictKeys">
    /// Action that calls <see cref="AddConflictKey"/> to define the conflict target columns.
    /// </param>
    /// <param name="updateAssignments">
    /// Action that calls <see cref="AddConflictUpdate{TValue}"/> to define the SET assignments.
    /// </param>
    public SqlInsertBuilder<T> OnConflictDoUpdate(
        Action<SqlInsertBuilder<T>> conflictKeys,
        Action<SqlInsertBuilder<T>> updateAssignments)
    {
        if (!_dialect.SupportsOnConflict)
            throw new InvalidOperationException(
                $"OnConflictDoUpdate() is not supported by dialect '{_dialect.DialectName}'.");
        if (conflictKeys == null) throw new ArgumentNullException(nameof(conflictKeys));
        if (updateAssignments == null) throw new ArgumentNullException(nameof(updateAssignments));
        conflictKeys(this);
        updateAssignments(this);
        return this;
    }

    /// <summary>
    /// Registers a column as part of the ON CONFLICT target column list.
    /// Intended for use inside the <c>OnConflictDoUpdate</c> action.
    /// </summary>
    /// <typeparam name="TValue">The type of the column value.</typeparam>
    /// <param name="column">Expression selecting the column to add as a conflict key.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SqlInsertBuilder<T> AddConflictKey<TValue>(Expression<Func<T, TValue>> column)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        _conflictKeyColumns.Add(ExpressionHelper.GetMemberName(column));
        return this;
    }

    /// <summary>
    /// Registers a column = value assignment for the DO UPDATE SET clause.
    /// Intended for use inside the <c>OnConflictDoUpdate</c> action.
    /// </summary>
    /// <typeparam name="TValue">The type of the column value.</typeparam>
    /// <param name="column">Expression selecting the column to update.</param>
    /// <param name="value">The new value for the column.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SqlInsertBuilder<T> AddConflictUpdate<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        _conflictUpdateAssignments.Add((ExpressionHelper.GetMemberName(column), value));
        return this;
    }

    /// <summary>
    /// MySQL: emits <c>INSERT IGNORE INTO ...</c>.
    /// </summary>
    public SqlInsertBuilder<T> InsertIgnore()
    {
        if (!_dialect.SupportsOnDuplicateKey)
            throw new InvalidOperationException(
                $"InsertIgnore() is not supported by dialect '{_dialect.DialectName}'.");
        _onDuplicateKeyUpdate = false;
        _insertIgnore = true;
        return this;
    }

    /// <summary>
    /// MySQL: appends <c>ON DUPLICATE KEY UPDATE col = @pu…</c> after VALUES.
    /// </summary>
    /// <param name="configure">
    /// Action that calls <see cref="AddDuplicateKeyAssignment{TValue}"/> to define SET assignments.
    /// </param>
    public SqlInsertBuilder<T> OnDuplicateKeyUpdate(Action<SqlInsertBuilder<T>> configure)
    {
        if (!_dialect.SupportsOnDuplicateKey)
            throw new InvalidOperationException(
                $"OnDuplicateKeyUpdate() is not supported by dialect '{_dialect.DialectName}'.");
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        _onDuplicateKeyUpdate = true;
        configure(this);
        return this;
    }

    /// <summary>
    /// Registers a column = value assignment for the ON DUPLICATE KEY UPDATE clause.
    /// Intended for use inside the <c>OnDuplicateKeyUpdate</c> action.
    /// </summary>
    /// <typeparam name="TValue">The type of the column value.</typeparam>
    /// <param name="column">Expression selecting the column to update.</param>
    /// <param name="value">The new value for the column.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SqlInsertBuilder<T> AddDuplicateKeyAssignment<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        _duplicateKeyAssignments.Add((ExpressionHelper.GetMemberName(column), value));
        return this;
    }

    // ── INSERT … SELECT ───────────────────────────────────────────────────────

    /// <summary>
    /// Specifies a SELECT query whose results will be inserted into the table.
    /// Mutually exclusive with <see cref="Set{TValue}"/> — use one or the other.
    /// </summary>
    /// <example>
    /// <code>
    /// var select = new SqlQueryBuilder&lt;TestUser&gt;(dialect)
    ///     .From("Archive")
    ///     .Select(x => x.Name)
    ///     .Build();
    ///
    /// var result = new SqlInsertBuilder&lt;TestUser&gt;(dialect)
    ///     .Into("Users")
    ///     .Columns(x => x.Name)
    ///     .SelectFrom(select)
    ///     .Build();
    /// // INSERT INTO [Users] ([Name]) SELECT [Name] FROM [Archive]
    /// </code>
    /// </example>
    public SqlInsertBuilder<T> SelectFrom(SqlQueryResult selectQuery)
    {
        ArgumentNullException.ThrowIfNull(selectQuery);
        if (_rows[0].Count > 0)
            throw new InvalidOperationException(
                "Cannot combine Set() with SelectFrom(). Use one or the other.");
        _selectQuery = selectQuery;
        return this;
    }

    /// <summary>
    /// Specifies the target columns for the INSERT when using <see cref="SelectFrom"/>.
    /// If not called the column list is omitted: <c>INSERT INTO table SELECT …</c>
    /// </summary>
    public SqlInsertBuilder<T> Columns(params Expression<Func<T, object>>[] columns)
    {
        if (columns == null || columns.Length == 0)
            throw new ArgumentException("At least one column must be specified.", nameof(columns));
        _selectColumns = columns
            .Select(c => _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(c)))
            .ToList();
        return this;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>Assembles and returns the final <see cref="SqlQueryResult"/>.</summary>
    public SqlQueryResult Build()
    {
        _paramIndex = 0;

        // INSERT … SELECT mode — bypass VALUES logic entirely
        if (_selectQuery != null)
            return BuildInsertSelect();

        if (_onConflictDoNothing && _conflictUpdateAssignments.Count > 0)
            throw new InvalidOperationException(
                "OnConflictDoNothing and OnConflictDoUpdate are mutually exclusive.");

        if (_conflictUpdateAssignments.Count > 0 && _onDuplicateKeyUpdate)
            throw new InvalidOperationException(
                "OnConflictDoUpdate and OnDuplicateKeyUpdate are mutually exclusive. Use only one.");

        var firstRow = _rows[0];
        if (firstRow.Count == 0)
            throw new InvalidOperationException("At least one column assignment is required for INSERT.");

        // Validate all rows have same column count
        foreach (var row in _rows)
            if (row.Count != firstRow.Count)
                throw new InvalidOperationException(
                    $"All rows must have the same number of columns. First row has {firstRow.Count}, but another row has {row.Count}.");

        if (_outputInserted && !_dialect.SupportsInlineOutput)
            throw new InvalidOperationException(
                $"OutputInserted() requires SqlServerDialect. Current dialect is '{_dialect.DialectName}'. Use Returning() instead.");

        if (_returning && !_dialect.SupportsReturning)
            throw new InvalidOperationException(
                $"Returning() is not supported by '{_dialect.DialectName}'. Use OutputInserted() for SQL Server.");

        var parameters = new Dictionary<string, object>();
        var columns = firstRow.Select(a => _dialect.QuoteIdentifier(a.Column)).ToList();
        var valueSets = new List<string>();

        foreach (var row in _rows)
        {
            var paramNames = new List<string>();
            foreach (var (_, value) in row)
            {
                string paramName = $"pi{_paramIndex++}";
                paramNames.Add($"{_dialect.ParameterPrefix}{paramName}");
                parameters[paramName] = value ?? DBNull.Value;
            }
            valueSets.Add($"({string.Join(", ", paramNames)})");
        }

        string tableSql = BuildTableSql();
        string colList = string.Join(", ", columns);
        string valList = string.Join(", ", valueSets);

        // Determine INSERT keyword prefix
        string insertKeyword = BuildInsertKeyword();

        var sb = new System.Text.StringBuilder();
        sb.Append($"{insertKeyword}{tableSql} ({colList})");

        if (_outputInserted)
            sb.Append(" OUTPUT INSERTED.*");

        sb.Append($" VALUES {valList}");

        // ON CONFLICT DO NOTHING
        if (_onConflictDoNothing)
        {
            sb.Append(" ON CONFLICT DO NOTHING");
        }

        // ON CONFLICT (...) DO UPDATE SET ...
        if (_conflictUpdateAssignments.Count > 0)
        {
            int puIdx = parameters.Count;
            var setClauses = new List<string>();
            foreach (var (col, value) in _conflictUpdateAssignments)
            {
                string paramName = $"pu{puIdx++}";
                setClauses.Add($"{_dialect.QuoteIdentifier(col)} = {_dialect.ParameterPrefix}{paramName}");
                parameters[paramName] = value ?? DBNull.Value;
            }

            string conflictCols = _conflictKeyColumns.Count > 0
                ? $"({string.Join(", ", _conflictKeyColumns.Select(c => _dialect.QuoteIdentifier(c)))})"
                : string.Empty;

            string conflictColsSep = _conflictKeyColumns.Count > 0 ? $"{conflictCols} " : string.Empty;
            sb.Append($" ON CONFLICT {conflictColsSep}DO UPDATE SET {string.Join(", ", setClauses)}");
        }

        // ON DUPLICATE KEY UPDATE ...
        if (_onDuplicateKeyUpdate && _duplicateKeyAssignments.Count > 0)
        {
            int puIdx = parameters.Count;
            var setClauses = new List<string>();
            foreach (var (col, value) in _duplicateKeyAssignments)
            {
                string paramName = $"pu{puIdx++}";
                setClauses.Add($"{_dialect.QuoteIdentifier(col)} = {_dialect.ParameterPrefix}{paramName}");
                parameters[paramName] = value ?? DBNull.Value;
            }
            sb.Append($" ON DUPLICATE KEY UPDATE {string.Join(", ", setClauses)}");
        }

        if (_returning)
        {
            string retCols = _returningColumns.Count > 0
                ? string.Join(", ", _returningColumns.Select(c => _dialect.QuoteIdentifier(c)))
                : "*";
            sb.Append($" RETURNING {retCols}");
        }

        string finalSql = _tag != null ? $"-- {_tag}\n{sb}" : sb.ToString();
        return new SqlQueryResult(finalSql, parameters, _dialect.ParameterPrefix);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private string BuildTableSql()
    {
        string quotedTable = _dialect.QuoteTable(_tableName ?? typeof(T).Name);
        return !string.IsNullOrEmpty(_schema)
            ? $"{_dialect.QuoteTable(_schema)}.{quotedTable}"
            : quotedTable;
    }

    /// <summary>
    /// Returns the full INSERT keyword prefix including trailing space, e.g.
    /// "INSERT INTO ", "INSERT OR IGNORE INTO ", "INSERT IGNORE INTO ".
    /// </summary>
    private string BuildInsertKeyword()
    {
        if (_insertIgnore)
        {
            // MySQL: INSERT IGNORE INTO
            return "INSERT IGNORE INTO ";
        }

        if (_orIgnore)
        {
            // SQLite: INSERT OR IGNORE INTO
            return "INSERT OR IGNORE INTO ";
        }

        if (_orReplace)
            return "INSERT OR REPLACE INTO ";

        return "INSERT INTO ";
    }

    private SqlQueryResult BuildInsertSelect()
    {
        // _selectQuery is guaranteed non-null by the caller

        // Remap parameters from the SELECT query using sequential 'pi' prefix
        var parameters = new Dictionary<string, object>();
        var (remappedSql, remappedParams) = RemapSelectParameters(_selectQuery!, _dialect.ParameterPrefix);
        foreach (var kv in remappedParams)
            parameters[kv.Key] = kv.Value;

        string tableSql = BuildTableSql();
        string colList = _selectColumns?.Count > 0
            ? $" ({string.Join(", ", _selectColumns)})"
            : string.Empty;
        string insertKeyword = BuildInsertKeyword();

        string sql = $"{insertKeyword}{tableSql}{colList} {remappedSql}";
        string finalSql = _tag != null ? $"-- {_tag}\n{sql}" : sql;
        return new SqlQueryResult(finalSql, parameters, _dialect.ParameterPrefix);
    }

    /// <summary>
    /// Remaps parameters from a SELECT query to use sequential 'pi' prefixed names,
    /// matching the INSERT parameter prefix convention.
    /// </summary>
    /// <param name="source">The SELECT query result containing SQL and parameters to remap.</param>
    /// <param name="parameterPrefix">
    /// The dialect-specific parameter prefix (e.g. "@" for SQL Server/PostgreSQL/MySQL/SQLite,
    /// ":" for Oracle). Passed from <c>_dialect.ParameterPrefix</c> at the call site.
    /// </param>
    private static (string RenamedSql, Dictionary<string, object> RenamedParams) RemapSelectParameters(
        SqlQueryResult source, string parameterPrefix = "@")
    {
        if (source.Parameters.Count == 0)
            return (source.Sql, new Dictionary<string, object>());

        string sql = source.Sql;
        var renamed = new Dictionary<string, object>();

        // Sort keys by descending length to prevent partial replacements (e.g. @p10 before @p1).
        // Use regex word-boundary replace to avoid matching @p1 inside @p10.
        int i = 0;
        foreach (var kv in source.Parameters.OrderByDescending(p => p.Key.Length))
        {
            string newKey = $"pi{i++}";
            string oldParam = $"{parameterPrefix}{kv.Key}";
            sql = Regex.Replace(sql, Regex.Escape(oldParam) + @"\b", $"{parameterPrefix}{newKey}");
            renamed[newKey] = kv.Value;
        }

        return (sql, renamed);
    }
}
