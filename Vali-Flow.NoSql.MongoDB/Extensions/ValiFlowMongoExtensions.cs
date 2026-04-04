using System.Linq.Expressions;
using MongoDB.Bson;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Extensions;
using Vali_Flow.NoSql.MongoDB.Translators;

namespace Vali_Flow.NoSql.MongoDB.Extensions;

/// <summary>
/// Extension methods that add MongoDB query-building capabilities to <see cref="ValiFlow{T}"/>.
/// </summary>
/// <remarks>
/// The returned <see cref="BsonDocument"/> is natively accepted by all MongoDB collection methods.
/// <c>FilterDefinition&lt;T&gt;</c> has an implicit conversion from <c>BsonDocument</c>, so
/// <c>collection.Find(filter.ToMongo())</c> works without any explicit cast.
/// This package depends only on <c>MongoDB.Bson</c> — not the full <c>MongoDB.Driver</c> —
/// making it a lightweight query builder with no connection or execution concerns.
/// </remarks>
public static class ValiFlowMongoExtensions
{
    /// <summary>
    /// Translates the conditions built in this <see cref="ValiFlow{T}"/> instance
    /// into a MongoDB <see cref="BsonDocument"/> filter.
    /// </summary>
    /// <typeparam name="T">The entity / document type.</typeparam>
    /// <param name="flow">The ValiFlow builder containing the conditions.</param>
    /// <returns>
    /// A <see cref="BsonDocument"/> filter ready to pass to <c>Find</c>, <c>CountDocuments</c>, etc.
    /// </returns>
    /// <example>
    /// <code>
    /// var filter = new ValiFlow&lt;User&gt;()
    ///     .EqualTo(x => x.IsActive, true)
    ///     .GreaterThan(x => x.Age, 18);
    ///
    /// BsonDocument mongoFilter = filter.ToMongo();
    /// var users = await collection.Find(mongoFilter).ToListAsync();
    /// </code>
    /// </example>
    public static BsonDocument ToMongo<T>(this ValiFlow<T> flow) where T : class
    {
        if (flow == null) throw new ArgumentNullException(nameof(flow));

        return MongoFilterTranslator.Translate(flow.ToNoSqlIR());
    }

    /// <summary>
    /// Translates a prebuilt <see cref="Expression{TDelegate}"/> into a MongoDB filter.
    /// Use this overload when you already have a compiled expression.
    /// </summary>
    public static BsonDocument ToMongo<T>(this Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        return MongoFilterTranslator.Translate(expression.ToNoSqlIR());
    }
}
