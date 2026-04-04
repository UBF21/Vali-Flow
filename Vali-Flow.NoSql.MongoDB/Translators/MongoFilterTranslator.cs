using System.Text.RegularExpressions;
using MongoDB.Bson;
using Vali_Flow.NoSql.IR;

namespace Vali_Flow.NoSql.MongoDB.Translators;

/// <summary>
/// Translates a <see cref="IConditionNode"/> IR tree into a MongoDB <see cref="BsonDocument"/> filter.
/// </summary>
/// <remarks>
/// The output <see cref="BsonDocument"/> is natively accepted anywhere MongoDB expects a filter —
/// <c>FilterDefinition&lt;T&gt;</c> has an implicit conversion from <c>BsonDocument</c>, so
/// <c>collection.Find(filter.ToMongo())</c> works without any explicit cast.
///
/// Field names are taken directly from the IR node (which mirrors the .NET property name).
/// To use a custom MongoDB field name, apply <c>[BsonElement("field")]</c> on your entity properties.
/// </remarks>
public static class MongoFilterTranslator
{
    /// <summary>
    /// Optional hook for converting custom CLR types to <see cref="BsonValue"/>.
    /// When set, it is called before the built-in type switch in <see cref="ToBsonValue"/>.
    /// Return <c>null</c> to fall through to the default conversion.
    /// </summary>
    /// <example>
    /// <code>
    /// MongoFilterTranslator.CustomValueConverter = value =>
    ///     value is Money m ? new BsonDecimal128(m.Amount) : null;
    /// </code>
    /// </example>
    public static Func<object?, BsonValue?>? CustomValueConverter { get; set; }

    /// <summary>
    /// Translates the given <see cref="IConditionNode"/> into a MongoDB <see cref="BsonDocument"/> filter.
    /// </summary>
    /// <param name="node">The root condition node to translate.</param>
    /// <returns>
    /// A <see cref="BsonDocument"/> representing the filter.
    /// Pass directly to <c>collection.Find()</c>, <c>CountDocuments()</c>, etc.
    /// </returns>
    /// <example>
    /// <code>
    /// BsonDocument filter = filter.ToMongo();
    /// var users = await collection.Find(filter).ToListAsync();
    /// </code>
    /// </example>
    public static BsonDocument Translate(IConditionNode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        return TranslateNode(node);
    }

    private static BsonDocument TranslateNode(IConditionNode node) => node switch
    {
        // ── Logical combinators ───────────────────────────────────────
        AndNode and =>
            new BsonDocument("$and", new BsonArray
            {
                TranslateNode(and.Left),
                TranslateNode(and.Right)
            }),

        OrNode or =>
            new BsonDocument("$or", new BsonArray
            {
                TranslateNode(or.Left),
                TranslateNode(or.Right)
            }),

        // $nor with a single element = logical NOT
        NotNode not =>
            new BsonDocument("$nor", new BsonArray { TranslateNode(not.Inner) }),

        // ── Null checks ───────────────────────────────────────────────
        NullNode { Check: NullCheckOp.IsNull } n =>
            new BsonDocument(n.Field, BsonNull.Value),

        NullNode { Check: NullCheckOp.IsNotNull } n =>
            new BsonDocument(n.Field, new BsonDocument("$ne", BsonNull.Value)),

        // ── Equality ──────────────────────────────────────────────────
        EqualNode { IsNegated: false } eq =>
            new BsonDocument(eq.Field, ToBsonValue(eq.Value)),

        EqualNode { IsNegated: true } eq =>
            new BsonDocument(eq.Field, new BsonDocument("$ne", ToBsonValue(eq.Value))),

        // ── Comparison ────────────────────────────────────────────────
        ComparisonNode cmp =>
            new BsonDocument(cmp.Field, new BsonDocument(
                cmp.Op switch
                {
                    ComparisonOp.GreaterThan        => "$gt",
                    ComparisonOp.GreaterThanOrEqual => "$gte",
                    ComparisonOp.LessThan           => "$lt",
                    ComparisonOp.LessThanOrEqual    => "$lte",
                    _ => throw new NotSupportedException($"ComparisonOp.{cmp.Op} is not mapped.")
                },
                ToBsonValue(cmp.Value))),

        // ── Pattern match (regex) ─────────────────────────────────────
        LikeNode like =>
            new BsonDocument(like.Field, new BsonDocument("$regex",
                new BsonRegularExpression(BuildRegexPattern(like.Pattern, like.Op), "i"))),

        // ── Membership ────────────────────────────────────────────────
        // {field: {$in: []}} → MongoDB returns zero documents (empty set = always false)
        InNode inNode =>
            new BsonDocument(inNode.Field,
                new BsonDocument("$in",
                    new BsonArray(inNode.Values.Select(ToBsonValue)))),

        _ => throw new NotSupportedException(
            $"IR node type '{node.GetType().Name}' is not supported by MongoFilterTranslator.")
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildRegexPattern(string rawPattern, LikeOp op) => op switch
    {
        LikeOp.Contains   => Regex.Escape(rawPattern),
        LikeOp.StartsWith => $"^{Regex.Escape(rawPattern)}",
        LikeOp.EndsWith   => $"{Regex.Escape(rawPattern)}$",
        _ => throw new NotSupportedException($"LikeOp.{op} is not mapped.")
    };

    private static BsonValue ToBsonValue(object? value)
    {
        if (CustomValueConverter != null)
        {
            var custom = CustomValueConverter(value);
            if (custom != null) return custom;
        }

        return value switch
        {
            null               => BsonNull.Value,
            bool b             => new BsonBoolean(b),
            int i              => new BsonInt32(i),
            long l             => new BsonInt64(l),
            double d           => new BsonDouble(d),
            decimal dec        => new BsonDecimal128(dec),
            float f            => new BsonDouble(f),
            string s           => new BsonString(s),
            DateTime dt        => new BsonDateTime(dt),
            DateTimeOffset dto => new BsonDateTime(dto.UtcDateTime),
            Guid g             => new BsonBinaryData(g, GuidRepresentation.Standard),
            Enum e             => BsonValue.Create(Convert.ToInt32(e)),
            _                  => BsonValue.Create(value)
        };
    }
}
