namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Represents a string pattern-match condition: Contains, StartsWith, or EndsWith.
/// Translates to a regex in MongoDB and a wildcard query in Elasticsearch.
/// </summary>
/// <param name="Field">The document field name.</param>
/// <param name="Pattern">The raw string value (without wildcards — the translator adds them).</param>
/// <param name="Op">The pattern-match type.</param>
/// <param name="CaseSensitive">
/// When <c>false</c> (default), the MongoDB translator emits the <c>"i"</c> flag for case-insensitive matching.
/// Set to <c>true</c> to produce a case-sensitive regex.
/// </param>
public sealed record LikeNode(string Field, string Pattern, LikeOp Op, bool CaseSensitive = false) : IConditionNode
{
    /// <summary>Accepts a visitor for traversing the IR tree.</summary>
    /// <typeparam name="TResult">The return type of the visitor.</typeparam>
    /// <param name="visitor">The visitor processing this node.</param>
    /// <returns>The result from the visitor.</returns>
    public TResult Accept<TResult>(IConditionNodeVisitor<TResult> visitor) => visitor.VisitLike(this);
}
