using System.Text.RegularExpressions;
using MongoDB.Bson;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.Translators;

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
    /// Translates the given <see cref="IConditionNode"/> into a MongoDB <see cref="BsonDocument"/> filter.
    /// </summary>
    /// <param name="node">The root condition node to translate.</param>
    /// <param name="customConverter">
    /// Optional hook for converting custom CLR types to <see cref="BsonValue"/>.
    /// Called before the built-in type switch. Return <c>null</c> to fall through to the default conversion.
    /// Thread-safe: the converter is scoped to this call only.
    /// </param>
    /// <returns>
    /// A <see cref="BsonDocument"/> representing the filter.
    /// Pass directly to <c>collection.Find()</c>, <c>CountDocuments()</c>, etc.
    /// </returns>
    /// <example>
    /// <code>
    /// BsonDocument filter = filter.ToMongo(v => v is Money m ? new BsonDecimal128(m.Amount) : null);
    /// var users = await collection.Find(filter).ToListAsync();
    /// </code>
    /// </example>
    public static BsonDocument Translate(IConditionNode node, Func<object?, BsonValue?>? customConverter = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        return node.Accept(new MongoVisitor(customConverter));
    }

    private sealed class MongoVisitor(Func<object?, BsonValue?>? customConverter) : IConditionNodeVisitor<BsonDocument>
    {
        public BsonDocument VisitAnd(AndNode node) =>
            new BsonDocument("$and", new BsonArray
            {
                node.Left.Accept(this),
                node.Right.Accept(this)
            });

        public BsonDocument VisitOr(OrNode node) =>
            new BsonDocument("$or", new BsonArray
            {
                node.Left.Accept(this),
                node.Right.Accept(this)
            });

        // $nor with a single element = logical NOT
        public BsonDocument VisitNot(NotNode node) =>
            new BsonDocument("$nor", new BsonArray { node.Inner.Accept(this) });

        public BsonDocument VisitEqual(EqualNode node) =>
            node.IsNegated
                ? new BsonDocument(node.Field, new BsonDocument("$ne", ToBsonValue(node.Value)))
                : new BsonDocument(node.Field, ToBsonValue(node.Value));

        public BsonDocument VisitComparison(ComparisonNode node) =>
            new BsonDocument(node.Field, new BsonDocument(
                node.Op switch
                {
                    ComparisonOp.GreaterThan        => "$gt",
                    ComparisonOp.GreaterThanOrEqual => "$gte",
                    ComparisonOp.LessThan           => "$lt",
                    ComparisonOp.LessThanOrEqual    => "$lte",
                    _ => throw new NotSupportedException($"ComparisonOp.{node.Op} is not mapped.")
                },
                ToBsonValue(node.Value)));

        public BsonDocument VisitLike(LikeNode node) =>
            new BsonDocument(node.Field, new BsonDocument("$regex",
                new BsonRegularExpression(BuildRegexPattern(node.Pattern, node.Op), node.CaseSensitive ? "" : "i")));

        public BsonDocument VisitIn(InNode node) =>
            new BsonDocument(node.Field,
                new BsonDocument("$in",
                    new BsonArray(node.Values.Select(v => ToBsonValue(v)))));

        public BsonDocument VisitNull(NullNode node) =>
            node.Check == NullCheckOp.IsNull
                ? new BsonDocument(node.Field, BsonNull.Value)
                : new BsonDocument(node.Field, new BsonDocument("$ne", BsonNull.Value));

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string BuildRegexPattern(string rawPattern, LikeOp op) => op switch
        {
            LikeOp.Contains   => Regex.Escape(rawPattern),
            LikeOp.StartsWith => $"^{Regex.Escape(rawPattern)}",
            LikeOp.EndsWith   => $"{Regex.Escape(rawPattern)}$",
            _ => throw new NotSupportedException($"LikeOp.{op} is not mapped.")
        };

        private BsonValue ToBsonValue(object? value) =>
            ConditionValueResolver.Resolve(value, customConverter, v => v switch
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
                _                  => BsonValue.Create(v)
            });
    }
}
