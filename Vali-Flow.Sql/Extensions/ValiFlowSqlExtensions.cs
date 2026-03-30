using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;

namespace Vali_Flow.Sql.Extensions;

/// <summary>
/// Extension methods that add SQL translation capabilities to <see cref="ValiFlow{T}"/>.
/// </summary>
public static class ValiFlowSqlExtensions
{
    /// <summary>
    /// Translates the conditions built in this <see cref="ValiFlow{T}"/> instance
    /// into a parameterized SQL WHERE clause using the specified dialect.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="flow">The ValiFlow builder containing the conditions.</param>
    /// <param name="dialect">The SQL dialect to use for translation.</param>
    /// <returns>
    /// A <see cref="SqlResult"/> containing the SQL string and parameters dictionary.
    /// </returns>
    /// <example>
    /// <code>
    /// var filter = new ValiFlow&lt;User&gt;()
    ///     .NotNull(x => x.Name)
    ///     .GreaterThan(x => x.Age, 18);
    ///
    /// var sql = filter.ToSql(new SqlServerDialect());
    /// // sql.Sql        → "[Name] IS NOT NULL AND [Age] > @p0"
    /// // sql.Parameters → { "p0": 18 }
    ///
    /// // Dapper:
    /// var users = connection.Query&lt;User&gt;($"SELECT * FROM Users WHERE {sql.Sql}", sql.Parameters);
    ///
    /// // ADO.NET:
    /// cmd.CommandText = $"SELECT * FROM Users WHERE {sql.Sql}";
    /// sql.ApplyTo(cmd);
    /// </code>
    /// </example>
    public static SqlResult ToSql<T>(this ValiFlow<T> flow, ISqlDialect dialect)
    {
        if (flow == null)
        {
            throw new ArgumentNullException(nameof(flow));
        }

        if (dialect == null)
        {
            throw new ArgumentNullException(nameof(dialect));
        }

        Expression<Func<T, bool>> expression = flow.Build();
        return ExpressionToSqlVisitor.Translate(expression, dialect);
    }

    /// <summary>
    /// Translates the conditions built in this <see cref="ValiFlowQuery{T}"/> instance
    /// into a parameterized SQL WHERE clause using the specified dialect.
    /// </summary>
    public static SqlResult ToSql<T>(this ValiFlowQuery<T> flow, ISqlDialect dialect)
    {
        if (flow == null) throw new ArgumentNullException(nameof(flow));
        if (dialect == null) throw new ArgumentNullException(nameof(dialect));
        return ExpressionToSqlVisitor.Translate(flow.Build(), dialect);
    }

    /// <summary>
    /// Generates a <c>SELECT COUNT(*) FROM [table] WHERE ...</c> query using a <see cref="ValiFlowQuery{T}"/> filter.
    /// </summary>
    public static SqlQueryResult ToSqlCount<T>(
        this ValiFlowQuery<T>? flow,
        ISqlDialect dialect,
        string? tableName = null) where T : class
    {
        if (dialect == null) throw new ArgumentNullException(nameof(dialect));

        string table = dialect.QuoteTable(tableName ?? typeof(T).Name);
        var parameters = new Dictionary<string, object>();
        string whereClause = string.Empty;

        if (flow != null)
        {
            var whereResult = ExpressionToSqlVisitor.Translate(flow.Build(), dialect);
            whereClause = $" WHERE {whereResult.Sql}";
            foreach (var p in whereResult.Parameters)
                parameters[p.Key] = p.Value;
        }

        return new SqlQueryResult($"SELECT COUNT(*) FROM {table}{whereClause}", parameters);
    }

    /// <summary>
    /// Translates a prebuilt <see cref="Expression{TDelegate}"/> into SQL.
    /// Use this overload when you already have a compiled expression.
    /// </summary>
    public static SqlResult ToSql<T>(this Expression<Func<T, bool>> expression, ISqlDialect dialect)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        if (dialect == null)
        {
            throw new ArgumentNullException(nameof(dialect));
        }

        return ExpressionToSqlVisitor.Translate(expression, dialect);
    }

    /// <summary>
    /// Generates a <c>SELECT COUNT(*) FROM [table] WHERE ...</c> query using the specified filter and dialect.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="flow">The ValiFlow filter to apply in the WHERE clause. Pass null to count all rows.</param>
    /// <param name="dialect">The SQL dialect.</param>
    /// <param name="tableName">
    /// Optional table name override. If null, <c>typeof(T).Name</c> is used.
    /// </param>
    /// <returns>A <see cref="SqlQueryResult"/> with the COUNT query and parameters.</returns>
    /// <example>
    /// <code>
    /// var countResult = new ValiFlow&lt;User&gt;()
    ///     .EqualTo(x => x.IsActive, true)
    ///     .ToSqlCount(new SqlServerDialect(), "Users");
    ///
    /// // countResult.Sql → "SELECT COUNT(*) FROM [Users] WHERE [IsActive] = @p0"
    /// var count = connection.ExecuteScalar&lt;int&gt;(countResult.Sql, countResult.Parameters);
    /// </code>
    /// </example>
    public static SqlQueryResult ToSqlCount<T>(
        this ValiFlow<T>? flow,
        ISqlDialect dialect,
        string? tableName = null) where T : class
    {
        if (dialect == null) throw new ArgumentNullException(nameof(dialect));

        string table = dialect.QuoteTable(tableName ?? typeof(T).Name);
        var parameters = new Dictionary<string, object>();
        string whereClause = string.Empty;

        if (flow != null)
        {
            var whereResult = ExpressionToSqlVisitor.Translate(flow.Build(), dialect);
            whereClause = $" WHERE {whereResult.Sql}";
            foreach (var p in whereResult.Parameters)
                parameters[p.Key] = p.Value;
        }

        return new SqlQueryResult($"SELECT COUNT(*) FROM {table}{whereClause}", parameters);
    }
}
