using System.Data;
using System.Globalization;

namespace Vali_Flow.Sql.Models;

/// <summary>
/// Contains a complete parameterized SQL query (SELECT … FROM … WHERE … ORDER BY … LIMIT/OFFSET)
/// and its associated parameters.
/// </summary>
public sealed class SqlQueryResult
{
    /// <summary>The full parameterized SQL query.</summary>
    public string Sql { get; }

    /// <summary>Named parameters to pass to Dapper or ADO.NET.</summary>
    public IReadOnlyDictionary<string, object> Parameters { get; }

    /// <summary>
    /// The parameter prefix used in <see cref="Sql"/> (e.g. <c>"@"</c> for SQL Server/PostgreSQL/MySQL/SQLite,
    /// <c>":"</c> for Oracle). Used by <see cref="ApplyTo"/> to set the correct <c>IDbDataParameter.ParameterName</c>.
    /// </summary>
    public string ParameterPrefix { get; }

    internal SqlQueryResult(string sql, Dictionary<string, object> parameters, string parameterPrefix = "@")
    {
        Sql = sql ?? throw new ArgumentNullException(nameof(sql));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        ParameterPrefix = parameterPrefix ?? "@";
    }

    /// <summary>
    /// Applies all parameters to an ADO.NET <see cref="IDbCommand"/>.
    /// </summary>
    /// <param name="command">The command to populate with parameters.</param>
    /// <param name="parameterPrefix">
    /// Override the prefix used when setting <c>IDbDataParameter.ParameterName</c>.
    /// Defaults to <see cref="ParameterPrefix"/> when <see langword="null"/>.
    /// </param>
    public void ApplyTo(IDbCommand command, string? parameterPrefix = null)
        => SqlResultHelper.ApplyParameters(command, Parameters, parameterPrefix ?? ParameterPrefix);

    /// <summary>
    /// Returns the SQL with all parameters replaced by their actual values.
    /// <para><b>For debugging and logging only.</b> Do NOT use this string to execute against a database.</para>
    /// </summary>
    /// <example>
    /// Parameterized: <c>SELECT [Id] FROM [Users] WHERE [Age] > @p0 AND [Name] LIKE @p1</c><br/>
    /// Debug:         <c>SELECT [Id] FROM [Users] WHERE [Age] > 18 AND [Name] LIKE '%juan%'</c>
    /// </example>
    public string ToDebugSql()
    {
        var result = Sql;

        // Replace longest parameter names first to avoid partial replacement (e.g. @p10 before @p1)
        foreach (var (key, value) in Parameters.OrderByDescending(x => x.Key.Length))
        {
            string paramToken = key.StartsWith(ParameterPrefix, StringComparison.Ordinal)
                ? key
                : $"{ParameterPrefix}{key}";
            result = result.Replace(paramToken, FormatDebugValue(value));
        }

        return result;
    }

    /// <inheritdoc/>
    public override string ToString() => Sql;

    private static string FormatDebugValue(object? value)
    {
        if (value == null || value == DBNull.Value) return "NULL";

        return value switch
        {
            string s => $"'{s.Replace("'", "''")}'",
            bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss zzz}'",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "NULL"
        };
    }
}
