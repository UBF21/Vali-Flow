namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Represents a string pattern-match condition: Contains, StartsWith, or EndsWith.
/// Translates to a regex in MongoDB and a wildcard query in Elasticsearch.
/// </summary>
/// <param name="Field">The document field name.</param>
/// <param name="Pattern">The raw string value (without wildcards — the translator adds them).</param>
/// <param name="Op">The pattern-match type.</param>
public sealed record LikeNode(string Field, string Pattern, LikeOp Op) : IConditionNode;
