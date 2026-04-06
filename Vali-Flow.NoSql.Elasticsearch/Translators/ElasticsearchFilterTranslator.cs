using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.Translators;

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
    /// Translates the given <see cref="IConditionNode"/> into an Elasticsearch <see cref="Query"/>.
    /// </summary>
    /// <param name="node">The root condition node to translate.</param>
    /// <param name="customConverter">
    /// Optional hook for converting custom CLR types to <see cref="FieldValue"/>.
    /// Called before the built-in type switch. Return <c>null</c> to fall through to the default conversion.
    /// Thread-safe: the converter is scoped to this call only.
    /// </param>
    /// <returns>
    /// A <see cref="Query"/> ready to pass to <c>Search</c>, <c>Count</c>, <c>DeleteByQuery</c>, etc.
    /// </returns>
    /// <example>
    /// <code>
    /// Query esFilter = filter.ToElasticsearch(v => v is Money m ? FieldValue.Double((double)m.Amount) : null);
    /// var results = await client.SearchAsync&lt;User&gt;(s =&gt; s.Query(esFilter));
    /// </code>
    /// </example>
    public static Query Translate(IConditionNode node, Func<object?, FieldValue?>? customConverter = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        return node.Accept(new ElasticsearchVisitor(customConverter));
    }

    private sealed class ElasticsearchVisitor(Func<object?, FieldValue?>? customConverter) : IConditionNodeVisitor<Query>
    {
        public Query VisitAnd(AndNode node) => Query.Bool(new BoolQuery
        {
            Must = [node.Left.Accept(this), node.Right.Accept(this)]
        });

        public Query VisitOr(OrNode node) => Query.Bool(new BoolQuery
        {
            Should             = [node.Left.Accept(this), node.Right.Accept(this)],
            MinimumShouldMatch = 1
        });

        public Query VisitNot(NotNode node) => Query.Bool(new BoolQuery
        {
            MustNot = [node.Inner.Accept(this)]
        });

        public Query VisitEqual(EqualNode node) =>
            node.IsNegated
                ? Query.Bool(new BoolQuery
                {
                    MustNot = [Query.Term(new TermQuery(node.Field) { Value = ToFieldValue(node.Value) })]
                })
                : Query.Term(new TermQuery(node.Field) { Value = ToFieldValue(node.Value) });

        public Query VisitComparison(ComparisonNode node) => node.Op switch
        {
            ComparisonOp.GreaterThan        => Query.Range(new NumberRangeQuery(node.Field) { Gt  = ToDouble(node.Value) }),
            ComparisonOp.GreaterThanOrEqual => Query.Range(new NumberRangeQuery(node.Field) { Gte = ToDouble(node.Value) }),
            ComparisonOp.LessThan           => Query.Range(new NumberRangeQuery(node.Field) { Lt  = ToDouble(node.Value) }),
            ComparisonOp.LessThanOrEqual    => Query.Range(new NumberRangeQuery(node.Field) { Lte = ToDouble(node.Value) }),
            _ => throw new NotSupportedException($"ComparisonOp.{node.Op} is not mapped.")
        };

        public Query VisitLike(LikeNode node) => Query.Wildcard(new WildcardQuery(node.Field)
        {
            Value           = BuildWildcardPattern(node.Pattern, node.Op),
            CaseInsensitive = true
        });

        public Query VisitIn(InNode node)
        {
            // Empty IN → always false (must_not match_all)
            if (node.Values.Count == 0)
                return Query.Bool(new BoolQuery
                {
                    MustNot = [Query.MatchAll(new MatchAllQuery())]
                });

            return Query.Terms(new TermsQuery
            {
                Field = node.Field,
                Term  = new TermsQueryField(node.Values.Select(v => ToFieldValue(v)).ToArray())
            });
        }

        public Query VisitNull(NullNode node) =>
            // IsNull  → field must not exist (or be explicitly null, treated as missing in ES)
            // IsNotNull → field must exist
            node.Check == NullCheckOp.IsNull
                ? Query.Bool(new BoolQuery
                {
                    MustNot = [Query.Exists(new ExistsQuery { Field = node.Field })]
                })
                : Query.Exists(new ExistsQuery { Field = node.Field });

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

        private FieldValue ToFieldValue(object? value) =>
            ConditionValueResolver.Resolve(value, customConverter, v => v switch
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
                _             => FieldValue.String(v!.ToString()!)
            });
    }
}
