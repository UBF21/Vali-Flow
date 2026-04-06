namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Marker interface for all nodes in the Vali-Flow NoSql intermediate representation (IR) tree.
/// Each node represents a filter condition that can be translated to any NoSql provider
/// (MongoDB, Elasticsearch, etc.) without being tied to a specific query language.
/// </summary>
public interface IConditionNode
{
    TResult Accept<TResult>(IConditionNodeVisitor<TResult> visitor);
}
