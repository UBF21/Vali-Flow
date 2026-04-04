using System.Globalization;
using Amazon.DynamoDBv2.Model;
using Vali_Flow.NoSql.DynamoDB.Models;
using Vali_Flow.NoSql.IR;

namespace Vali_Flow.NoSql.DynamoDB.Translators;

// TODO: Visitor pattern if IR grows beyond 10 node types

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
        var expression = TranslateNode(node, ctx, customConverter);
        return new DynamoFilterExpression(expression, ctx.Names, ctx.Values);
    }

    private static string TranslateNode(IConditionNode node, TranslationContext ctx, Func<object?, AttributeValue?>? customConverter) => node switch
    {
        // ── Logical combinators ──────────────────────────────────────────────
        AndNode and =>
            $"({TranslateNode(and.Left, ctx, customConverter)} AND {TranslateNode(and.Right, ctx, customConverter)})",

        OrNode or =>
            $"({TranslateNode(or.Left, ctx, customConverter)} OR {TranslateNode(or.Right, ctx, customConverter)})",

        NotNode not =>
            $"NOT ({TranslateNode(not.Inner, ctx, customConverter)})",

        // ── Null checks ───────────────────────────────────────────────────────
        NullNode { Check: NullCheckOp.IsNull }    n => $"attribute_not_exists({ctx.AddName(n.Field)})",
        NullNode { Check: NullCheckOp.IsNotNull } n => $"attribute_exists({ctx.AddName(n.Field)})",

        // ── Equality ──────────────────────────────────────────────────────────
        EqualNode { IsNegated: false } eq =>
            $"{ctx.AddName(eq.Field)} = {ctx.AddValue(ToAttributeValue(eq.Value, customConverter))}",

        EqualNode { IsNegated: true } eq =>
            $"{ctx.AddName(eq.Field)} <> {ctx.AddValue(ToAttributeValue(eq.Value, customConverter))}",

        // ── Range ─────────────────────────────────────────────────────────────
        ComparisonNode cmp =>
            $"{ctx.AddName(cmp.Field)} {MapComparisonOp(cmp.Op)} {ctx.AddValue(ToAttributeValue(cmp.Value, customConverter))}",

        // ── Pattern match ─────────────────────────────────────────────────────
        LikeNode { Op: LikeOp.Contains }   like =>
            $"contains({ctx.AddName(like.Field)}, {ctx.AddValue(new AttributeValue { S = like.Pattern })})",

        LikeNode { Op: LikeOp.StartsWith } like =>
            $"begins_with({ctx.AddName(like.Field)}, {ctx.AddValue(new AttributeValue { S = like.Pattern })})",

        LikeNode { Op: LikeOp.EndsWith } =>
            throw new NotSupportedException(
                "DynamoDB FilterExpression does not support EndsWith (trailing wildcard). " +
                "Use Contains or StartsWith, or apply the filter client-side."),

        // ── Membership ────────────────────────────────────────────────────────
        // Empty IN → always false: contradictory attribute_exists AND attribute_not_exists
        InNode { Values.Count: 0 } inN =>
            $"(attribute_exists({ctx.AddName(inN.Field)}) AND attribute_not_exists({ctx.AddName(inN.Field)}))",

        InNode inNode when inNode.Values.Count > MaxInValues =>
            throw new InvalidOperationException(
                $"DynamoDB IN expression supports at most {MaxInValues} values; " +
                $"received {inNode.Values.Count}. Split the query or batch the values."),

        InNode inNode => BuildInExpression(inNode, ctx, customConverter),

        _ => throw new NotSupportedException(
            $"IR node type '{node.GetType().Name}' is not supported by DynamoFilterTranslator.")
    };

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static string BuildInExpression(InNode inNode, TranslationContext ctx, Func<object?, AttributeValue?>? customConverter)
    {
        var field = ctx.AddName(inNode.Field);
        var valuePlaceholders = inNode.Values
            .Select(v => ctx.AddValue(ToAttributeValue(v, customConverter)));
        return $"{field} IN ({string.Join(", ", valuePlaceholders)})";
    }

    private static string MapComparisonOp(ComparisonOp op) => op switch
    {
        ComparisonOp.GreaterThan        => ">",
        ComparisonOp.GreaterThanOrEqual => ">=",
        ComparisonOp.LessThan           => "<",
        ComparisonOp.LessThanOrEqual    => "<=",
        _ => throw new NotSupportedException($"ComparisonOp.{op} is not mapped.")
    };

    private static AttributeValue ToAttributeValue(object? value, Func<object?, AttributeValue?>? customConverter)
    {
        if (customConverter != null)
        {
            var custom = customConverter(value);
            if (custom != null) return custom;
        }

        return value switch
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
            _         => new AttributeValue { S = value.ToString()! }
        };
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
