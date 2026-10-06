using System.Globalization;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.NoSql.CosmosDb.Models;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.Translators;

namespace Vali_Flow.NoSql.CosmosDb.Translators;

/// <summary>
/// Translates a <see cref="IConditionNode"/> IR tree into an Azure Cosmos DB SQL API
/// <see cref="CosmosFilterExpression"/> — a parameterized WHERE clause fragment.
/// </summary>
/// <remarks>
/// The result uses the standard Cosmos SQL container alias <c>c</c> (e.g. <c>c.Age</c>) and
/// SQL Server-style <c>@p0</c> parameter placeholders, ready to append to a query text and
/// bind via <c>QueryDefinition.WithParameter</c>.
/// <para>
/// <b>Mapping notes:</b>
/// <list type="bullet">
///   <item><see cref="LikeNode"/> maps to the native <c>CONTAINS</c>/<c>STARTSWITH</c>/<c>ENDSWITH</c> functions.</item>
///   <item><see cref="NullNode"/> maps to the native <c>IS_NULL</c> function.</item>
///   <item><see cref="InNode"/> expands to one placeholder per value — Cosmos SQL does not accept a single array parameter for <c>IN</c>.</item>
/// </list>
/// </para>
/// </remarks>
public static class CosmosFilterTranslator
{
    private const string ContainerAlias = "c.";

    // Cosmos SQL expands every IN value into its own @pN placeholder, consuming the query's
    // documented 512KB max query size and its parameter-count budget alongside other params in the
    // same query. No official per-element IN limit is documented — this is a conservative practical
    // cap, not an exact Cosmos limit.
    private const int MaxInValues = 6_000;

    /// <summary>
    /// Translates the given <see cref="IConditionNode"/> into a <see cref="CosmosFilterExpression"/>.
    /// </summary>
    /// <param name="node">The root condition node to translate.</param>
    /// <param name="customConverter">
    /// Optional hook for converting custom CLR types into the raw value stored in the parameters dictionary.
    /// Called before the built-in type switch. Return <c>null</c> to fall through to the default conversion.
    /// Thread-safe: the converter is scoped to this call only.
    /// </param>
    /// <returns>A <see cref="CosmosFilterExpression"/> ready to append to a Cosmos SQL query.</returns>
    public static CosmosFilterExpression Translate(IConditionNode node, Func<object?, object?>? customConverter = null, string? tag = null, string? entityType = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.NoSql.CosmosDb.Translate", tag, entityType);
        try
        {
            var ctx = new TranslationContext();
            var visitor = new CosmosVisitor(ctx, customConverter);
            var expression = node.Accept(visitor);
            return new CosmosFilterExpression(expression, ctx.Parameters);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    private sealed class CosmosVisitor(TranslationContext ctx, Func<object?, object?>? customConverter)
        : IConditionNodeVisitor<string>
    {
        public string VisitAnd(AndNode node) =>
            $"({node.Left.Accept(this)} AND {node.Right.Accept(this)})";

        public string VisitOr(OrNode node) =>
            $"({node.Left.Accept(this)} OR {node.Right.Accept(this)})";

        public string VisitNot(NotNode node) =>
            $"NOT ({node.Inner.Accept(this)})";

        public string VisitNull(NullNode node) => NullHelper.Build(node);

        public string VisitEqual(EqualNode node) => EqualHelper.Build(node, ctx, customConverter);

        public string VisitComparison(ComparisonNode node) => ComparisonHelper.Build(node, ctx, customConverter);

        public string VisitLike(LikeNode node) => LikeHelper.Build(node, ctx);

        public string VisitIn(InNode node) => InHelper.Build(node, ctx, customConverter);
    }

    // ── Node-specific builders (kept tiny, one responsibility each) ──────────

    private static class NullHelper
    {
        public static string Build(NullNode node) =>
            node.Check == NullCheckOp.IsNull
                ? $"IS_NULL({Field(node.Field)})"
                : $"NOT IS_NULL({Field(node.Field)})";
    }

    private static class EqualHelper
    {
        public static string Build(EqualNode node, TranslationContext ctx, Func<object?, object?>? customConverter)
        {
            var placeholder = ctx.AddParameter(ConvertValue(node.Value, customConverter));
            return node.IsNegated
                ? $"{Field(node.Field)} != {placeholder}"
                : $"{Field(node.Field)} = {placeholder}";
        }
    }

    private static class ComparisonHelper
    {
        public static string Build(ComparisonNode node, TranslationContext ctx, Func<object?, object?>? customConverter) =>
            $"{Field(node.Field)} {MapComparisonOp(node.Op)} {ctx.AddParameter(ConvertValue(node.Value, customConverter))}";

        private static string MapComparisonOp(ComparisonOp op) => op switch
        {
            ComparisonOp.GreaterThan        => ">",
            ComparisonOp.GreaterThanOrEqual => ">=",
            ComparisonOp.LessThan           => "<",
            ComparisonOp.LessThanOrEqual    => "<=",
            _ => throw new NotSupportedException($"ComparisonOp.{op} is not mapped.")
        };
    }

    private static class LikeHelper
    {
        public static string Build(LikeNode node, TranslationContext ctx) => node.Op switch
        {
            LikeOp.Contains   => $"CONTAINS({Field(node.Field)}, {ctx.AddParameter(node.Pattern)})",
            LikeOp.StartsWith => $"STARTSWITH({Field(node.Field)}, {ctx.AddParameter(node.Pattern)})",
            LikeOp.EndsWith   => $"ENDSWITH({Field(node.Field)}, {ctx.AddParameter(node.Pattern)})",
            _ => throw new NotSupportedException($"LikeOp.{node.Op} is not mapped.")
        };
    }

    private static class InHelper
    {
        public static string Build(InNode node, TranslationContext ctx, Func<object?, object?>? customConverter)
        {
            if (node.Values.Count == 0)
                throw new InvalidOperationException(
                    "IN condition with empty list is not supported in Cosmos DB SQL queries. Filter the empty case before building the query.");

            if (node.Values.Count > MaxInValues)
                throw new InvalidOperationException(
                    $"Cosmos DB SQL IN expression supports at most {MaxInValues} values (conservative practical " +
                    $"limit — each value consumes a placeholder against the 512KB max query size and the query's " +
                    $"parameter budget); received {node.Values.Count}. Split the query or batch the values.");

            var field = Field(node.Field);
            var placeholders = node.Values.Select(v => ctx.AddParameter(ConvertValue(v, customConverter)));
            return $"{field} IN ({string.Join(", ", placeholders)})";
        }
    }

    private static string Field(string fieldName) => $"{ContainerAlias}{fieldName}";

    private static object? ConvertValue(object? value, Func<object?, object?>? customConverter) =>
        ConditionValueResolver.Resolve(value, customConverter, v => v switch
        {
            null      => null,
            bool b    => b,
            string s  => s,
            int i     => i,
            long l    => l,
            double d  => d,
            float f   => f,
            decimal m => m,
            Guid g    => g.ToString(),
            Enum e    => Convert.ToInt64(e, CultureInfo.InvariantCulture),
            _         => v!.ToString()!
        });

    // ── Private translation context (NOT part of public API) ─────────────────

    private sealed class TranslationContext
    {
        private int _paramCounter;

        public Dictionary<string, object?> Parameters { get; } = new();

        public string AddParameter(object? value)
        {
            var placeholder = $"@p{_paramCounter++}";
            Parameters[placeholder] = value;
            return placeholder;
        }
    }
}
