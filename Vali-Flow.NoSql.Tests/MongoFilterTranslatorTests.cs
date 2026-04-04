using System.Linq.Expressions;
using MongoDB.Bson;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.MongoDB.Extensions;
using Vali_Flow.NoSql.MongoDB.Translators;
using Vali_Flow.NoSql.Tests.Models;

namespace Vali_Flow.NoSql.Tests;

public sealed class MongoFilterTranslatorTests
{
    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToMongo_EqualString_ProducesSimpleFieldMatch()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice";

        BsonDocument doc = expr.ToMongo();

        doc["Name"].AsString.Should().Be("Alice");
    }

    [Fact]
    public void ToMongo_NotEqualString_ProducesNeOperator()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name != "Bob";

        BsonDocument doc = expr.ToMongo();

        doc["Name"].AsBsonDocument["$ne"].AsString.Should().Be("Bob");
    }

    [Fact]
    public void ToMongo_EqualBool_ProducesBoolField()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive == true;

        BsonDocument doc = expr.ToMongo();

        doc["IsActive"].AsBoolean.Should().BeTrue();
    }

    // ── Null checks ───────────────────────────────────────────────────────────

    [Fact]
    public void ToMongo_IsNull_ProducesNullValue()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email == null;

        BsonDocument doc = expr.ToMongo();

        doc["Email"].Should().Be(BsonNull.Value);
    }

    [Fact]
    public void ToMongo_IsNotNull_ProducesNeNull()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email != null;

        BsonDocument doc = expr.ToMongo();

        doc["Email"].AsBsonDocument["$ne"].Should().Be(BsonNull.Value);
    }

    // ── Comparison ────────────────────────────────────────────────────────────

    [Fact]
    public void ToMongo_GreaterThan_ProducesGtOperator()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age > 18;

        BsonDocument doc = expr.ToMongo();

        doc["Age"].AsBsonDocument["$gt"].AsInt32.Should().Be(18);
    }

    [Fact]
    public void ToMongo_GreaterThanOrEqual_ProducesGteOperator()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age >= 21;

        BsonDocument doc = expr.ToMongo();

        doc["Age"].AsBsonDocument["$gte"].AsInt32.Should().Be(21);
    }

    [Fact]
    public void ToMongo_LessThan_ProducesLtOperator()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age < 65;

        BsonDocument doc = expr.ToMongo();

        doc["Age"].AsBsonDocument["$lt"].AsInt32.Should().Be(65);
    }

    [Fact]
    public void ToMongo_LessThanOrEqual_ProducesLteOperator()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age <= 60;

        BsonDocument doc = expr.ToMongo();

        doc["Age"].AsBsonDocument["$lte"].AsInt32.Should().Be(60);
    }

    // ── Pattern match ─────────────────────────────────────────────────────────

    [Fact]
    public void ToMongo_Contains_ProducesRegexWithPattern()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.Contains("ali");

        BsonDocument doc = expr.ToMongo();

        var regex = doc["Name"].AsBsonDocument["$regex"].AsBsonRegularExpression;
        regex.Pattern.Should().Be("ali");
        regex.Options.Should().Be("i");
    }

    [Fact]
    public void ToMongo_StartsWith_ProducesRegexWithCaret()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.StartsWith("Al");

        BsonDocument doc = expr.ToMongo();

        var regex = doc["Name"].AsBsonDocument["$regex"].AsBsonRegularExpression;
        regex.Pattern.Should().StartWith("^");
        regex.Pattern.Should().Contain("Al");
    }

    [Fact]
    public void ToMongo_EndsWith_ProducesRegexWithDollar()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.EndsWith("ce");

        BsonDocument doc = expr.ToMongo();

        var regex = doc["Name"].AsBsonDocument["$regex"].AsBsonRegularExpression;
        regex.Pattern.Should().EndWith("$");
        regex.Pattern.Should().Contain("ce");
    }

    [Fact]
    public void ToMongo_ContainsSpecialChars_EscapesRegexPattern()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.Contains("a.b*c");

        BsonDocument doc = expr.ToMongo();

        var regex = doc["Name"].AsBsonDocument["$regex"].AsBsonRegularExpression;
        regex.Pattern.Should().Be(@"a\.b\*c");
    }

    // ── Membership ────────────────────────────────────────────────────────────

    [Fact]
    public void ToMongo_ListContains_ProducesInOperator()
    {
        var ids = new List<int> { 1, 2, 3 };
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        BsonDocument doc = expr.ToMongo();

        var inArray = doc["Id"].AsBsonDocument["$in"].AsBsonArray;
        inArray.Select(v => v.AsInt32).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void ToMongo_EmptyListContains_ProducesEmptyInArray()
    {
        var ids = new List<int>();
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        BsonDocument doc = expr.ToMongo();

        var inArray = doc["Id"].AsBsonDocument["$in"].AsBsonArray;
        inArray.Should().BeEmpty();
    }

    // ── Logical ───────────────────────────────────────────────────────────────

    [Fact]
    public void ToMongo_AndAlso_ProducesAndOperator()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive && x.Age > 18;

        BsonDocument doc = expr.ToMongo();

        var andArray = doc["$and"].AsBsonArray;
        andArray.Should().HaveCount(2);
    }

    [Fact]
    public void ToMongo_OrElse_ProducesOrOperator()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice" || x.Name == "Bob";

        BsonDocument doc = expr.ToMongo();

        var orArray = doc["$or"].AsBsonArray;
        orArray.Should().HaveCount(2);
    }

    [Fact]
    public void ToMongo_Not_ProducesNorOperator()
    {
        Expression<Func<TestDocument, bool>> expr = x => !(x.Age > 18);

        BsonDocument doc = expr.ToMongo();

        // $nor with a single element = NOT
        var norArray = doc["$nor"].AsBsonArray;
        norArray.Should().HaveCount(1);
        norArray[0].AsBsonDocument["Age"].AsBsonDocument["$gt"].AsInt32.Should().Be(18);
    }

    // ── ValiFlow<T> fluent pipeline ───────────────────────────────────────────

    [Fact]
    public void ToMongo_ValiFlowEqualTo_ProducesFieldMatch()
    {
        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.Category, "Fruit");

        BsonDocument doc = flow.ToMongo();

        doc["Category"].AsString.Should().Be("Fruit");
    }

    [Fact]
    public void ToMongo_ValiFlowCombined_ProducesAndDocument()
    {
        var flow = new ValiFlow<TestDocument>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThan(x => x.Age, 18);

        BsonDocument doc = flow.ToMongo();

        doc.Contains("$and").Should().BeTrue();
        doc["$and"].AsBsonArray.Should().HaveCount(2);
    }

    // ── Direct Translate() null guard ─────────────────────────────────────────

    [Fact]
    public void Translate_NullNode_ThrowsArgumentNullException()
    {
        Action act = () => MongoFilterTranslator.Translate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── OCP — CustomValueConverter ────────────────────────────────────────────

    [Fact]
    public void CustomValueConverter_WhenSet_UsedForMatchingType()
    {
        Func<object?, BsonValue?> converter = v => v is decimal d ? new BsonDecimal128(d) : null;

        var node = new EqualNode("Price", 9.99m, false);
        BsonDocument doc = MongoFilterTranslator.Translate(node, converter);

        doc["Price"].BsonType.Should().Be(BsonType.Decimal128);
    }

    [Fact]
    public void CustomValueConverter_WhenReturnsNull_FallsThroughToDefault()
    {
        Func<object?, BsonValue?> converter = _ => null;

        var node = new EqualNode("Name", "Alice", false);
        BsonDocument doc = MongoFilterTranslator.Translate(node, converter);

        doc["Name"].AsString.Should().Be("Alice");
    }

    // ── ToBsonValue type coverage ─────────────────────────────────────────────

    [Fact]
    public void ToMongo_IntValue_ProducesBsonInt32()
    {
        var node = new EqualNode("Age", 30, false);

        BsonDocument doc = MongoFilterTranslator.Translate(node);

        doc["Age"].BsonType.Should().Be(BsonType.Int32);
        doc["Age"].AsInt32.Should().Be(30);
    }

    [Fact]
    public void ToMongo_LongValue_ProducesBsonInt64()
    {
        var node = new EqualNode("Counter", 999_999_999_999L, false);

        BsonDocument doc = MongoFilterTranslator.Translate(node);

        doc["Counter"].BsonType.Should().Be(BsonType.Int64);
    }

    [Fact]
    public void ToMongo_GuidValue_ProducesBsonBinaryData()
    {
        var guid = Guid.NewGuid();
        var node = new EqualNode("Id", guid, false);

        BsonDocument doc = MongoFilterTranslator.Translate(node);

        doc["Id"].BsonType.Should().Be(BsonType.Binary);
    }

    [Fact]
    public void ToMongo_DateTimeValue_ProducesBsonDateTime()
    {
        var dt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var node = new EqualNode("CreatedAt", dt, false);

        BsonDocument doc = MongoFilterTranslator.Translate(node);

        doc["CreatedAt"].BsonType.Should().Be(BsonType.DateTime);
    }
}
