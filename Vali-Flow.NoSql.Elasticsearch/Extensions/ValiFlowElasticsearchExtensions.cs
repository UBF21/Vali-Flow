using System.Linq.Expressions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Elasticsearch.Translators;
using Vali_Flow.NoSql.Extensions;

namespace Vali_Flow.NoSql.Elasticsearch.Extensions;

/// <summary>
/// Extension methods that add Elasticsearch query-building capabilities to <see cref="ValiFlow{T}"/>.
/// </summary>
/// <remarks>
/// The returned <see cref="Query"/> can be passed directly to any Elasticsearch operation:
/// <c>client.SearchAsync&lt;T&gt;(s =&gt; s.Query(filter.ToElasticsearch()))</c>.
/// This package depends only on <c>Elastic.Clients.Elasticsearch</c> for query types —
/// it is a query builder with no connection or execution concerns.
/// </remarks>
public static class ValiFlowElasticsearchExtensions
{
    /// <summary>
    /// Translates the conditions built in this <see cref="ValiFlow{T}"/> instance
    /// into an Elasticsearch <see cref="Query"/>.
    /// </summary>
    /// <typeparam name="T">The entity / document type.</typeparam>
    /// <param name="flow">The ValiFlow builder containing the conditions.</param>
    /// <returns>
    /// A <see cref="Query"/> ready to pass to <c>Search</c>, <c>Count</c>, <c>DeleteByQuery</c>, etc.
    /// </returns>
    /// <example>
    /// <code>
    /// var filter = new ValiFlow&lt;User&gt;()
    ///     .EqualTo(x => x.IsActive, true)
    ///     .GreaterThan(x => x.Age, 18);
    ///
    /// Query esFilter = filter.ToElasticsearch();
    /// var results = await client.SearchAsync&lt;User&gt;(s =&gt; s.Query(esFilter));
    /// </code>
    /// </example>
    public static Query ToElasticsearch<T>(this ValiFlow<T> flow, Func<object?, FieldValue?>? customConverter = null) where T : class
    {
        if (flow == null) throw new ArgumentNullException(nameof(flow));

        return ElasticsearchFilterTranslator.Translate(flow.ToNoSqlIR(), customConverter);
    }

    /// <summary>
    /// Translates a prebuilt <see cref="Expression{TDelegate}"/> into an Elasticsearch query.
    /// Use this overload when you already have a compiled expression.
    /// </summary>
    public static Query ToElasticsearch<T>(this Expression<Func<T, bool>> expression, Func<object?, FieldValue?>? customConverter = null)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        return ElasticsearchFilterTranslator.Translate(expression.ToNoSqlIR(), customConverter);
    }
}
