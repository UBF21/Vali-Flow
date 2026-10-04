namespace Vali_Flow.NoSql.Couchbase.Models;

/// <summary>
/// Encapsulates the result of translating a Vali-Flow filter into a Couchbase N1QL (SQL++) WHERE clause.
/// </summary>
/// <remarks>
/// Couchbase has no "native" filter object like MongoDB's BsonDocument or DynamoDB's AttributeValue —
/// N1QL is plain parameterized text, the same approach as SQL. Combine <see cref="WhereClause"/> with
/// a full N1QL statement and bind <see cref="Parameters"/> as named parameters (<c>$p0</c>, <c>$p1</c>, ...):
/// <code>
/// var filter = flow.ToCouchbase();
/// var query = $"SELECT * FROM `bucket` WHERE {filter.WhereClause}";
/// var options = new QueryOptions();
/// foreach (var (name, value) in filter.Parameters)
///     options.Parameter(name, value);
/// </code>
/// </remarks>
public sealed class CouchbaseFilterExpression
{
    /// <summary>
    /// The N1QL WHERE clause fragment (without the leading <c>WHERE</c> keyword),
    /// e.g. <c>"`Name` = $p0"</c>.
    /// </summary>
    public string WhereClause { get; }

    /// <summary>Named N1QL parameters (<c>$p0</c>, <c>$p1</c>, ...) to bind to the query.</summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    internal CouchbaseFilterExpression(string whereClause, Dictionary<string, object?> parameters)
    {
        WhereClause = whereClause;
        Parameters  = parameters.AsReadOnly();
    }
}
