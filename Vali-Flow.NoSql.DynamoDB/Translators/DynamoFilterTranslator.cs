using System.Globalization;
using Amazon.DynamoDBv2.Model;
using Vali_Flow.NoSql.DynamoDB.Models;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.Translators;

namespace Vali_Flow.NoSql.DynamoDB.Translators;

/// <summary>
/// Translates a <see cref="IConditionNode"/> IR tree into a DynamoDB <see cref="DynamoFilterExpression"/>.
/// </summary>
/// <remarks>
/// The result encapsulates a <c>FilterExpression</c> string together with
/// <c>ExpressionAttributeNames</c> and <c>ExpressionAttributeValues</c>.
/// Pass these directly to a <c>ScanRequest</c> or <c>QueryRequest</c>.
/// <para>
/// <b>Limitations:</b>
/// <list type="bullet">
///   <item><see cref="LikeNode"/> with <c>EndsWith</c> is not supported — DynamoDB has no trailing-wildcard function.</item>
///   <item><see cref="InNode"/> supports a maximum of 100 values (DynamoDB limit).</item>
/// </list>
/// </para>
/// </remarks>
public static class DynamoFilterTranslator
{
    private const int MaxInValues = 100;

    /// <summary>
    /// Translates the given <see cref="IConditionNode"/> into a <see cref="DynamoFilterExpression"/>.
    /// </summary>
    /// <param name="node">The root condition node to translate.</param>
    /// <param name="customConverter">
    /// Optional hook for converting custom CLR types to <see cref="AttributeValue"/>.
    /// Called before the built-in type switch. Return <c>null</c> to fall through to the default conversion.
    /// Thread-safe: the converter is scoped to this call only.
    /// </param>
    /// <returns>
    /// A <see cref="DynamoFilterExpression"/> ready to apply to a <c>ScanRequest</c> or <c>QueryRequest</c>.
    /// </returns>
    public static DynamoFilterExpression Translate(IConditionNode node, Func<object?, AttributeValue?>? customConverter = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        var ctx = new TranslationContext();
        var visitor = new DynamoVisitor(ctx, customConverter);
        var expression = node.Accept(visitor);
        return new DynamoFilterExpression(expression, ctx.Names, ctx.Values);
    }

    private sealed class DynamoVisitor(TranslationContext ctx, Func<object?, AttributeValue?>? customConverter)
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
                ? $"attribute_not_exists({ctx.AddName(node.Field)})"
                : $"attribute_exists({ctx.AddName(node.Field)})";

        public string VisitEqual(EqualNode node) =>
            node.IsNegated
                ? $"{ctx.AddName(node.Field)} <> {ctx.AddValue(ToAttributeValue(node.Value))}"
                : $"{ctx.AddName(node.Field)} = {ctx.AddValue(ToAttributeValue(node.Value))}";

        public string VisitComparison(ComparisonNode node) =>
            $"{ctx.AddName(node.Field)} {MapComparisonOp(node.Op)} {ctx.AddValue(ToAttributeValue(node.Value))}";

        public string VisitLike(LikeNode node) => node.Op switch
        {
            LikeOp.Contains   => $"contains({ctx.AddName(node.Field)}, {ctx.AddValue(new AttributeValue { S = node.Pattern })})",
            LikeOp.StartsWith => $"begins_with({ctx.AddName(node.Field)}, {ctx.AddValue(new AttributeValue { S = node.Pattern })})",
            LikeOp.EndsWith   => throw new NotSupportedException(
                "DynamoDB FilterExpression does not support EndsWith (trailing wildcard). " +
                "Use Contains or StartsWith, or apply the filter client-side."),
            _ => throw new NotSupportedException($"LikeOp.{node.Op} is not mapped.")
        };

        public string VisitIn(InNode node)
        {
            // Empty IN → always false: contradictory attribute_exists AND attribute_not_exists
            if (node.Values.Count == 0)
                return $"(attribute_exists({ctx.AddName(node.Field)}) AND attribute_not_exists({ctx.AddName(node.Field)}))";

            if (node.Values.Count > MaxInValues)
                throw new InvalidOperationException(
                    $"DynamoDB IN expression supports at most {MaxInValues} values; " +
                    $"received {node.Values.Count}. Split the query or batch the values.");

            var field = ctx.AddName(node.Field);
            var valuePlaceholders = node.Values
                .Select(v => ctx.AddValue(ToAttributeValue(v)));
            return $"{field} IN ({string.Join(", ", valuePlaceholders)})";
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

        private AttributeValue ToAttributeValue(object? value) =>
            ConditionValueResolver.Resolve(value, customConverter, v => v switch
            {
                null      => new AttributeValue { NULL = true },
                bool b    => new AttributeValue { BOOL = b },
                string s  => new AttributeValue { S = s },
                int i     => new AttributeValue { N = i.ToString(CultureInfo.InvariantCulture) },
                long l    => new AttributeValue { N = l.ToString(CultureInfo.InvariantCulture) },
                double d  => new AttributeValue { N = d.ToString(CultureInfo.InvariantCulture) },
                float f   => new AttributeValue { N = f.ToString(CultureInfo.InvariantCulture) },
                decimal m => new AttributeValue { N = m.ToString(CultureInfo.InvariantCulture) },
                Guid g    => new AttributeValue { S = g.ToString() },
                Enum e    => new AttributeValue { N = Convert.ToInt64(e).ToString(CultureInfo.InvariantCulture) },
                _         => new AttributeValue { S = v!.ToString()! }
            });
    }

    // ── Private translation context (NOT part of public API) ─────────────────

    private sealed class TranslationContext
    {
        private int _nameCounter;
        private int _valueCounter;

        public Dictionary<string, string> Names  { get; } = new();
        public Dictionary<string, AttributeValue> Values { get; } = new();

        public string AddName(string fieldName)
        {
            var placeholder = $"#f{_nameCounter++}";
            Names[placeholder] = fieldName;
            return placeholder;
        }

        public string AddValue(AttributeValue value)
        {
            var placeholder = $":v{_valueCounter++}";
            Values[placeholder] = value;
            return placeholder;
        }
    }
}
