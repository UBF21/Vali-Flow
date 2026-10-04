using System.Globalization;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.Translators;

namespace Vali_Flow.NoSql.Redis.Translators;

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
    // RediSearch has no documented explicit limit on OR'd terms in a query. This is a
    // conservative practical cap: each value expands to a full OR clause in the query string,
    // so an unbounded list risks hitting Redis's proto-max-bulk-len and produces unwieldy queries.
    private const int MaxInValues = 1_000;

    /// <summary>
    /// Translates the given <see cref="IConditionNode"/> into a RediSearch query string.
    /// </summary>
    /// <param name="node">The root condition node to translate.</param>
    /// <param name="customConverter">
    /// Optional hook for converting custom CLR types to their Redis value string. Applied before the
    /// built-in type switch for every value-producing node (Equal, Comparison/range, and IN — both its
    /// numeric and tag branches), same evaluation order as the other NoSql providers
    /// (<see cref="Vali_Flow.NoSql.Translators.ConditionValueResolver"/>): custom first, falls back to
    /// the default conversion when it returns <c>null</c>. The string is embedded as a quoted tag value
    /// for Equal/IN-tag queries, or unquoted for Comparison/IN-numeric range bounds — return the raw
    /// value appropriate to the call site (e.g. a numeric string for a type used in range queries).
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
    public static string Translate(IConditionNode node, Func<object?, string?>? customConverter = null, string? tag = null, string? entityType = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.NoSql.Redis.Translate", tag, entityType);
        try
        {
            Validate(node);

            return node.Accept(new RedisVisitor(customConverter));
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>
    /// Validates that the node tree contains no NullNode expressions,
    /// which are not supported by RediSearch.
    /// Throws <see cref="NotSupportedException"/> with context if found.
    /// </summary>
    public static void Validate(IConditionNode node) =>
        node.Accept(new UnsupportedNodeDetector(
            isUnsupported: n => n is NullNode,
            buildMessage:  n => $"RediSearch does not support null/existence checks " +
                                $"(NullNode on field '{((NullNode)n).Field}'). " +
                                "Use Validate() before Translate() to detect this early."));

    private sealed class RedisVisitor(Func<object?, string?>? customConverter) : IConditionNodeVisitor<string>
    {
        public string VisitAnd(AndNode node) =>
            $"({node.Left.Accept(this)} {node.Right.Accept(this)})";

        public string VisitOr(OrNode node) =>
            $"({node.Left.Accept(this)} | {node.Right.Accept(this)})";

        public string VisitNot(NotNode node) =>
            $"-({node.Inner.Accept(this)})";

        public string VisitNull(NullNode node) =>
            throw new NotSupportedException(
                "RediSearch does not support field-existence / null checks without schema-specific handling. " +
                "Use a customConverter or handle null checks at the application level before querying.");

        public string VisitEqual(EqualNode node) =>
            BuildEqualQuery(node.Field, node.Value, node.IsNegated);

        public string VisitComparison(ComparisonNode node)
        {
            // Custom converter runs first (same order as the other NoSql providers via
            // ConditionValueResolver), falling back to the built-in numeric conversion.
            var bound = ConditionValueResolver.Resolve(node.Value, customConverter, ToNumericString);

            return node.Op switch
            {
                // Exclusive bound uses leading '(' before the number: [(v +inf]
                ComparisonOp.GreaterThan        => $"@{node.Field}:[({bound} +inf]",
                ComparisonOp.GreaterThanOrEqual => $"@{node.Field}:[{bound} +inf]",
                ComparisonOp.LessThan           => $"@{node.Field}:[-inf ({bound}]",
                ComparisonOp.LessThanOrEqual    => $"@{node.Field}:[-inf {bound}]",
                _ => throw new NotSupportedException($"ComparisonOp.{node.Op} is not mapped.")
            };
        }

        public string VisitLike(LikeNode node)
        {
            // Escape RediSearch query-syntax special characters in the user-supplied pattern
            // BEFORE adding our own wildcard '*' markers below — otherwise a pattern containing
            // characters like ')', '|', '@' could break out of this field's scope and inject
            // arbitrary RediSearch query syntax (e.g. "x) | @other:{admin}").
            string escaped = EscapeTextTerm(node.Pattern);
            return node.Op switch
            {
                LikeOp.Contains   => $"@{node.Field}:*{escaped}*",
                LikeOp.StartsWith => $"@{node.Field}:{escaped}*",
                LikeOp.EndsWith   => $"@{node.Field}:*{escaped}",
                _ => throw new NotSupportedException($"LikeOp.{node.Op} is not mapped.")
            };
        }

        public string VisitIn(InNode node)
        {
            // Empty IN → always false (negate match-all)
            if (node.Values.Count == 0) return "(-*)";

            if (node.Values.Count > MaxInValues)
                throw new InvalidOperationException(
                    $"RediSearch IN query supports at most {MaxInValues} values (practical limit — " +
                    $"each value expands to a full OR clause in the query string); received {node.Values.Count}. " +
                    "Split the query or batch the values.");

            if (node.Values.Any(v => v == null))
                throw new InvalidOperationException(
                    "IN condition with null values is not supported in Redis Search. Remove null values before building the query.");

            // Guard above guarantees no nulls — use node.Values directly
            bool allNumeric = node.Values.All(IsNumericOrBool);
            bool anyNumeric = node.Values.Any(IsNumericOrBool);

            // M3 fix: reject heterogeneous lists before producing a silently wrong query
            if (anyNumeric && !allNumeric)
                throw new InvalidOperationException(
                    $"InNode '{node.Field}' contains mixed numeric and non-numeric values. " +
                    "All non-null values must be of the same kind.");

            if (allNumeric)
            {
                // OR'd range queries: (@field:[v1 v1]|@field:[v2 v2]|...)
                // Custom converter first, same order as Equal/Comparison, falls back to numeric.
                var parts = node.Values.Select(v =>
                {
                    var num = ConditionValueResolver.Resolve(v!, customConverter, ToNumericString);
                    return $"@{node.Field}:[{num} {num}]";
                });
                return $"({string.Join("|", parts)})";
            }

            // Tag OR query: @field:{"v1"|"v2"|"v3"}
            var tags = node.Values.Select(v => BuildTagValue(v));
            return $"@{node.Field}:{{{string.Join("|", tags)}}}";
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private string BuildEqualQuery(string field, object value, bool negated)
        {
            // Custom converter takes priority over built-in type detection (OCP),
            // same evaluation order as every other NoSql provider via ConditionValueResolver:
            // custom first, fall back to the default conversion only when it returns null.
            var custom = customConverter?.Invoke(value);
            if (custom != null)
            {
                var customTag = $"\"{EscapeTagValue(custom)}\"";
                return negated
                    ? $"-@{field}:{{{customTag}}}"
                    : $"@{field}:{{{customTag}}}";
            }

            if (IsNumericOrBool(value))
            {
                var num = ToNumericString(value);
                return negated
                    ? $"(-@{field}:[{num} {num}])"
                    : $"@{field}:[{num} {num}]";
            }

            // String / other → quoted tag query (DIALECT 2).
            // Custom converter already ran above and returned null — build the default tag directly.
            var tag = $"\"{EscapeTagValue(DefaultTagString(value))}\"";
            return negated
                ? $"-@{field}:{{{tag}}}"
                : $"@{field}:{{{tag}}}";
        }

        private string BuildTagValue(object? value) =>
            $"\"{EscapeTagValue(ConditionValueResolver.Resolve(value, customConverter, DefaultTagString))}\"";

        private static string DefaultTagString(object? value) => value switch
        {
            null     => string.Empty,
            bool b   => b ? "true" : "false",
            string s => s,
            Enum e   => e.ToString()!,
            _        => value.ToString() ?? string.Empty
        };

        private static bool IsNumericOrBool(object? value) =>
            value is int or long or double or float or decimal or bool;

        private static string ToNumericString(object? value) => value switch
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

        // RediSearch query-syntax special characters that must be backslash-escaped when they
        // appear inside an unquoted text term (used for Contains/StartsWith/EndsWith wildcards).
        // Backslash is escaped first so escaping the others doesn't double-escape it.
        private static readonly char[] TextTermSpecialChars =
            { '\\', ',', '.', '<', '>', '{', '}', '[', ']', '"', '\'', ':', ';', '!',
              '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '+', '=', '~', '|' };

        private static string EscapeTextTerm(string value)
        {
            var sb = new System.Text.StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (Array.IndexOf(TextTermSpecialChars, c) >= 0) sb.Append('\\');
                sb.Append(c);
            }
            return sb.ToString();
        }
    }

}
