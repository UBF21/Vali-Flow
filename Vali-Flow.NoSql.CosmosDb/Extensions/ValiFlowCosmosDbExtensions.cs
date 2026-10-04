using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.CosmosDb.Models;
using Vali_Flow.NoSql.CosmosDb.Translators;
using Vali_Flow.NoSql.Extensions;

namespace Vali_Flow.NoSql.CosmosDb.Extensions;

/// <summary>
/// Extension methods that add Azure Cosmos DB SQL API WHERE-clause-building capabilities to <see cref="ValiFlow{T}"/>.
/// </summary>
/// <remarks>
/// The returned <see cref="CosmosFilterExpression"/> encapsulates the WHERE clause fragment and the
/// parameter dictionary for use with a Cosmos SQL <c>QueryDefinition</c>.
/// This package depends only on <c>Vali-Flow.NoSql</c> —
/// it is a query builder with no connection, SDK, or execution concerns.
/// </remarks>
public static class ValiFlowCosmosDbExtensions
{
    /// <summary>
    /// Translates the conditions built in this <see cref="ValiFlow{T}"/> instance
    /// into an Azure Cosmos DB <see cref="CosmosFilterExpression"/>.
    /// </summary>
    /// <typeparam name="T">The entity / document type.</typeparam>
    /// <param name="flow">The ValiFlow builder containing the conditions.</param>
    /// <param name="customConverter">Optional override to convert a value into its raw Cosmos parameter value for types the default conversion doesn't handle.</param>
    /// <returns>A <see cref="CosmosFilterExpression"/> ready to append to a Cosmos SQL query.</returns>
    /// <example>
    /// <code>
    /// var filter = new ValiFlow&lt;Order&gt;()
    ///     .EqualTo(x => x.Status, "Active")
    ///     .GreaterThan(x => x.Total, 100m);
    ///
    /// CosmosFilterExpression f = filter.ToCosmosDb();
    /// var queryText = $"SELECT * FROM c WHERE {f.WhereClause}";
    /// var query = new QueryDefinition(queryText);
    /// foreach (var kv in f.Parameters) query = query.WithParameter(kv.Key, kv.Value);
    /// </code>
    /// </example>
    public static CosmosFilterExpression ToCosmosDb<T>(this ValiFlow<T> flow, Func<object?, object?>? customConverter = null, string? tag = null) where T : class
    {
        if (flow == null) throw new ArgumentNullException(nameof(flow));

        return CosmosFilterTranslator.Translate(flow.ToNoSqlIR(), customConverter, tag, typeof(T).Name);
    }

    /// <summary>
    /// Translates a prebuilt <see cref="Expression{TDelegate}"/> into an Azure Cosmos DB <see cref="CosmosFilterExpression"/>.
    /// Use this overload when you already have a compiled expression.
    /// </summary>
    public static CosmosFilterExpression ToCosmosDb<T>(this Expression<Func<T, bool>> expression, Func<object?, object?>? customConverter = null, string? tag = null)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        return CosmosFilterTranslator.Translate(expression.ToNoSqlIR(), customConverter, tag, typeof(T).Name);
    }
}
