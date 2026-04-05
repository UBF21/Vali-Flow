using System.Linq.Expressions;
using Amazon.DynamoDBv2.Model;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.DynamoDB.Extensions;
using Vali_Flow.NoSql.DynamoDB.Models;
using Vali_Flow.NoSql.DynamoDB.Translators;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.DynamoDB.Tests.Models;

namespace Vali_Flow.NoSql.DynamoDB.Tests;

public sealed class DynamoFilterTranslatorTests
{
    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_EqualString_ProducesEqualExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice";

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeNames["#f0"].Should().Be("Name");
        f.ExpressionAttributeValues[":v0"].S.Should().Be("Alice");
    }

    [Fact]
    public void ToDynamoDB_NotEqualString_ProducesNotEqualExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name != "Bob";

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("#f0 <> :v0");
        f.ExpressionAttributeValues[":v0"].S.Should().Be("Bob");
    }

    [Fact]
    public void ToDynamoDB_EqualInt_ProducesNumericEqualExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age == 30;

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeValues[":v0"].N.Should().Be("30");
    }

    [Fact]
    public void ToDynamoDB_EqualBool_ProducesBoolExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive == true;

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeValues[":v0"].BOOL.Should().BeTrue();
    }

    // ── Bool member direct ────────────────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_BoolMemberDirect_ProducesBoolTrueExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive;

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeValues[":v0"].BOOL.Should().BeTrue();
    }

    // ── Null checks ───────────────────────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_IsNull_ProducesAttributeNotExists()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email == null;

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("attribute_not_exists(#f0)");
        f.ExpressionAttributeNames["#f0"].Should().Be("Email");
        f.ExpressionAttributeValues.Should().BeEmpty();
    }

    [Fact]
    public void ToDynamoDB_IsNotNull_ProducesAttributeExists()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email != null;

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("attribute_exists(#f0)");
        f.ExpressionAttributeNames["#f0"].Should().Be("Email");
    }

    // ── Range ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_GreaterThan_ProducesGtExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age > 18;

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("#f0 > :v0");
        f.ExpressionAttributeValues[":v0"].N.Should().Be("18");
    }

    [Fact]
    public void ToDynamoDB_GreaterThanOrEqual_ProducesGteExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age >= 21;

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("#f0 >= :v0");
        f.ExpressionAttributeValues[":v0"].N.Should().Be("21");
    }

    [Fact]
    public void ToDynamoDB_LessThan_ProducesLtExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age < 65;

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("#f0 < :v0");
    }

    [Fact]
    public void ToDynamoDB_LessThanOrEqual_ProducesLteExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age <= 60;

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("#f0 <= :v0");
        f.ExpressionAttributeValues[":v0"].N.Should().Be("60");
    }

    // ── Pattern match ─────────────────────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_StringContains_ProducesContainsFunction()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.Contains("Ali");

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("contains(#f0, :v0)");
        f.ExpressionAttributeNames["#f0"].Should().Be("Name");
        f.ExpressionAttributeValues[":v0"].S.Should().Be("Ali");
    }

    [Fact]
    public void ToDynamoDB_StringStartsWith_ProducesBeginsWith()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.StartsWith("Al");

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("begins_with(#f0, :v0)");
        f.ExpressionAttributeValues[":v0"].S.Should().Be("Al");
    }

    [Fact]
    public void ToDynamoDB_StringEndsWith_ThrowsNotSupportedException()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.EndsWith("ce");

        Action act = () => expr.ToDynamoDB();

        act.Should().Throw<NotSupportedException>();
    }

    // ── Membership ────────────────────────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_ListContains_ProducesInExpression()
    {
        var ids = new List<int> { 1, 2, 3 };
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("#f0 IN (:v0, :v1, :v2)");
        f.ExpressionAttributeValues[":v0"].N.Should().Be("1");
        f.ExpressionAttributeValues[":v1"].N.Should().Be("2");
        f.ExpressionAttributeValues[":v2"].N.Should().Be("3");
    }

    [Fact]
    public void ToDynamoDB_EmptyListContains_ProducesAlwaysFalse()
    {
        var ids = new List<int>();
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Contain("attribute_exists");
        f.FilterExpression.Should().Contain("attribute_not_exists");
        f.FilterExpression.Should().Contain("AND");
    }

    [Fact]
    public void ToDynamoDB_ListWithMoreThan100Items_ThrowsInvalidOperationException()
    {
        var ids = Enumerable.Range(1, 101).ToList();
        var node = new InNode("Id", ids.Cast<object?>().ToList());

        Action act = () => DynamoFilterTranslator.Translate(node);

        act.Should().Throw<InvalidOperationException>().WithMessage("*100*");
    }

    // ── Logical combinators ───────────────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_AndAlso_ProducesAndExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive && x.Age > 18;

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("(#f0 = :v0 AND #f1 > :v1)");
        f.ExpressionAttributeNames.Should().HaveCount(2);
        f.ExpressionAttributeValues.Should().HaveCount(2);
    }

    [Fact]
    public void ToDynamoDB_OrElse_ProducesOrExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice" || x.Name == "Bob";

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("(#f0 = :v0 OR #f1 = :v1)");
        f.ExpressionAttributeValues[":v0"].S.Should().Be("Alice");
        f.ExpressionAttributeValues[":v1"].S.Should().Be("Bob");
    }

    [Fact]
    public void ToDynamoDB_Not_ProducesNotExpression()
    {
        Expression<Func<TestDocument, bool>> expr = x => !(x.Age > 18);

        DynamoFilterExpression f = expr.ToDynamoDB();

        f.FilterExpression.Should().Be("NOT (#f0 > :v0)");
    }

    // ── ValiFlow<T> fluent pipeline ───────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_ValiFlowEqualTo_ProducesEqualExpression()
    {
        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.Category, "Fruit");

        DynamoFilterExpression f = flow.ToDynamoDB();

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeValues[":v0"].S.Should().Be("Fruit");
    }

    [Fact]
    public void ToDynamoDB_ValiFlowCombined_ProducesAndExpression()
    {
        var flow = new ValiFlow<TestDocument>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThan(x => x.Age, 18);

        DynamoFilterExpression f = flow.ToDynamoDB();

        f.FilterExpression.Should().Be("(#f0 = :v0 AND #f1 > :v1)");
    }

    // ── Null guards ───────────────────────────────────────────────────────────

    [Fact]
    public void Translate_NullNode_ThrowsArgumentNullException()
    {
        Action act = () => DynamoFilterTranslator.Translate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDynamoDB_ValiFlowNull_ThrowsArgumentNullException()
    {
        ValiFlow<TestDocument> flow = null!;

        Action act = () => flow.ToDynamoDB();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDynamoDB_ExpressionNull_ThrowsArgumentNullException()
    {
        Expression<Func<TestDocument, bool>> expr = null!;

        Action act = () => expr.ToDynamoDB();

        act.Should().Throw<ArgumentNullException>();
    }

    // ── OCP — CustomAttributeValueConverter ──────────────────────────────────

    [Fact]
    public void CustomAttributeValueConverter_WhenSet_UsedForMatchingType()
    {
        Func<object?, AttributeValue?> converter = v =>
            v is decimal d ? new AttributeValue { N = d.ToString("G", System.Globalization.CultureInfo.InvariantCulture) } : null;

        var node = new EqualNode("Price", 9.99m, false);
        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node, converter);

        f.ExpressionAttributeValues[":v0"].N.Should().Be("9.99");
    }

    [Fact]
    public void CustomAttributeValueConverter_WhenReturnsNull_FallsThroughToDefault()
    {
        Func<object?, AttributeValue?> converter = _ => null;

        var node = new EqualNode("Name", "Alice", false);
        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node, converter);

        f.ExpressionAttributeValues[":v0"].S.Should().Be("Alice");
    }

    // ── Direct node construction ──────────────────────────────────────────────

    [Fact]
    public void Translate_DirectEqualNode_ProducesCorrectExpression()
    {
        var node = new EqualNode("Status", "Active", false);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeNames["#f0"].Should().Be("Status");
        f.ExpressionAttributeValues[":v0"].S.Should().Be("Active");
    }

    [Fact]
    public void Translate_DirectAndNode_ProducesAndWithTwoAttributes()
    {
        var left  = new EqualNode("IsActive", true, false);
        var right = new ComparisonNode("Age", 18, ComparisonOp.GreaterThan);
        var node  = new AndNode(left, right);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().Be("(#f0 = :v0 AND #f1 > :v1)");
        f.ExpressionAttributeNames.Should().HaveCount(2);
    }

    [Fact]
    public void Translate_DecimalValue_ProducesNumberAttributeValue()
    {
        var node = new EqualNode("Price", 9.99m, false);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.ExpressionAttributeValues[":v0"].N.Should().Be("9.99");
    }

    // ── Type Coverage adicional ───────────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_GuidValue_ProducesStringAttribute()
    {
        var guid = Guid.NewGuid();
        var node = new EqualNode("Id", guid, false);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeValues[":v0"].S.Should().Be(guid.ToString());
    }

    [Fact]
    public void ToDynamoDB_FloatValue_ProducesNumberAttribute()
    {
        var node = new EqualNode("Score", 3.14f, false);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeValues[":v0"].N.Should()
            .Be(((float)3.14f).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToDynamoDB_DoubleValue_ProducesNumberAttribute()
    {
        var node = new EqualNode("Score", 3.14, false);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeValues[":v0"].N.Should()
            .Be(((double)3.14).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToDynamoDB_LongValue_ProducesNumberAttribute()
    {
        var node = new EqualNode("Score", 12345678901L, false);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeValues[":v0"].N.Should().Be("12345678901");
    }

    [Fact]
    public void ToDynamoDB_EnumValue_ProducesNumberAttribute()
    {
        var node = new EqualNode("Status", DynamoTestStatus.Active, false);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeValues[":v0"].N.Should().Be("1");
    }

    // ── Boolean false ─────────────────────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_EqualBoolFalse_ProducesBoolFalseAttribute()
    {
        var node = new EqualNode("IsActive", false, false);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().Be("#f0 = :v0");
        f.ExpressionAttributeValues[":v0"].BOOL.Should().BeFalse();
    }

    // ── Boundary: exactamente 100 items ──────────────────────────────────────

    [Fact]
    public void ToDynamoDB_ListWith100Items_ProducesValidInExpression()
    {
        var values = Enumerable.Range(1, 100).Select(i => (object?)i).ToList();
        var node   = new InNode("Id", values);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().StartWith("#f0 IN (");
        f.ExpressionAttributeValues.Should().HaveCount(100);
        f.ExpressionAttributeValues.Should().ContainKey(":v0");
        f.ExpressionAttributeValues.Should().ContainKey(":v99");
    }

    // ── Lógica anidada ────────────────────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_NestedAndOr_ProducesCorrectParentheses()
    {
        // (A AND B) OR C  →  ((A AND B) OR C)
        var a    = new EqualNode("IsActive", true, false);
        var b    = new ComparisonNode("Age", 18, ComparisonOp.GreaterThan);
        var c    = new EqualNode("Name", "Admin", false);
        var node = new OrNode(new AndNode(a, b), c);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().StartWith("(");
        f.FilterExpression.Should().Contain("AND");
        f.FilterExpression.Should().Contain("OR");
        // Inner AND must be parenthesised
        f.FilterExpression.Should().MatchRegex(@"\(.*AND.*\).*OR");
    }

    [Fact]
    public void ToDynamoDB_TripleNesting_ProducesValidExpression()
    {
        // NOT (A AND (B OR C))
        var a    = new EqualNode("IsActive", true, false);
        var b    = new ComparisonNode("Age", 18, ComparisonOp.GreaterThan);
        var c    = new EqualNode("Name", "Admin", false);
        var node = new NotNode(new AndNode(a, new OrNode(b, c)));

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().StartWith("NOT (");
        f.FilterExpression.Should().Contain("AND");
        f.FilterExpression.Should().Contain("OR");
    }

    // ── ExpressionAttributeNames deduplication ────────────────────────────────

    [Fact]
    public void ToDynamoDB_SameFieldTwice_UsesDifferentNamePlaceholders()
    {
        var left  = new EqualNode("Age", 18, false);
        var right = new ComparisonNode("Age", 16, ComparisonOp.GreaterThan);
        var node  = new AndNode(left, right);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        // Both placeholders must map to "Age"
        f.ExpressionAttributeNames.Should().ContainKey("#f0");
        f.ExpressionAttributeNames.Should().ContainKey("#f1");
        f.ExpressionAttributeNames["#f0"].Should().Be("Age");
        f.ExpressionAttributeNames["#f1"].Should().Be("Age");
        f.FilterExpression.Should().Contain("#f0");
        f.FilterExpression.Should().Contain("#f1");
    }

    // ── Range con decimal ─────────────────────────────────────────────────────

    [Fact]
    public void ToDynamoDB_DecimalGreaterThan_ProducesNumericComparison()
    {
        var node = new ComparisonNode("Price", 99.99m, ComparisonOp.GreaterThan);

        DynamoFilterExpression f = DynamoFilterTranslator.Translate(node);

        f.FilterExpression.Should().Be("#f0 > :v0");
        f.ExpressionAttributeNames["#f0"].Should().Be("Price");
        f.ExpressionAttributeValues[":v0"].N.Should().Be("99.99");
    }

    private enum DynamoTestStatus { Active = 1 }
}
