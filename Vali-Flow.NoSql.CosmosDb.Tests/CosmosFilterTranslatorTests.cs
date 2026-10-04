using System.Diagnostics;
using System.Linq.Expressions;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.CosmosDb.Extensions;
using Vali_Flow.NoSql.CosmosDb.Models;
using Vali_Flow.NoSql.CosmosDb.Translators;
using Vali_Flow.NoSql.CosmosDb.Tests.Models;
using Vali_Flow.NoSql.IR;

namespace Vali_Flow.NoSql.CosmosDb.Tests;

public sealed class CosmosFilterTranslatorTests
{
    private static ActivityListener AttachListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ValiFlowDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public void ToCosmosDb_WithTag_SetsTagAndEntityTypeOnActivity()
    {
        using var listener = AttachListener();
        Activity? captured = null;
        listener.ActivityStopped = a => captured = a;

        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.Name, "Alice");

        flow.ToCosmosDb(tag: "report-x");

        captured.Should().NotBeNull();
        captured!.GetTagItem("vali_flow.tag").Should().Be("report-x");
        captured.GetTagItem("vali_flow.entity_type").Should().Be(nameof(TestDocument));
    }

    [Fact]
    public void Translate_InExceedsMaxValues_SetsErrorStatusOnActivity()
    {
        using var listener = AttachListener();
        Activity? captured = null;
        listener.ActivityStopped = a => captured = a;

        var values = Enumerable.Range(0, 6_001).Cast<object?>().ToList();
        var node = new InNode("Age", values);

        Action act = () => CosmosFilterTranslator.Translate(node);

        act.Should().Throw<InvalidOperationException>();
        captured.Should().NotBeNull();
        captured!.Status.Should().Be(ActivityStatusCode.Error);
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToCosmosDb_EqualString_ProducesEqualClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice";

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("c.Name = @p0");
        f.Parameters["@p0"].Should().Be("Alice");
    }

    [Fact]
    public void ToCosmosDb_NotEqualString_ProducesNotEqualClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name != "Bob";

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("c.Name != @p0");
        f.Parameters["@p0"].Should().Be("Bob");
    }

    [Fact]
    public void ToCosmosDb_EqualInt_ProducesNumericEqualClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age == 30;

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("c.Age = @p0");
        f.Parameters["@p0"].Should().Be(30);
    }

    [Fact]
    public void ToCosmosDb_EqualBool_ProducesBoolClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive == true;

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("c.IsActive = @p0");
        f.Parameters["@p0"].Should().Be(true);
    }

    [Fact]
    public void ToCosmosDb_BoolMemberDirect_ProducesBoolTrueClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive;

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("c.IsActive = @p0");
        f.Parameters["@p0"].Should().Be(true);
    }

    // ── Null checks ───────────────────────────────────────────────────────────

    [Fact]
    public void ToCosmosDb_IsNull_ProducesIsNullFunction()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email == null;

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("IS_NULL(c.Email)");
        f.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void ToCosmosDb_IsNotNull_ProducesNotIsNullFunction()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email != null;

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("NOT IS_NULL(c.Email)");
    }

    // ── Range ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToCosmosDb_GreaterThan_ProducesGtClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age > 18;

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("c.Age > @p0");
        f.Parameters["@p0"].Should().Be(18);
    }

    [Fact]
    public void ToCosmosDb_GreaterThanOrEqual_ProducesGteClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age >= 21;

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("c.Age >= @p0");
    }

    [Fact]
    public void ToCosmosDb_LessThan_ProducesLtClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age < 65;

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("c.Age < @p0");
    }

    [Fact]
    public void ToCosmosDb_LessThanOrEqual_ProducesLteClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age <= 60;

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("c.Age <= @p0");
    }

    // ── Pattern match ─────────────────────────────────────────────────────────

    [Fact]
    public void ToCosmosDb_StringContains_ProducesContainsFunction()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.Contains("Ali");

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("CONTAINS(c.Name, @p0)");
        f.Parameters["@p0"].Should().Be("Ali");
    }

    [Fact]
    public void ToCosmosDb_StringStartsWith_ProducesStartsWithFunction()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.StartsWith("Al");

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("STARTSWITH(c.Name, @p0)");
    }

    [Fact]
    public void ToCosmosDb_StringEndsWith_ProducesEndsWithFunction()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.EndsWith("ce");

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("ENDSWITH(c.Name, @p0)");
        f.Parameters["@p0"].Should().Be("ce");
    }

    // ── Membership ────────────────────────────────────────────────────────────

    [Fact]
    public void ToCosmosDb_ListContains_ProducesInClauseWithOnePlaceholderPerValue()
    {
        var ids = new List<int> { 1, 2, 3 };
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("c.Id IN (@p0, @p1, @p2)");
        f.Parameters["@p0"].Should().Be(1);
        f.Parameters["@p1"].Should().Be(2);
        f.Parameters["@p2"].Should().Be(3);
    }

    [Fact]
    public void ToCosmosDb_EmptyListContains_ThrowsInvalidOperationException()
    {
        var ids = new List<int>();
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        Action act = () => expr.ToCosmosDb();

        act.Should().Throw<InvalidOperationException>().WithMessage("*empty*");
    }

    [Fact]
    public void ToCosmosDb_StringListContains_ProducesInClauseWithStringValues()
    {
        var categories = new List<string> { "Electronics", "Books", "Tools" };
        Expression<Func<TestDocument, bool>> expr = x => categories.Contains(x.Category);

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("c.Category IN (@p0, @p1, @p2)");
        f.Parameters["@p0"].Should().Be("Electronics");
    }

    [Fact]
    public void ToCosmosDb_ListWithMoreThan6000Items_ThrowsInvalidOperationException()
    {
        var ids = Enumerable.Range(1, 6001).ToList();
        var node = new InNode("Id", ids.Cast<object?>().ToList());

        Action act = () => CosmosFilterTranslator.Translate(node);

        act.Should().Throw<InvalidOperationException>().WithMessage("*6000*");
    }

    [Fact]
    public void ToCosmosDb_ListWithExactly6000Items_DoesNotThrow()
    {
        var ids = Enumerable.Range(1, 6000).ToList();
        var node = new InNode("Id", ids.Cast<object?>().ToList());

        Action act = () => CosmosFilterTranslator.Translate(node);

        act.Should().NotThrow();
    }

    // ── Logical combinators ───────────────────────────────────────────────────

    [Fact]
    public void ToCosmosDb_AndAlso_ProducesAndClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive && x.Age > 18;

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("(c.IsActive = @p0 AND c.Age > @p1)");
        f.Parameters.Should().HaveCount(2);
    }

    [Fact]
    public void ToCosmosDb_OrElse_ProducesOrClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice" || x.Name == "Bob";

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("(c.Name = @p0 OR c.Name = @p1)");
    }

    [Fact]
    public void ToCosmosDb_Not_ProducesNotClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => !(x.Age > 18);

        CosmosFilterExpression f = expr.ToCosmosDb();

        f.WhereClause.Should().Be("NOT (c.Age > @p0)");
    }

    [Fact]
    public void ToCosmosDb_NestedAndOr_ProducesCorrectParentheses()
    {
        var a    = new EqualNode("IsActive", true, false);
        var b    = new ComparisonNode("Age", 18, ComparisonOp.GreaterThan);
        var c    = new EqualNode("Name", "Admin", false);
        var node = new OrNode(new AndNode(a, b), c);

        CosmosFilterExpression f = CosmosFilterTranslator.Translate(node);

        f.WhereClause.Should().StartWith("(");
        f.WhereClause.Should().Contain("AND");
        f.WhereClause.Should().Contain("OR");
        f.WhereClause.Should().MatchRegex(@"\(.*AND.*\).*OR");
    }

    // ── ValiFlow<T> fluent pipeline ───────────────────────────────────────────

    [Fact]
    public void ToCosmosDb_ValiFlowEqualTo_ProducesEqualClause()
    {
        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.Category, "Fruit");

        CosmosFilterExpression f = flow.ToCosmosDb();

        f.WhereClause.Should().Be("c.Category = @p0");
        f.Parameters["@p0"].Should().Be("Fruit");
    }

    [Fact]
    public void ToCosmosDb_ValiFlowCombined_ProducesAndClause()
    {
        var flow = new ValiFlow<TestDocument>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThan(x => x.Age, 18);

        CosmosFilterExpression f = flow.ToCosmosDb();

        f.WhereClause.Should().Be("(c.IsActive = @p0 AND c.Age > @p1)");
    }

    // ── Null guards ───────────────────────────────────────────────────────────

    [Fact]
    public void Translate_NullNode_ThrowsArgumentNullException()
    {
        Action act = () => CosmosFilterTranslator.Translate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToCosmosDb_ValiFlowNull_ThrowsArgumentNullException()
    {
        ValiFlow<TestDocument> flow = null!;

        Action act = () => flow.ToCosmosDb();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToCosmosDb_ExpressionNull_ThrowsArgumentNullException()
    {
        Expression<Func<TestDocument, bool>> expr = null!;

        Action act = () => expr.ToCosmosDb();

        act.Should().Throw<ArgumentNullException>();
    }

    // ── OCP — custom converter ────────────────────────────────────────────────

    [Fact]
    public void CustomConverter_WhenSet_UsedForMatchingType()
    {
        Func<object?, object?> converter = v => v is decimal d ? d.ToString("G", System.Globalization.CultureInfo.InvariantCulture) : null;

        var node = new EqualNode("Price", 9.99m, false);
        CosmosFilterExpression f = CosmosFilterTranslator.Translate(node, converter);

        f.Parameters["@p0"].Should().Be("9.99");
    }

    [Fact]
    public void CustomConverter_WhenReturnsNull_FallsThroughToDefault()
    {
        Func<object?, object?> converter = _ => null;

        var node = new EqualNode("Name", "Alice", false);
        CosmosFilterExpression f = CosmosFilterTranslator.Translate(node, converter);

        f.Parameters["@p0"].Should().Be("Alice");
    }

    // ── Direct node construction / type coverage ──────────────────────────────

    [Fact]
    public void Translate_DirectEqualNode_ProducesCorrectClause()
    {
        var node = new EqualNode("Status", "Active", false);

        CosmosFilterExpression f = CosmosFilterTranslator.Translate(node);

        f.WhereClause.Should().Be("c.Status = @p0");
        f.Parameters["@p0"].Should().Be("Active");
    }

    [Fact]
    public void ToCosmosDb_GuidValue_ProducesStringParameter()
    {
        var guid = Guid.NewGuid();
        var node = new EqualNode("Id", guid, false);

        CosmosFilterExpression f = CosmosFilterTranslator.Translate(node);

        f.Parameters["@p0"].Should().Be(guid.ToString());
    }

    [Fact]
    public void ToCosmosDb_EnumValue_ProducesLongParameter()
    {
        var node = new EqualNode("Status", CosmosTestStatus.Active, false);

        CosmosFilterExpression f = CosmosFilterTranslator.Translate(node);

        f.Parameters["@p0"].Should().Be(1L);
    }

    [Fact]
    public void ToCosmosDb_DecimalGreaterThan_ProducesNumericComparison()
    {
        var node = new ComparisonNode("Price", 99.99m, ComparisonOp.GreaterThan);

        CosmosFilterExpression f = CosmosFilterTranslator.Translate(node);

        f.WhereClause.Should().Be("c.Price > @p0");
        f.Parameters["@p0"].Should().Be(99.99m);
    }

    [Fact]
    public void ToCosmosDb_FallbackTypeObject_ProducesStringParameter()
    {
        var node = new EqualNode("Misc", new Uri("https://example.com"), false);

        CosmosFilterExpression f = CosmosFilterTranslator.Translate(node);

        f.Parameters["@p0"].Should().Be("https://example.com/");
    }

    private enum CosmosTestStatus { Active = 1 }
}
