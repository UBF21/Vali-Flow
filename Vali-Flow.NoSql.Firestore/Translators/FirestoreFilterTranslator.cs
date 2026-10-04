using Google.Cloud.Firestore;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.Translators;

namespace Vali_Flow.NoSql.Firestore.Translators;

/// <summary>
/// Translates a <see cref="IConditionNode"/> IR tree into a native Google Cloud Firestore <see cref="Filter"/>.
/// </summary>
/// <remarks>
/// The output <see cref="Filter"/> is accepted directly by <c>Query.Where(filter)</c> — no wrapping needed.
/// <para>
/// <b>Limitations (confirmed against Google.Cloud.Firestore 4.4.0):</b>
/// <list type="bullet">
///   <item><see cref="LikeNode"/> (Contains/StartsWith/EndsWith) is not supported — Firestore has no
///   pattern-matching/regex query operator.</item>
///   <item><see cref="NotNode"/> is not supported — the SDK exposes no generic "NOT filter" composition;
///   only field-level negations (<c>NotEqualTo</c>, <c>NotInArray</c>) exist, which are already handled by
///   <see cref="EqualNode.IsNegated"/> and would need to be expressed at the node level.</item>
/// </list>
/// </para>
/// </remarks>
public static class FirestoreFilterTranslator
{
    /// <summary>
    /// Translates the given <see cref="IConditionNode"/> into a native Firestore <see cref="Filter"/>.
    /// </summary>
    /// <param name="node">The root condition node to translate.</param>
    /// <param name="customConverter">
    /// Optional hook for converting custom CLR types into a value Firestore accepts.
    /// Called before the built-in type switch. Return <c>null</c> to fall through to the default conversion.
    /// Thread-safe: the converter is scoped to this call only.
    /// </param>
    /// <returns>A <see cref="Filter"/> ready to pass to <c>Query.Where(filter)</c>.</returns>
    public static Filter Translate(IConditionNode node, Func<object?, object?>? customConverter = null, string? tag = null, string? entityType = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.NoSql.Firestore.Translate", tag, entityType);
        try
        {
            return node.Accept(new FirestoreVisitor(customConverter));
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    private sealed class FirestoreVisitor(Func<object?, object?>? customConverter) : IConditionNodeVisitor<Filter>
    {
        public Filter VisitAnd(AndNode node) =>
            Filter.And(node.Left.Accept(this), node.Right.Accept(this));

        public Filter VisitOr(OrNode node) =>
            Filter.Or(node.Left.Accept(this), node.Right.Accept(this));

        public Filter VisitNot(NotNode node) =>
            throw new NotSupportedException(
                "Firestore does not support a generic NOT filter over an arbitrary sub-expression. " +
                "The SDK only exposes field-level negations (NotEqualTo/NotInArray) via Filter, " +
                "which are already handled when the negated node is an EqualNode/InNode directly.");

        public Filter VisitEqual(EqualNode node) =>
            node.IsNegated
                ? Filter.NotEqualTo(node.Field, ToValue(node.Value))
                : Filter.EqualTo(node.Field, ToValue(node.Value));

        public Filter VisitComparison(ComparisonNode node) => node.Op switch
        {
            ComparisonOp.GreaterThan        => Filter.GreaterThan(node.Field, ToValue(node.Value)),
            ComparisonOp.GreaterThanOrEqual => Filter.GreaterThanOrEqualTo(node.Field, ToValue(node.Value)),
            ComparisonOp.LessThan           => Filter.LessThan(node.Field, ToValue(node.Value)),
            ComparisonOp.LessThanOrEqual    => Filter.LessThanOrEqualTo(node.Field, ToValue(node.Value)),
            _ => throw new NotSupportedException($"ComparisonOp.{node.Op} is not mapped.")
        };

        public Filter VisitLike(LikeNode node) => throw new NotSupportedException(
            $"Firestore does not support pattern-matching queries (LikeOp.{node.Op}). " +
            "There is no regex/LIKE query operator — filter client-side or use a dedicated search index (e.g. Algolia/Elasticsearch) instead.");

        public Filter VisitIn(InNode node) =>
            Filter.InArray(node.Field, node.Values.Select(ToValue).ToArray());

        public Filter VisitNull(NullNode node) =>
            node.Check == NullCheckOp.IsNull
                ? Filter.EqualTo(node.Field, null)
                : Filter.NotEqualTo(node.Field, null);

        // ── Helpers ───────────────────────────────────────────────────────────────

        private object? ToValue(object? value) =>
            ConditionValueResolver.Resolve(value, customConverter, v => v switch
            {
                null               => null,
                Enum e             => Convert.ToInt32(e),
                DateTimeOffset dto => Timestamp.FromDateTimeOffset(dto),
                DateTime dt        => Timestamp.FromDateTime(dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime()),
                _                  => v
            });
    }
}
