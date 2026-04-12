using System.Data;

namespace Vali_Flow.Sql.Models;

/// <summary>
/// Contains the translated SQL WHERE clause and its associated parameters.
/// </summary>
public sealed class SqlResult
{
    /// <summary>The parameterized SQL WHERE clause (without the WHERE keyword).</summary>
    public string Sql { get; }

    /// <summary>Named parameters to be passed to Dapper or ADO.NET.</summary>
    public IReadOnlyDictionary<string, object> Parameters { get; }

    /// <summary>
    /// The parameter prefix used in <see cref="Sql"/> (e.g. <c>"@"</c> for SQL Server/PostgreSQL/MySQL/SQLite,
    /// <c>":"</c> for Oracle). Used by <see cref="ApplyTo"/> to set the correct <c>IDbDataParameter.ParameterName</c>.
    /// </summary>
    public string ParameterPrefix { get; }

    internal SqlResult(string sql, Dictionary<string, object> parameters, string parameterPrefix = "@")
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
    /// Defaults to <see cref="ParameterPrefix"/> when <c>null</c>.
    /// </param>
    public void ApplyTo(IDbCommand command, string? parameterPrefix = null)
        => SqlResultHelper.ApplyParameters(command, Parameters, parameterPrefix ?? ParameterPrefix);

    /// <summary>Returns the SQL string for debugging.</summary>
    public override string ToString() => Sql;
}

/// <summary>Shared helper for applying parameters to an ADO.NET command.</summary>
internal static class SqlResultHelper
{
    internal static void ApplyParameters(
        IDbCommand command,
        IReadOnlyDictionary<string, object> parameters,
        string prefix)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        foreach (var kvp in parameters)
        {
            var param = command.CreateParameter();
            param.ParameterName = kvp.Key.StartsWith(prefix, StringComparison.Ordinal)
                ? kvp.Key
                : $"{prefix}{kvp.Key}";
            param.Value = kvp.Value ?? DBNull.Value;
            command.Parameters.Add(param);
        }
    }
}
