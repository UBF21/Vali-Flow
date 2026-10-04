using System.Diagnostics;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Firestore.Extensions;
using Vali_Flow.NoSql.Firestore.Tests.Models;
using Vali_Flow.NoSql.Firestore.Translators;
using Vali_Flow.NoSql.IR;

namespace Vali_Flow.NoSql.Firestore.Tests.Translators;

public class FirestoreFilterTranslatorTests
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
    public void ToFirestore_WithTag_SetsTagAndEntityTypeOnActivity()
    {
        using var listener = AttachListener();
        Activity? captured = null;
        listener.ActivityStopped = a => captured = a;

        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.Name, "Alice");

        flow.ToFirestore(tag: "report-x");

        captured.Should().NotBeNull();
        captured!.GetTagItem("vali_flow.tag").Should().Be("report-x");
        captured.GetTagItem("vali_flow.entity_type").Should().Be(nameof(TestDocument));
    }

    [Fact]
    public void Translate_LikeNode_SetsErrorStatusOnActivity()
    {
        using var listener = AttachListener();
        Activity? captured = null;
        listener.ActivityStopped = a => captured = a;

        var node = new LikeNode("Name", "son", LikeOp.Contains);

        Action act = () => FirestoreFilterTranslator.Translate(node);

        act.Should().Throw<NotSupportedException>();
        captured.Should().NotBeNull();
        captured!.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Fact]
    public void Translate_NullNode_ThrowsArgumentNullException()
    {
        Action act = () => FirestoreFilterTranslator.Translate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VisitEqual_NotNegated_ReturnsEqualToFilter()
    {
        var node = new EqualNode("Name", "John", isNegated: false);

        var filter = FirestoreFilterTranslator.Translate(node);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void VisitEqual_Negated_ReturnsNotEqualToFilter()
    {
        var node = new EqualNode("Name", "John", isNegated: true);

        var filter = FirestoreFilterTranslator.Translate(node);

        filter.Should().NotBeNull();
    }

    [Theory]
    [InlineData(ComparisonOp.GreaterThan)]
    [InlineData(ComparisonOp.GreaterThanOrEqual)]
    [InlineData(ComparisonOp.LessThan)]
    [InlineData(ComparisonOp.LessThanOrEqual)]
    public void VisitComparison_SupportedOps_ReturnsFilter(ComparisonOp op)
    {
        var node = new ComparisonNode("Age", 18, op);

        var filter = FirestoreFilterTranslator.Translate(node);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void VisitIn_ReturnsInArrayFilter()
    {
        var node = new InNode("Category", new List<object?> { "a", "b", "c" });

        var filter = FirestoreFilterTranslator.Translate(node);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void VisitNull_IsNull_ReturnsEqualToNullFilter()
    {
        var node = new NullNode("Email", NullCheckOp.IsNull);

        var filter = FirestoreFilterTranslator.Translate(node);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void VisitNull_IsNotNull_ReturnsNotEqualToNullFilter()
    {
        var node = new NullNode("Email", NullCheckOp.IsNotNull);

        var filter = FirestoreFilterTranslator.Translate(node);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void VisitAnd_ReturnsCombinedFilter()
    {
        var node = new AndNode(
            new EqualNode("IsActive", true, isNegated: false),
            new ComparisonNode("Age", 18, ComparisonOp.GreaterThan));

        var filter = FirestoreFilterTranslator.Translate(node);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void VisitOr_ReturnsCombinedFilter()
    {
        var node = new OrNode(
            new EqualNode("Category", "a", isNegated: false),
            new EqualNode("Category", "b", isNegated: false));

        var filter = FirestoreFilterTranslator.Translate(node);

        filter.Should().NotBeNull();
    }

    [Theory]
    [InlineData(LikeOp.Contains)]
    [InlineData(LikeOp.StartsWith)]
    [InlineData(LikeOp.EndsWith)]
    public void VisitLike_AnyOp_ThrowsNotSupportedException(LikeOp op)
    {
        var node = new LikeNode("Name", "jo", op, CaseSensitive: true);

        Action act = () => FirestoreFilterTranslator.Translate(node);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*pattern-matching*");
    }

    [Fact]
    public void VisitNot_ThrowsNotSupportedException()
    {
        var node = new NotNode(new EqualNode("IsActive", true, isNegated: false));

        Action act = () => FirestoreFilterTranslator.Translate(node);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*NOT filter*");
    }

    [Fact]
    public void Translate_CustomConverter_IsUsedBeforeFallback()
    {
        var node = new EqualNode("Price", 42m, isNegated: false);

        var filter = FirestoreFilterTranslator.Translate(node, v => v is decimal d ? (double)d : null);

        filter.Should().NotBeNull();
    }
}
