namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Marker interface for all nodes in the Vali-Flow NoSql intermediate representation (IR) tree.
/// Each node represents a filter condition that can be translated to any NoSql provider
/// (MongoDB, Elasticsearch, etc.) without being tied to a specific query language.
/// </summary>
public interface IConditionNode
{
    /// <summary>Accepts a visitor for traversing the IR tree (Visitor pattern).</summary>
    /// <typeparam name="TResult">The return type of the visitor's visit methods.</typeparam>
    /// <param name="visitor">The visitor instance that processes this node.</param>
    /// <returns>The result of the visitor's processing of this node.</returns>
    TResult Accept<TResult>(IConditionNodeVisitor<TResult> visitor);
}
