using System.Globalization;

namespace Vali_Flow.Sql.Builder;

/// <summary>
/// Fluent builder for a SQL <c>CASE WHEN ... THEN ... ELSE ... END</c> expression.
/// </summary>
/// <example>
/// <code>
/// var expr = CaseWhenBuilder
///     .When("[Status] = 1", "Active")
///     .When("[Status] = 2", "Inactive")
///     .Else("Unknown")
///     .As("StatusLabel");
///
/// // Result: "CASE WHEN [Status] = 1 THEN 'Active' WHEN [Status] = 2 THEN 'Inactive' ELSE 'Unknown' END AS [StatusLabel]"
/// </code>
/// </example>
public sealed class CaseWhenBuilder
{
    private readonly List<(string When, string Then)> _branches = new();
    private string? _elseBranch;
    private string? _alias;

    private CaseWhenBuilder() { }

    /// <summary>Starts a new CASE WHEN builder with the first WHEN clause.</summary>
    /// <param name="whenSql">Raw SQL condition, e.g. <c>"[Status] = 1"</c>.</param>
    /// <param name="thenValue">The result value. Strings are automatically quoted.</param>
    public static CaseWhenBuilder Create(string whenSql, object thenValue)
    {
        if (string.IsNullOrWhiteSpace(whenSql)) throw new ArgumentException("whenSql cannot be null or whitespace.", nameof(whenSql));
        var builder = new CaseWhenBuilder();
        return builder.AddWhen(whenSql, thenValue);
    }

    /// <summary>Adds another WHEN branch.</summary>
    public CaseWhenBuilder When(string whenSql, object thenValue)
    {
        if (string.IsNullOrWhiteSpace(whenSql)) throw new ArgumentException("whenSql cannot be null or whitespace.", nameof(whenSql));
        return AddWhen(whenSql, thenValue);
    }

    /// <summary>Sets the ELSE value. Strings are automatically quoted.</summary>
    public CaseWhenBuilder Else(object elseValue)
    {
        _elseBranch = FormatValue(elseValue);
        return this;
    }

    /// <summary>Sets the column alias for the CASE expression (AS alias).</summary>
    public CaseWhenBuilder As(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("alias cannot be null or whitespace.", nameof(alias));
        _alias = alias;
        return this;
    }

    /// <summary>
    /// Builds the full CASE WHEN SQL fragment.
    /// </summary>
    /// <param name="dialect">Optional dialect for quoting the alias; if null the alias is not quoted.</param>
    public string Build(Dialects.ISqlDialect? dialect = null)
    {
        if (_branches.Count == 0)
            throw new InvalidOperationException("At least one WHEN branch is required.");

        var parts = _branches.Select(b => $"WHEN {b.When} THEN {b.Then}");
        var caseBody = string.Join(" ", parts);
        var elseClause = _elseBranch != null ? $" ELSE {_elseBranch}" : string.Empty;
        var sql = $"CASE {caseBody}{elseClause} END";

        if (_alias != null)
        {
            var quotedAlias = dialect != null ? dialect.QuoteIdentifier(_alias) : _alias;
            sql += $" AS {quotedAlias}";
        }

        return sql;
    }

    /// <inheritdoc cref="Build(Dialects.ISqlDialect?)"/>
    public override string ToString() => Build();

    private CaseWhenBuilder AddWhen(string whenSql, object thenValue)
    {
        _branches.Add((whenSql, FormatValue(thenValue)));
        return this;
    }

    private static string FormatValue(object value)
    {
        if (value == null) return "NULL";

        return value switch
        {
            string s => $"'{s.Replace("'", "''")}'",
            bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "NULL"
        };
    }
}
