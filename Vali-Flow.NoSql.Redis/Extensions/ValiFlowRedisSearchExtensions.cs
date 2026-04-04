using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Extensions;
using Vali_Flow.NoSql.Redis.Translators;

namespace Vali_Flow.NoSql.Redis.Extensions;

/// <summary>
/// Extension methods that add RediSearch query-building capabilities to <see cref="ValiFlow{T}"/>.
/// </summary>
/// <remarks>
/// The returned query string can be passed directly to <c>db.FT().Search(indexName, new Query(result))</c>.
/// This package depends only on <c>NRedisStack</c> for the query type —
/// it is a query builder with no connection or execution concerns.
/// </remarks>
public static class ValiFlowRedisSearchExtensions
{
    /// <summary>
    /// Translates the conditions built in this <see cref="ValiFlow{T}"/> instance
    /// into a RediSearch query string.
    /// </summary>
    /// <typeparam name="T">The entity / document type.</typeparam>
    /// <param name="flow">The ValiFlow builder containing the conditions.</param>
    /// <returns>
    /// A RediSearch query string ready to pass to <c>new Query(result)</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// var filter = new ValiFlow&lt;Product&gt;()
    ///     .EqualTo(x => x.IsActive, true)
    ///     .GreaterThan(x => x.Age, 18);
    ///
    /// string query = filter.ToRedisSearch();
    /// var results = db.FT().Search("idx:products", new Query(query));
    /// </code>
    /// </example>
    public static string ToRedisSearch<T>(this ValiFlow<T> flow) where T : class
    {
        if (flow == null) throw new ArgumentNullException(nameof(flow));

        return RedisSearchFilterTranslator.Translate(flow.ToNoSqlIR());
    }

    /// <summary>
    /// Translates a prebuilt <see cref="Expression{TDelegate}"/> into a RediSearch query string.
    /// Use this overload when you already have a compiled expression.
    /// </summary>
    public static string ToRedisSearch<T>(this Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        return RedisSearchFilterTranslator.Translate(expression.ToNoSqlIR());
    }
}
