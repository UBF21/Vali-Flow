using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Couchbase.Models;
using Vali_Flow.NoSql.Couchbase.Translators;
using Vali_Flow.NoSql.Extensions;

namespace Vali_Flow.NoSql.Couchbase.Extensions;

/// <summary>
/// Extension methods that add Couchbase N1QL (SQL++) WHERE-clause-building capabilities
/// to <see cref="ValiFlow{T}"/>.
/// </summary>
/// <remarks>
/// The returned <see cref="CouchbaseFilterExpression"/> encapsulates the WHERE clause fragment
/// and named parameter dictionary for use with a N1QL query. This package depends only on
/// <c>Vali-Flow.NoSql</c> — it is a query builder with no Couchbase SDK or connection concerns.
/// </remarks>
public static class ValiFlowCouchbaseExtensions
{
    /// <summary>
    /// Translates the conditions built in this <see cref="ValiFlow{T}"/> instance
    /// into a Couchbase <see cref="CouchbaseFilterExpression"/>.
    /// </summary>
    /// <typeparam name="T">The entity / document type.</typeparam>
    /// <param name="flow">The ValiFlow builder containing the conditions.</param>
    /// <param name="customConverter">Optional override to convert a value into the N1QL parameter value for types the default conversion doesn't handle.</param>
    /// <returns>A <see cref="CouchbaseFilterExpression"/> ready to bind to a N1QL query.</returns>
    /// <example>
    /// <code>
    /// var filter = new ValiFlow&lt;Order&gt;()
    ///     .EqualTo(x => x.Status, "Active")
    ///     .GreaterThan(x => x.Total, 100m);
    ///
    /// CouchbaseFilterExpression f = filter.ToCouchbase();
    /// var query = $"SELECT * FROM `orders` WHERE {f.WhereClause}";
    /// var options = new QueryOptions();
    /// foreach (var (name, value) in f.Parameters)
    ///     options.Parameter(name, value);
    /// </code>
    /// </example>
    public static CouchbaseFilterExpression ToCouchbase<T>(this ValiFlow<T> flow, Func<object?, object?>? customConverter = null, string? tag = null) where T : class
    {
        if (flow == null) throw new ArgumentNullException(nameof(flow));

        return CouchbaseFilterTranslator.Translate(flow.ToNoSqlIR(), customConverter, tag, typeof(T).Name);
    }

    /// <summary>
    /// Translates a prebuilt <see cref="Expression{TDelegate}"/> into a Couchbase <see cref="CouchbaseFilterExpression"/>.
    /// Use this overload when you already have a compiled expression.
    /// </summary>
    public static CouchbaseFilterExpression ToCouchbase<T>(this Expression<Func<T, bool>> expression, Func<object?, object?>? customConverter = null, string? tag = null)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        return CouchbaseFilterTranslator.Translate(expression.ToNoSqlIR(), customConverter, tag, typeof(T).Name);
    }
}
