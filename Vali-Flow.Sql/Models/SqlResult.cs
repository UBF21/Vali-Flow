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

    internal SqlResult(string sql, Dictionary<string, object> parameters)
    {
        Sql = sql ?? throw new ArgumentNullException(nameof(sql));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    /// <summary>
    /// Applies all parameters to an ADO.NET <see cref="IDbCommand"/>.
    /// </summary>
    public void ApplyTo(IDbCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        foreach (var kvp in Parameters)
        {
            var param = command.CreateParameter();
            param.ParameterName = kvp.Key;
            param.Value = kvp.Value ?? DBNull.Value;
            command.Parameters.Add(param);
        }
    }

    /// <summary>Returns the SQL string for debugging.</summary>
    public override string ToString() => Sql;
}
