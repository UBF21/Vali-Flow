using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Vali_Flow.NoSql.IR;

namespace Vali_Flow.NoSql.Elasticsearch.Translators;

/// <summary>
/// Translates a <see cref="IConditionNode"/> IR tree into an Elasticsearch <see cref="Query"/>.
/// </summary>
/// <remarks>
/// The returned <see cref="Query"/> can be passed directly to any Elasticsearch operation that
/// accepts a query — e.g. <c>client.SearchAsync&lt;T&gt;(s =&gt; s.Query(filter.ToElasticsearch()))</c>.
///
/// Field names are taken directly from the IR node (which mirrors the .NET property name).
/// To use a custom Elasticsearch field name, map it via your index mappings or serializer settings.
/// </remarks>
public static class ElasticsearchFilterTranslator
{
    /// <summary>
    /// Optional hook for converting custom CLR types to <see cref="FieldValue"/>.
    /// When set, it is called before the built-in type switch in <see cref="ToFieldValue"/>.
    /// Return <c>null</c> to fall through to the default conversion.
    /// </summary>
    /// <example>
    /// <code>
    /// ElasticsearchFilterTranslator.CustomValueConverter = value =>
    ///     value is Money m ? FieldValue.Double((double)m.Amount) : null;
    /// </code>
    /// </example>
    public static Func<object?, FieldValue?>? CustomValueConverter { get; set; }

    /// <summary>
    /// Translates the given <see cref="IConditionNode"/> into an Elasticsearch <see cref="Query"/>.
    /// </summary>
    /// <param name="node">The root condition node to translate.</param>
    /// <returns>
    /// A <see cref="Query"/> ready to pass to <c>Search</c>, <c>Count</c>, <c>DeleteByQuery</c>, etc.
    /// </returns>
    /// <example>
    /// <code>
    /// Query esFilter = filter.ToElasticsearch();
    /// var results = await client.SearchAsync&lt;User&gt;(s =&gt; s.Query(esFilter));
    /// </code>
    /// </example>
    public static Query Translate(IConditionNode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        return TranslateNode(node);
    }

    private static Query TranslateNode(IConditionNode node) => node switch
    {
        // ── Logical combinators ───────────────────────────────────────────────
        AndNode and => Query.Bool(new BoolQuery
        {
            Must = [TranslateNode(and.Left), TranslateNode(and.Right)]
        }),

        OrNode or => Query.Bool(new BoolQuery
        {
            Should             = [TranslateNode(or.Left), TranslateNode(or.Right)],
            MinimumShouldMatch = 1
        }),

        NotNode not => Query.Bool(new BoolQuery
        {
            MustNot = [TranslateNode(not.Inner)]
        }),

        // ── Null checks ───────────────────────────────────────────────────────
        // IsNull  → field must not exist (or be explicitly null, treated as missing in ES)
        NullNode { Check: NullCheckOp.IsNull } n => Query.Bool(new BoolQuery
        {
            MustNot = [Query.Exists(new ExistsQuery { Field = n.Field })]
        }),

        // IsNotNull → field must exist
        NullNode { Check: NullCheckOp.IsNotNull } n =>
            Query.Exists(new ExistsQuery { Field = n.Field }),

        // ── Equality ──────────────────────────────────────────────────────────
        EqualNode { IsNegated: false } eq =>
            Query.Term(new TermQuery(eq.Field) { Value = ToFieldValue(eq.Value) }),

        EqualNode { IsNegated: true } eq => Query.Bool(new BoolQuery
        {
            MustNot = [Query.Term(new TermQuery(eq.Field) { Value = ToFieldValue(eq.Value) })]
        }),

        // ── Range ─────────────────────────────────────────────────────────────
        ComparisonNode cmp => Query.Range(new NumberRangeQuery(cmp.Field)
        {
            Gt  = cmp.Op == ComparisonOp.GreaterThan        ? ToDouble(cmp.Value) : null,
            Gte = cmp.Op == ComparisonOp.GreaterThanOrEqual ? ToDouble(cmp.Value) : null,
            Lt  = cmp.Op == ComparisonOp.LessThan           ? ToDouble(cmp.Value) : null,
            Lte = cmp.Op == ComparisonOp.LessThanOrEqual    ? ToDouble(cmp.Value) : null,
        }),

        // ── Pattern match (wildcard) ──────────────────────────────────────────
        LikeNode like => Query.Wildcard(new WildcardQuery(like.Field)
        {
            Value           = BuildWildcardPattern(like.Pattern, like.Op),
            CaseInsensitive = true
        }),

        // ── Membership ────────────────────────────────────────────────────────
        // Empty IN → always false (must_not match_all)
        InNode { Values.Count: 0 } => Query.Bool(new BoolQuery
        {
            MustNot = [Query.MatchAll(new MatchAllQuery())]
        }),

        InNode inNode => Query.Terms(new TermsQuery
        {
            Field = inNode.Field,
            Term  = new TermsQueryField(inNode.Values.Select(ToFieldValue).ToArray())
        }),

        _ => throw new NotSupportedException(
            $"IR node type '{node.GetType().Name}' is not supported by ElasticsearchFilterTranslator.")
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildWildcardPattern(string rawPattern, LikeOp op) => op switch
    {
        LikeOp.Contains   => $"*{EscapeWildcard(rawPattern)}*",
        LikeOp.StartsWith => $"{EscapeWildcard(rawPattern)}*",
        LikeOp.EndsWith   => $"*{EscapeWildcard(rawPattern)}",
        _ => throw new NotSupportedException($"LikeOp.{op} is not mapped.")
    };

    // Elasticsearch wildcard special chars: * ? \
    private static string EscapeWildcard(string pattern)
        => pattern.Replace(@"\", @"\\").Replace("*", @"\*").Replace("?", @"\?");

    private static double ToDouble(object value)
    {
        try { return Convert.ToDouble(value); }
        catch { throw new NotSupportedException($"Cannot convert '{value?.GetType().Name}' to double for a range query."); }
    }

    private static FieldValue ToFieldValue(object? value)
    {
        if (CustomValueConverter != null)
        {
            var custom = CustomValueConverter(value);
            if (custom.HasValue) return custom.Value;
        }

        return value switch
        {
            null          => FieldValue.Null,
            bool b        => FieldValue.Boolean(b),
            int i         => FieldValue.Long(i),
            long l        => FieldValue.Long(l),
            double d      => FieldValue.Double(d),
            float f       => FieldValue.Double(f),
            decimal dec   => FieldValue.Double((double)dec),
            string s      => FieldValue.String(s),
            Enum e        => FieldValue.Long(Convert.ToInt64(e)),
            _             => FieldValue.String(value.ToString()!)
        };
    }
}
