using System.Globalization;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.NoSql.Couchbase.Models;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.Translators;

namespace Vali_Flow.NoSql.Couchbase.Translators;

/// <summary>
/// Translates a <see cref="IConditionNode"/> IR tree into a Couchbase N1QL (SQL++)
/// <see cref="CouchbaseFilterExpression"/>.
/// </summary>
/// <remarks>
/// N1QL is plain parameterized text (like SQL), so — unlike Mongo/Elasticsearch/Dynamo —
/// there is no "native" SDK filter object to build; the output is a WHERE clause fragment
/// plus a named-parameter dictionary (<c>$p0</c>, <c>$p1</c>, ...).
/// Identifiers are backtick-quoted (<c>`field`</c>), the same convention N1QL and MySQL share.
/// </remarks>
public static class CouchbaseFilterTranslator
{
    // The IN values are bound as a single array parameter (not expanded per-value), so the real
    // constraint is the max N1QL request size. No official per-element count limit is documented
    // by Couchbase — this is a conservative practical cap used as a guardrail.
    private const int MaxInValues = 10_000;

    /// <summary>
    /// Translates the given <see cref="IConditionNode"/> into a <see cref="CouchbaseFilterExpression"/>.
    /// </summary>
    /// <param name="node">The root condition node to translate.</param>
    /// <param name="customConverter">
    /// Optional hook to convert a CLR value into the value bound as an N1QL parameter.
    /// Called before the built-in type switch. Return <c>null</c> to fall through to the default conversion.
    /// </param>
    /// <returns>A <see cref="CouchbaseFilterExpression"/> ready to bind to a N1QL query.</returns>
    public static CouchbaseFilterExpression Translate(IConditionNode node, Func<object?, object?>? customConverter = null, string? tag = null, string? entityType = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.NoSql.Couchbase.Translate", tag, entityType);
        try
        {
            var ctx = new TranslationContext();
            var visitor = new CouchbaseVisitor(ctx, customConverter);
            var clause = node.Accept(visitor);
            return new CouchbaseFilterExpression(clause, ctx.Parameters);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    private sealed class CouchbaseVisitor(TranslationContext ctx, Func<object?, object?>? customConverter)
        : IConditionNodeVisitor<string>
    {
        public string VisitAnd(AndNode node) =>
            $"({node.Left.Accept(this)} AND {node.Right.Accept(this)})";

        public string VisitOr(OrNode node) =>
            $"({node.Left.Accept(this)} OR {node.Right.Accept(this)})";

        public string VisitNot(NotNode node) =>
            $"NOT ({node.Inner.Accept(this)})";

        public string VisitNull(NullNode node) =>
            node.Check == NullCheckOp.IsNull
                ? $"{Quote(node.Field)} IS NULL"
                : $"{Quote(node.Field)} IS NOT NULL";

        public string VisitEqual(EqualNode node) =>
            node.IsNegated
                ? $"{Quote(node.Field)} <> {ctx.AddValue(ResolveValue(node.Value))}"
                : $"{Quote(node.Field)} = {ctx.AddValue(ResolveValue(node.Value))}";

        public string VisitComparison(ComparisonNode node) =>
            $"{Quote(node.Field)} {MapComparisonOp(node.Op)} {ctx.AddValue(ResolveValue(node.Value))}";

        public string VisitLike(LikeNode node)
        {
            var pattern = node.Op switch
            {
                LikeOp.Contains   => $"%{node.Pattern}%",
                LikeOp.StartsWith => $"{node.Pattern}%",
                LikeOp.EndsWith   => $"%{node.Pattern}",
                _ => throw new NotSupportedException($"LikeOp.{node.Op} is not mapped.")
            };
            return $"{Quote(node.Field)} LIKE {ctx.AddValue(pattern)}";
        }

        public string VisitIn(InNode node)
        {
            if (node.Values.Count == 0)
                throw new InvalidOperationException(
                    "IN condition with empty list is not supported in Couchbase N1QL expressions. Filter the empty case before building the query.");

            if (node.Values.Count > MaxInValues)
                throw new InvalidOperationException(
                    $"Couchbase N1QL IN expression supports at most {MaxInValues} values (practical limit, " +
                    $"bounded by the max N1QL request size); received {node.Values.Count}. " +
                    "Split the query or batch the values.");

            var values = node.Values.Select(ResolveValue).ToList();
            return $"{Quote(node.Field)} IN {ctx.AddValue(values)}";
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private static string MapComparisonOp(ComparisonOp op) => op switch
        {
            ComparisonOp.GreaterThan        => ">",
            ComparisonOp.GreaterThanOrEqual => ">=",
            ComparisonOp.LessThan           => "<",
            ComparisonOp.LessThanOrEqual    => "<=",
            _ => throw new NotSupportedException($"ComparisonOp.{op} is not mapped.")
        };

        private static string Quote(string field) => $"`{field}`";

        private object? ResolveValue(object? value) =>
            ConditionValueResolver.Resolve(value, customConverter, v => v switch
            {
                null      => null,
                bool b    => b,
                string s  => s,
                int i     => i,
                long l    => l,
                double d  => d,
                float f   => (double)f,
                decimal m => m.ToString(CultureInfo.InvariantCulture),
                Guid g    => g.ToString(),
                Enum e    => Convert.ToInt64(e),
                _         => v!.ToString()!
            });
    }

    // ── Private translation context (NOT part of public API) ─────────────────

    private sealed class TranslationContext
    {
        private int _paramCounter;

        public Dictionary<string, object?> Parameters { get; } = new();

        public string AddValue(object? value)
        {
            var placeholder = $"$p{_paramCounter++}";
            Parameters[placeholder] = value;
            return placeholder;
        }
    }
}
