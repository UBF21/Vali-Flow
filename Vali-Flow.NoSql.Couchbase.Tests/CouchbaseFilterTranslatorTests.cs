using System.Diagnostics;
using System.Linq.Expressions;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Couchbase.Extensions;
using Vali_Flow.NoSql.Couchbase.Models;
using Vali_Flow.NoSql.Couchbase.Translators;
using Vali_Flow.NoSql.Couchbase.Tests.Models;
using Vali_Flow.NoSql.IR;

namespace Vali_Flow.NoSql.Couchbase.Tests;

public sealed class CouchbaseFilterTranslatorTests
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
    public void ToCouchbase_WithTag_SetsTagAndEntityTypeOnActivity()
    {
        using var listener = AttachListener();
        Activity? captured = null;
        listener.ActivityStopped = a => captured = a;

        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.Name, "Alice");

        flow.ToCouchbase(tag: "report-x");

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

        var values = Enumerable.Range(0, 10_001).Cast<object?>().ToList();
        var node = new InNode("Age", values);

        Action act = () => CouchbaseFilterTranslator.Translate(node);

        act.Should().Throw<InvalidOperationException>();
        captured.Should().NotBeNull();
        captured!.Status.Should().Be(ActivityStatusCode.Error);
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToCouchbase_EqualString_ProducesEqualClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice";

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Name` = $p0");
        f.Parameters["$p0"].Should().Be("Alice");
    }

    [Fact]
    public void ToCouchbase_NotEqualString_ProducesNotEqualClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name != "Bob";

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Name` <> $p0");
        f.Parameters["$p0"].Should().Be("Bob");
    }

    [Fact]
    public void ToCouchbase_EqualInt_ProducesEqualClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age == 30;

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Age` = $p0");
        f.Parameters["$p0"].Should().Be(30);
    }

    [Fact]
    public void ToCouchbase_EqualBool_ProducesEqualClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive == true;

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`IsActive` = $p0");
        f.Parameters["$p0"].Should().Be(true);
    }

    // ── Null checks ───────────────────────────────────────────────────────────

    [Fact]
    public void ToCouchbase_IsNull_ProducesIsNullClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email == null;

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Email` IS NULL");
        f.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void ToCouchbase_IsNotNull_ProducesIsNotNullClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email != null;

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Email` IS NOT NULL");
    }

    // ── Range ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToCouchbase_GreaterThan_ProducesGtClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age > 18;

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Age` > $p0");
        f.Parameters["$p0"].Should().Be(18);
    }

    [Fact]
    public void ToCouchbase_GreaterThanOrEqual_ProducesGteClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age >= 21;

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Age` >= $p0");
    }

    [Fact]
    public void ToCouchbase_LessThan_ProducesLtClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age < 65;

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Age` < $p0");
    }

    [Fact]
    public void ToCouchbase_LessThanOrEqual_ProducesLteClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age <= 60;

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Age` <= $p0");
    }

    // ── Pattern match ─────────────────────────────────────────────────────────

    [Fact]
    public void ToCouchbase_StringContains_ProducesLikeWithBothWildcards()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.Contains("Ali");

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Name` LIKE $p0");
        f.Parameters["$p0"].Should().Be("%Ali%");
    }

    [Fact]
    public void ToCouchbase_StringStartsWith_ProducesLikeWithTrailingWildcard()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.StartsWith("Al");

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Name` LIKE $p0");
        f.Parameters["$p0"].Should().Be("Al%");
    }

    [Fact]
    public void ToCouchbase_StringEndsWith_ProducesLikeWithLeadingWildcard()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.EndsWith("ce");

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Name` LIKE $p0");
        f.Parameters["$p0"].Should().Be("%ce");
    }

    // ── Membership ────────────────────────────────────────────────────────────

    [Fact]
    public void ToCouchbase_ListContains_ProducesInClauseWithArrayParameter()
    {
        var ids = new List<int> { 1, 2, 3 };
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("`Id` IN $p0");
        f.Parameters["$p0"].Should().BeEquivalentTo(new List<object?> { 1, 2, 3 });
    }

    [Fact]
    public void ToCouchbase_EmptyListContains_ThrowsInvalidOperationException()
    {
        var ids = new List<int>();
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        Action act = () => expr.ToCouchbase();

        act.Should().Throw<InvalidOperationException>().WithMessage("*empty*");
    }

    [Fact]
    public void ToCouchbase_ListWithMoreThan10000Items_ThrowsInvalidOperationException()
    {
        var ids = Enumerable.Range(1, 10_001).Cast<object?>().ToList();
        var node = new InNode("Id", ids);

        Action act = () => CouchbaseFilterTranslator.Translate(node);

        act.Should().Throw<InvalidOperationException>().WithMessage("*10000*");
    }

    [Fact]
    public void ToCouchbase_ListWithExactly10000Items_DoesNotThrow()
    {
        var ids = Enumerable.Range(1, 10_000).Cast<object?>().ToList();
        var node = new InNode("Id", ids);

        Action act = () => CouchbaseFilterTranslator.Translate(node);

        act.Should().NotThrow();
    }

    // ── Logical combinators ───────────────────────────────────────────────────

    [Fact]
    public void ToCouchbase_AndAlso_ProducesAndClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive && x.Age > 18;

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("(`IsActive` = $p0 AND `Age` > $p1)");
    }

    [Fact]
    public void ToCouchbase_OrElse_ProducesOrClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice" || x.Name == "Bob";

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("(`Name` = $p0 OR `Name` = $p1)");
    }

    [Fact]
    public void ToCouchbase_Not_ProducesNotClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => !(x.Age > 18);

        CouchbaseFilterExpression f = expr.ToCouchbase();

        f.WhereClause.Should().Be("NOT (`Age` > $p0)");
    }

    [Fact]
    public void ToCouchbase_NestedAndOr_ProducesCorrectParentheses()
    {
        var a    = new EqualNode("IsActive", true, false);
        var b    = new ComparisonNode("Age", 18, ComparisonOp.GreaterThan);
        var c    = new EqualNode("Name", "Admin", false);
        var node = new OrNode(new AndNode(a, b), c);

        CouchbaseFilterExpression f = CouchbaseFilterTranslator.Translate(node);

        f.WhereClause.Should().Be("((`IsActive` = $p0 AND `Age` > $p1) OR `Name` = $p2)");
    }

    // ── ValiFlow<T> fluent pipeline ───────────────────────────────────────────

    [Fact]
    public void ToCouchbase_ValiFlowEqualTo_ProducesEqualClause()
    {
        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.Category, "Fruit");

        CouchbaseFilterExpression f = flow.ToCouchbase();

        f.WhereClause.Should().Be("`Category` = $p0");
        f.Parameters["$p0"].Should().Be("Fruit");
    }

    [Fact]
    public void ToCouchbase_ValiFlowCombined_ProducesAndClause()
    {
        var flow = new ValiFlow<TestDocument>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThan(x => x.Age, 18);

        CouchbaseFilterExpression f = flow.ToCouchbase();

        f.WhereClause.Should().Be("(`IsActive` = $p0 AND `Age` > $p1)");
    }

    // ── Null guards ───────────────────────────────────────────────────────────

    [Fact]
    public void Translate_NullNode_ThrowsArgumentNullException()
    {
        Action act = () => CouchbaseFilterTranslator.Translate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToCouchbase_ValiFlowNull_ThrowsArgumentNullException()
    {
        ValiFlow<TestDocument> flow = null!;

        Action act = () => flow.ToCouchbase();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToCouchbase_ExpressionNull_ThrowsArgumentNullException()
    {
        Expression<Func<TestDocument, bool>> expr = null!;

        Action act = () => expr.ToCouchbase();

        act.Should().Throw<ArgumentNullException>();
    }

    // ── OCP — custom converter ────────────────────────────────────────────────

    [Fact]
    public void CustomConverter_WhenSet_UsedForMatchingType()
    {
        Func<object?, object?> converter = v => v is decimal d ? $"DEC:{d}" : null;

        var node = new EqualNode("Price", 9.99m, false);
        CouchbaseFilterExpression f = CouchbaseFilterTranslator.Translate(node, converter);

        f.Parameters["$p0"].Should().Be("DEC:9.99");
    }

    [Fact]
    public void CustomConverter_WhenReturnsNull_FallsThroughToDefault()
    {
        Func<object?, object?> converter = _ => null;

        var node = new EqualNode("Name", "Alice", false);
        CouchbaseFilterExpression f = CouchbaseFilterTranslator.Translate(node, converter);

        f.Parameters["$p0"].Should().Be("Alice");
    }

    // ── Type coverage ─────────────────────────────────────────────────────────

    [Fact]
    public void ToCouchbase_DecimalValue_ProducesNumericParameter()
    {
        // Regression: N1QL has no implicit coercion between string and number — a decimal
        // bound as a string silently matched zero rows against a numeric field for every
        // comparison op, confirmed against a real Couchbase cluster. Must bind as a number.
        var node = new EqualNode("Price", 9.99m, false);

        CouchbaseFilterExpression f = CouchbaseFilterTranslator.Translate(node);

        f.Parameters["$p0"].Should().Be(9.99m);
        f.Parameters["$p0"].Should().BeOfType<decimal>();
    }

    [Fact]
    public void ToCouchbase_GuidValue_ProducesStringParameter()
    {
        var guid = Guid.NewGuid();
        var node = new EqualNode("Id", guid, false);

        CouchbaseFilterExpression f = CouchbaseFilterTranslator.Translate(node);

        f.Parameters["$p0"].Should().Be(guid.ToString());
    }

    [Fact]
    public void ToCouchbase_EnumValue_ProducesLongParameter()
    {
        var node = new EqualNode("Status", CouchbaseTestStatus.Active, false);

        CouchbaseFilterExpression f = CouchbaseFilterTranslator.Translate(node);

        f.Parameters["$p0"].Should().Be(1L);
    }

    [Fact]
    public void ToCouchbase_FallbackTypeObject_ProducesStringParameter()
    {
        var node = new EqualNode("Misc", new Uri("https://example.com"), false);

        CouchbaseFilterExpression f = CouchbaseFilterTranslator.Translate(node);

        f.Parameters["$p0"].Should().Be("https://example.com/");
    }

    private enum CouchbaseTestStatus { Active = 1 }
}
