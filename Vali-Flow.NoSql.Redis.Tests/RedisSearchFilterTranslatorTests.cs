using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.Redis.Extensions;
using Vali_Flow.NoSql.Redis.Translators;
using Vali_Flow.NoSql.Redis.Tests.Models;

namespace Vali_Flow.NoSql.Redis.Tests;

public sealed class RedisSearchFilterTranslatorTests
{
    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToRedisSearch_EqualString_ProducesQuotedTagQuery()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice";

        string q = expr.ToRedisSearch();

        q.Should().Be(@"@Name:{""Alice""}");
    }

    [Fact]
    public void ToRedisSearch_NotEqualString_ProducesNegatedTagQuery()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name != "Bob";

        string q = expr.ToRedisSearch();

        q.Should().Be(@"-@Name:{""Bob""}");
    }

    [Fact]
    public void ToRedisSearch_EqualInt_ProducesNumericRange()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age == 30;

        string q = expr.ToRedisSearch();

        q.Should().Be("@Age:[30 30]");
    }

    [Fact]
    public void ToRedisSearch_NotEqualInt_ProducesNegatedNumericRange()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age != 30;

        string q = expr.ToRedisSearch();

        q.Should().Be("(-@Age:[30 30])");
    }

    [Fact]
    public void ToRedisSearch_EqualBoolTrue_ProducesNumericRange1()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive == true;

        string q = expr.ToRedisSearch();

        q.Should().Be("@IsActive:[1 1]");
    }

    // ── Bool member direct ────────────────────────────────────────────────────

    [Fact]
    public void ToRedisSearch_BoolMemberDirect_ProducesNumericRange1()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive;

        string q = expr.ToRedisSearch();

        q.Should().Be("@IsActive:[1 1]");
    }

    // ── Null checks ───────────────────────────────────────────────────────────

    [Fact]
    public void ToRedisSearch_IsNull_ThrowsNotSupportedException()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email == null;

        Action act = () => expr.ToRedisSearch();

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ToRedisSearch_IsNotNull_ThrowsNotSupportedException()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email != null;

        Action act = () => expr.ToRedisSearch();

        act.Should().Throw<NotSupportedException>();
    }

    // ── Range ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToRedisSearch_GreaterThan_ProducesExclusiveLowerBound()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age > 18;

        string q = expr.ToRedisSearch();

        q.Should().Be("@Age:[(18 +inf]");
    }

    [Fact]
    public void ToRedisSearch_GreaterThanOrEqual_ProducesInclusiveLowerBound()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age >= 21;

        string q = expr.ToRedisSearch();

        q.Should().Be("@Age:[21 +inf]");
    }

    [Fact]
    public void ToRedisSearch_LessThan_ProducesExclusiveUpperBound()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age < 65;

        string q = expr.ToRedisSearch();

        q.Should().Be("@Age:[-inf (65]");
    }

    [Fact]
    public void ToRedisSearch_LessThanOrEqual_ProducesInclusiveUpperBound()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age <= 60;

        string q = expr.ToRedisSearch();

        q.Should().Be("@Age:[-inf 60]");
    }

    [Fact]
    public void ToRedisSearch_ConstantOnLeft_FlipsRange()
    {
        // 18 < x.Age → x.Age > 18
        Expression<Func<TestDocument, bool>> expr = x => 18 < x.Age;

        string q = expr.ToRedisSearch();

        q.Should().Be("@Age:[(18 +inf]");
    }

    // ── Pattern match ─────────────────────────────────────────────────────────

    [Fact]
    public void ToRedisSearch_StringContains_ProducesWildcardBothSides()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.Contains("ali");

        string q = expr.ToRedisSearch();

        q.Should().Be("@Name:*ali*");
    }

    [Fact]
    public void ToRedisSearch_StringStartsWith_ProducesTrailingWildcard()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.StartsWith("Al");

        string q = expr.ToRedisSearch();

        q.Should().Be("@Name:Al*");
    }

    [Fact]
    public void ToRedisSearch_StringEndsWith_ProducesLeadingWildcard()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.EndsWith("ce");

        string q = expr.ToRedisSearch();

        q.Should().Be("@Name:*ce");
    }

    // ── Membership ────────────────────────────────────────────────────────────

    [Fact]
    public void ToRedisSearch_StringListContains_ProducesTagOrQuery()
    {
        var cats = new List<string> { "Fruit", "Vegetable" };
        Expression<Func<TestDocument, bool>> expr = x => cats.Contains(x.Category);

        string q = expr.ToRedisSearch();

        q.Should().Be(@"@Category:{""Fruit""|""Vegetable""}");
    }

    [Fact]
    public void ToRedisSearch_IntListContains_ProducesNumericOrQuery()
    {
        var ids = new List<int> { 1, 2, 3 };
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        string q = expr.ToRedisSearch();

        q.Should().Be("(@Id:[1 1]|@Id:[2 2]|@Id:[3 3])");
    }

    [Fact]
    public void ToRedisSearch_EmptyListContains_ProducesAlwaysFalse()
    {
        var ids = new List<int>();
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        string q = expr.ToRedisSearch();

        q.Should().Be("(-*)");
    }

    // ── Logical combinators ───────────────────────────────────────────────────

    [Fact]
    public void ToRedisSearch_AndAlso_ProducesImplicitAndQuery()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive && x.Age > 18;

        string q = expr.ToRedisSearch();

        q.Should().Be("(@IsActive:[1 1] @Age:[(18 +inf])");
    }

    [Fact]
    public void ToRedisSearch_OrElse_ProducesPipeQuery()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice" || x.Name == "Bob";

        string q = expr.ToRedisSearch();

        q.Should().Be(@"(@Name:{""Alice""} | @Name:{""Bob""})");
    }

    [Fact]
    public void ToRedisSearch_Not_ProducesNegatedGroup()
    {
        Expression<Func<TestDocument, bool>> expr = x => !(x.Age > 18);

        string q = expr.ToRedisSearch();

        q.Should().Be("-(@Age:[(18 +inf])");
    }

    // ── ValiFlow<T> fluent pipeline ───────────────────────────────────────────

    [Fact]
    public void ToRedisSearch_ValiFlowEqualTo_ProducesTagQuery()
    {
        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.Category, "Fruit");

        string q = flow.ToRedisSearch();

        q.Should().Be(@"@Category:{""Fruit""}");
    }

    [Fact]
    public void ToRedisSearch_ValiFlowCombined_ProducesAndQuery()
    {
        var flow = new ValiFlow<TestDocument>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThan(x => x.Age, 18);

        string q = flow.ToRedisSearch();

        q.Should().Be("(@IsActive:[1 1] @Age:[(18 +inf])");
    }

    // ── Null guards ───────────────────────────────────────────────────────────

    [Fact]
    public void Translate_NullNode_ThrowsArgumentNullException()
    {
        Action act = () => RedisSearchFilterTranslator.Translate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToRedisSearch_ValiFlowNull_ThrowsArgumentNullException()
    {
        ValiFlow<TestDocument> flow = null!;

        Action act = () => flow.ToRedisSearch();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToRedisSearch_ExpressionNull_ThrowsArgumentNullException()
    {
        Expression<Func<TestDocument, bool>> expr = null!;

        Action act = () => expr.ToRedisSearch();

        act.Should().Throw<ArgumentNullException>();
    }

    // ── OCP — CustomValueConverter ────────────────────────────────────────────

    [Fact]
    public void CustomValueConverter_WhenSet_UsedForMatchingType()
    {
        RedisSearchFilterTranslator.CustomValueConverter = v =>
            v is decimal d ? d.ToString("G", System.Globalization.CultureInfo.InvariantCulture) : null;

        try
        {
            var node = new EqualNode("Price", 9.99m, false);
            string q = RedisSearchFilterTranslator.Translate(node);

            q.Should().Be(@"@Price:{""9.99""}");
        }
        finally
        {
            RedisSearchFilterTranslator.CustomValueConverter = null;
        }
    }

    [Fact]
    public void CustomValueConverter_WhenReturnsNull_FallsThroughToDefault()
    {
        RedisSearchFilterTranslator.CustomValueConverter = _ => null;

        try
        {
            var node = new EqualNode("Name", "Alice", false);
            string q = RedisSearchFilterTranslator.Translate(node);

            q.Should().Be(@"@Name:{""Alice""}");
        }
        finally
        {
            RedisSearchFilterTranslator.CustomValueConverter = null;
        }
    }

    // ── Direct node construction ──────────────────────────────────────────────

    [Fact]
    public void Translate_DirectEqualNode_ProducesTagQuery()
    {
        var node = new EqualNode("Status", "Active", false);

        string q = RedisSearchFilterTranslator.Translate(node);

        q.Should().Be(@"@Status:{""Active""}");
    }

    [Fact]
    public void Translate_DirectAndNode_ProducesImplicitAnd()
    {
        var left  = new EqualNode("IsActive", true, false);
        var right = new ComparisonNode("Age", 18, ComparisonOp.GreaterThan);
        var node  = new AndNode(left, right);

        string q = RedisSearchFilterTranslator.Translate(node);

        q.Should().Be("(@IsActive:[1 1] @Age:[(18 +inf])");
    }

    [Fact]
    public void Translate_DecimalEqualNode_ProducesNumericRange()
    {
        var node = new EqualNode("Price", 9.99m, false);

        string q = RedisSearchFilterTranslator.Translate(node);

        q.Should().Be("@Price:[9.99 9.99]");
    }
}
