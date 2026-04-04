using System.Globalization;
using Vali_Flow.NoSql.IR;

namespace Vali_Flow.NoSql.Redis.Translators;

// TODO: Visitor pattern if IR grows beyond 10 node types

/// <summary>
/// Translates a <see cref="IConditionNode"/> IR tree into a RediSearch query string.
/// </summary>
/// <remarks>
/// The returned string can be passed directly to <c>db.FT().Search(index, new Query(result))</c>.
/// NRedisStack 1.3.0+ automatically appends <c>DIALECT 2</c>, which enables quoted tag values.
/// <para>
/// Field names map directly to the .NET property name. For custom Redis field names,
/// use a naming convention in your RediSearch index definition.
/// </para>
/// <para>
/// <b>Limitation:</b> <see cref="NullNode"/> (IsNull / IsNotNull) is not supported — RediSearch
/// has no native field-existence query. Use a <c>customConverter</c> or handle
/// null checks at the application level.
/// </para>
/// </remarks>
public static class RedisSearchFilterTranslator
{
    /// <summary>
    /// Translates the given <see cref="IConditionNode"/> into a RediSearch query string.
    /// </summary>
    /// <param name="node">The root condition node to translate.</param>
    /// <param name="customConverter">
    /// Optional hook for converting custom CLR types to their Redis tag-value string.
    /// Called before the built-in type switch. Return <c>null</c> to fall through to the default conversion.
    /// Thread-safe: the converter is scoped to this call only.
    /// </param>
    /// <returns>
    /// A RediSearch query string ready to pass to <c>new Query(result)</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// string query = filter.ToRedisSearch(v => v is Money m ? m.Amount.ToString(CultureInfo.InvariantCulture) : null);
    /// var results = db.FT().Search("idx:products", new Query(query));
    /// </code>
    /// </example>
    public static string Translate(IConditionNode node, Func<object?, string?>? customConverter = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        return TranslateNode(node, customConverter);
    }

    private static string TranslateNode(IConditionNode node, Func<object?, string?>? customConverter) => node switch
    {
        // ── Logical combinators ──────────────────────────────────────────────
        AndNode and => $"({TranslateNode(and.Left, customConverter)} {TranslateNode(and.Right, customConverter)})",

        OrNode or => $"({TranslateNode(or.Left, customConverter)} | {TranslateNode(or.Right, customConverter)})",

        NotNode not => $"-({TranslateNode(not.Inner, customConverter)})",

        // ── Null checks — not natively supported in RediSearch ───────────────
        NullNode => throw new NotSupportedException(
            "RediSearch does not support field-existence / null checks without schema-specific handling. " +
            "Use a customConverter or handle null checks at the application level before querying."),

        // ── Equality ─────────────────────────────────────────────────────────
        EqualNode { IsNegated: false } eq => BuildEqualQuery(eq.Field, eq.Value, negated: false, customConverter),
        EqualNode { IsNegated: true }  eq => BuildEqualQuery(eq.Field, eq.Value, negated: true,  customConverter),

        // ── Range ─────────────────────────────────────────────────────────────
        // Exclusive bound uses leading '(' before the number: [(v +inf]
        ComparisonNode cmp => cmp.Op switch
        {
            ComparisonOp.GreaterThan        => $"@{cmp.Field}:[({ToNumericString(cmp.Value)} +inf]",
            ComparisonOp.GreaterThanOrEqual => $"@{cmp.Field}:[{ToNumericString(cmp.Value)} +inf]",
            ComparisonOp.LessThan           => $"@{cmp.Field}:[-inf ({ToNumericString(cmp.Value)}]",
            ComparisonOp.LessThanOrEqual    => $"@{cmp.Field}:[-inf {ToNumericString(cmp.Value)}]",
            _ => throw new NotSupportedException($"ComparisonOp.{cmp.Op} is not mapped.")
        },

        // ── Pattern match (wildcard — TEXT field) ─────────────────────────────
        LikeNode like => like.Op switch
        {
            LikeOp.Contains   => $"@{like.Field}:*{like.Pattern}*",
            LikeOp.StartsWith => $"@{like.Field}:{like.Pattern}*",
            LikeOp.EndsWith   => $"@{like.Field}:*{like.Pattern}",
            _ => throw new NotSupportedException($"LikeOp.{like.Op} is not mapped.")
        },

        // ── Membership ───────────────────────────────────────────────────────
        // Empty IN → always false (negate match-all)
        InNode { Values.Count: 0 } => "(-*)",

        InNode inNode => BuildInQuery(inNode, customConverter),

        _ => throw new NotSupportedException(
            $"IR node type '{node.GetType().Name}' is not supported by RedisSearchFilterTranslator.")
    };

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static string BuildEqualQuery(string field, object value, bool negated, Func<object?, string?>? customConverter)
    {
        // OCP: custom converter takes priority over built-in type detection
        if (customConverter != null)
        {
            var custom = customConverter(value);
            if (custom != null)
            {
                var customTag = $"\"{EscapeTagValue(custom)}\"";
                return negated
                    ? $"-@{field}:{{{customTag}}}"
                    : $"@{field}:{{{customTag}}}";
            }
        }

        if (IsNumericOrBool(value))
        {
            var num = ToNumericString(value);
            return negated
                ? $"(-@{field}:[{num} {num}])"
                : $"@{field}:[{num} {num}]";
        }

        // String / other → quoted tag query (DIALECT 2).
        // Converter already returned null above — pass null to avoid double-invocation (O1 fix).
        var tag = BuildTagValue(value, null);
        return negated
            ? $"-@{field}:{{{tag}}}"
            : $"@{field}:{{{tag}}}";
    }

    private static string BuildInQuery(InNode inNode, Func<object?, string?>? customConverter)
    {
        var nonNull = inNode.Values.Where(v => v != null).ToList();

        if (nonNull.Count > 0)
        {
            bool allNumeric = nonNull.All(IsNumericOrBool);
            bool anyNumeric = nonNull.Any(IsNumericOrBool);

            // M3 fix: reject heterogeneous lists before producing a silently wrong query
            if (anyNumeric && !allNumeric)
                throw new InvalidOperationException(
                    $"InNode '{inNode.Field}' contains mixed numeric and non-numeric values. " +
                    "All non-null values must be of the same kind.");

            if (allNumeric)
            {
                // OR'd range queries: (@field:[v1 v1]|@field:[v2 v2]|...)
                var parts = nonNull.Select(v => $"@{inNode.Field}:[{ToNumericString(v!)} {ToNumericString(v!)}]");
                return $"({string.Join("|", parts)})";
            }
        }

        // Tag OR query: @field:{"v1"|"v2"|"v3"}
        var tags = inNode.Values.Select(v => BuildTagValue(v, customConverter));
        return $"@{inNode.Field}:{{{string.Join("|", tags)}}}";
    }

    private static string BuildTagValue(object? value, Func<object?, string?>? customConverter)
    {
        if (customConverter != null)
        {
            var custom = customConverter(value);
            if (custom != null) return $"\"{EscapeTagValue(custom)}\"";
        }

        var str = value switch
        {
            null    => string.Empty,
            bool b  => b ? "true" : "false",
            string s => s,
            Enum e  => e.ToString(),
            _       => value.ToString() ?? string.Empty
        };
        return $"\"{EscapeTagValue(str)}\"";
    }

    private static bool IsNumericOrBool(object? value) =>
        value is int or long or double or float or decimal or bool;

    private static string ToNumericString(object value) => value switch
    {
        bool b      => b ? "1" : "0",
        int i       => i.ToString(CultureInfo.InvariantCulture),
        long l      => l.ToString(CultureInfo.InvariantCulture),
        double d    => d.ToString(CultureInfo.InvariantCulture),
        float f     => f.ToString(CultureInfo.InvariantCulture),
        decimal dec => ((double)dec).ToString(CultureInfo.InvariantCulture),
        _           => Convert.ToDouble(value).ToString(CultureInfo.InvariantCulture)
    };

    // Escape \ and " inside DIALECT 2 quoted tag values
    private static string EscapeTagValue(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
