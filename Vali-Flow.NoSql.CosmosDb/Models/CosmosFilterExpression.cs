namespace Vali_Flow.NoSql.CosmosDb.Models;

/// <summary>
/// Encapsulates the result of translating a Vali-Flow filter into a Cosmos DB SQL API WHERE clause fragment.
/// </summary>
/// <remarks>
/// Pass the two properties directly when building a Cosmos SQL query:
/// <code>
/// var cosmos = filter.ToCosmosDb();
/// var queryText = $"SELECT * FROM c WHERE {cosmos.WhereClause}";
/// var query = new QueryDefinition(queryText);
/// foreach (var kv in cosmos.Parameters) query = query.WithParameter(kv.Key, kv.Value);
/// </code>
/// </remarks>
public sealed class CosmosFilterExpression
{
    /// <summary>The WHERE clause fragment, without the leading "WHERE" keyword (e.g. <c>"c.Age &gt; @p0"</c>).</summary>
    public string WhereClause { get; }

    /// <summary>Mapping from parameter placeholders (<c>@p0</c>) to their values.</summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    internal CosmosFilterExpression(string whereClause, Dictionary<string, object?> parameters)
    {
        WhereClause = whereClause;
        Parameters = parameters.AsReadOnly();
    }
}
