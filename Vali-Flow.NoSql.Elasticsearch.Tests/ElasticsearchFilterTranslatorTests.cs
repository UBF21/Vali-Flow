using System.Linq.Expressions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Elasticsearch.Extensions;
using Vali_Flow.NoSql.Elasticsearch.Translators;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.Elasticsearch.Tests.Models;

namespace Vali_Flow.NoSql.Elasticsearch.Tests;

public sealed class ElasticsearchFilterTranslatorTests
{
    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_EqualString_ProducesTermQuery()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice";

        Query q = expr.ToElasticsearch();

        var got = q.TryGet<TermQuery>(out var term);
        got.Should().BeTrue();
        term!.Field.ToString().Should().Be("Name");
        term.Value.Should().Be(FieldValue.String("Alice"));
    }

    [Fact]
    public void ToElasticsearch_NotEqualString_ProducesBoolMustNotTermQuery()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name != "Bob";

        Query q = expr.ToElasticsearch();

        var got = q.TryGet<BoolQuery>(out var bool_);
        got.Should().BeTrue();
        bool_!.MustNot.Should().HaveCount(1);
        bool_.MustNot!.First().TryGet<TermQuery>(out var inner).Should().BeTrue();
        inner!.Field.ToString().Should().Be("Name");
    }

    [Fact]
    public void ToElasticsearch_EqualInt_ProducesTermQueryWithLongValue()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age == 30;

        Query q = expr.ToElasticsearch();

        q.TryGet<TermQuery>(out var term);
        term.Should().NotBeNull();
        term!.Field.ToString().Should().Be("Age");
        term.Value.Should().Be(FieldValue.Long(30));
    }

    [Fact]
    public void ToElasticsearch_EqualBool_ProducesTermQueryWithBoolValue()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive == true;

        Query q = expr.ToElasticsearch();

        q.TryGet<TermQuery>(out var term);
        term!.Value.Should().Be(FieldValue.Boolean(true));
    }

    // ── Bool member direct ────────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_BoolMemberDirect_ProducesTermQueryTrue()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive;

        Query q = expr.ToElasticsearch();

        q.TryGet<TermQuery>(out var term);
        term!.Field.ToString().Should().Be("IsActive");
        term.Value.Should().Be(FieldValue.Boolean(true));
    }

    // ── Null checks ───────────────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_IsNull_ProducesBoolMustNotExists()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email == null;

        Query q = expr.ToElasticsearch();

        q.TryGet<BoolQuery>(out var bool_);
        bool_!.MustNot.Should().HaveCount(1);
        bool_.MustNot!.First().TryGet<ExistsQuery>(out var exists);
        exists!.Field.ToString().Should().Be("Email");
    }

    [Fact]
    public void ToElasticsearch_IsNotNull_ProducesExistsQuery()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email != null;

        Query q = expr.ToElasticsearch();

        q.TryGet<ExistsQuery>(out var exists);
        exists!.Field.ToString().Should().Be("Email");
    }

    // ── Range ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_GreaterThan_ProducesNumberRangeGt()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age > 18;

        Query q = expr.ToElasticsearch();

        q.TryGet<NumberRangeQuery>(out var range);
        range!.Field.ToString().Should().Be("Age");
        range.Gt.Should().Be(18.0);
        range.Gte.Should().BeNull();
        range.Lt.Should().BeNull();
        range.Lte.Should().BeNull();
    }

    [Fact]
    public void ToElasticsearch_GreaterThanOrEqual_ProducesNumberRangeGte()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age >= 21;

        Query q = expr.ToElasticsearch();

        q.TryGet<NumberRangeQuery>(out var range);
        range!.Gte.Should().Be(21.0);
        range.Gt.Should().BeNull();
    }

    [Fact]
    public void ToElasticsearch_LessThan_ProducesNumberRangeLt()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age < 65;

        Query q = expr.ToElasticsearch();

        q.TryGet<NumberRangeQuery>(out var range);
        range!.Lt.Should().Be(65.0);
    }

    [Fact]
    public void ToElasticsearch_LessThanOrEqual_ProducesNumberRangeLte()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age <= 60;

        Query q = expr.ToElasticsearch();

        q.TryGet<NumberRangeQuery>(out var range);
        range!.Lte.Should().Be(60.0);
    }

    [Fact]
    public void ToElasticsearch_ConstantOnLeft_FlipsRange()
    {
        // 18 < x.Age → x.Age > 18
        Expression<Func<TestDocument, bool>> expr = x => 18 < x.Age;

        Query q = expr.ToElasticsearch();

        q.TryGet<NumberRangeQuery>(out var range);
        range!.Field.ToString().Should().Be("Age");
        range.Gt.Should().Be(18.0);
    }

    // ── Pattern match ─────────────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_StringContains_ProducesWildcardBothSides()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.Contains("ali");

        Query q = expr.ToElasticsearch();

        q.TryGet<WildcardQuery>(out var wq);
        wq!.Field.ToString().Should().Be("Name");
        wq.Value.Should().Be("*ali*");
        wq.CaseInsensitive.Should().BeTrue();
    }

    [Fact]
    public void ToElasticsearch_StringStartsWith_ProducesWildcardTrailingOnly()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.StartsWith("Al");

        Query q = expr.ToElasticsearch();

        q.TryGet<WildcardQuery>(out var wq);
        wq!.Value.Should().Be("Al*");
    }

    [Fact]
    public void ToElasticsearch_StringEndsWith_ProducesWildcardLeadingOnly()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.EndsWith("ce");

        Query q = expr.ToElasticsearch();

        q.TryGet<WildcardQuery>(out var wq);
        wq!.Value.Should().Be("*ce");
    }

    [Fact]
    public void ToElasticsearch_ContainsSpecialChars_EscapesWildcardChars()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.Contains("a*b?c");

        Query q = expr.ToElasticsearch();

        q.TryGet<WildcardQuery>(out var wq);
        wq!.Value.Should().Be(@"*a\*b\?c*");
    }

    // ── Membership ────────────────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_ListContains_ProducesTermsQuery()
    {
        var ids = new List<int> { 1, 2, 3 };
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        Query q = expr.ToElasticsearch();

        q.TryGet<TermsQuery>(out var terms);
        terms!.Field.ToString().Should().Be("Id");
        terms.Term.Should().NotBeNull();
    }

    [Fact]
    public void ToElasticsearch_EmptyListContains_ProducesBoolMustNotMatchAll()
    {
        var ids = new List<int>();
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        Query q = expr.ToElasticsearch();

        q.TryGet<BoolQuery>(out var bool_);
        bool_!.MustNot.Should().HaveCount(1);
        bool_.MustNot!.First().TryGet<MatchAllQuery>(out _).Should().BeTrue();
    }

    // ── Logical combinators ───────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_AndAlso_ProducesBoolMust()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive && x.Age > 18;

        Query q = expr.ToElasticsearch();

        q.TryGet<BoolQuery>(out var bool_);
        bool_!.Must.Should().HaveCount(2);
        var shouldClauses = bool_.Should;
        shouldClauses.Should().BeNullOrEmpty();
        bool_.MustNot.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ToElasticsearch_OrElse_ProducesBoolShould()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice" || x.Name == "Bob";

        Query q = expr.ToElasticsearch();

        q.TryGet<BoolQuery>(out var bool_);
        var shouldClauses = bool_!.Should;
        shouldClauses.Should().HaveCount(2);
        bool_.Must.Should().BeNullOrEmpty();
        bool_.MinimumShouldMatch.Should().NotBeNull();
    }

    [Fact]
    public void ToElasticsearch_Not_ProducesBoolMustNot()
    {
        Expression<Func<TestDocument, bool>> expr = x => !(x.Age > 18);

        Query q = expr.ToElasticsearch();

        q.TryGet<BoolQuery>(out var bool_);
        bool_!.MustNot.Should().HaveCount(1);
        bool_.MustNot!.First().TryGet<NumberRangeQuery>(out _).Should().BeTrue();
    }

    // ── ValiFlow<T> fluent pipeline ───────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_ValiFlowEqualTo_ProducesTermQuery()
    {
        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.Category, "Fruit");

        Query q = flow.ToElasticsearch();

        q.TryGet<TermQuery>(out var term);
        term!.Field.ToString().Should().Be("Category");
        term.Value.Should().Be(FieldValue.String("Fruit"));
    }

    [Fact]
    public void ToElasticsearch_ValiFlowCombined_ProducesBoolMust()
    {
        var flow = new ValiFlow<TestDocument>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThan(x => x.Age, 18);

        Query q = flow.ToElasticsearch();

        q.TryGet<BoolQuery>(out var bool_);
        bool_!.Must.Should().HaveCount(2);
    }

    // ── Null guards ───────────────────────────────────────────────────────────

    [Fact]
    public void Translate_NullNode_ThrowsArgumentNullException()
    {
        Action act = () => ElasticsearchFilterTranslator.Translate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToElasticsearch_ValiFlowNull_ThrowsArgumentNullException()
    {
        ValiFlow<TestDocument> flow = null!;

        Action act = () => flow.ToElasticsearch();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToElasticsearch_ExpressionNull_ThrowsArgumentNullException()
    {
        Expression<Func<TestDocument, bool>> expr = null!;

        Action act = () => expr.ToElasticsearch();

        act.Should().Throw<ArgumentNullException>();
    }

    // ── OCP — CustomValueConverter ────────────────────────────────────────────

    [Fact]
    public void CustomValueConverter_WhenSet_UsedForMatchingType()
    {
        Func<object?, FieldValue?> converter = v => v is decimal d ? FieldValue.Double((double)d) : null;

        var node = new EqualNode("Price", 9.99m, false);
        Query q = ElasticsearchFilterTranslator.Translate(node, converter);

        q.TryGet<TermQuery>(out var term);
        term.Should().NotBeNull();
        term!.Value.Should().Be(FieldValue.Double(9.99));
    }

    [Fact]
    public void CustomValueConverter_WhenReturnsNull_FallsThroughToDefault()
    {
        Func<object?, FieldValue?> converter = _ => null;

        var node = new EqualNode("Name", "Alice", false);
        Query q = ElasticsearchFilterTranslator.Translate(node, converter);

        q.TryGet<TermQuery>(out var term);
        term!.Value.Should().Be(FieldValue.String("Alice"));
    }

    // ── Direct node construction ──────────────────────────────────────────────

    [Fact]
    public void Translate_DirectEqualNode_ProducesTermQuery()
    {
        var node = new EqualNode("Status", "Active", false);

        Query q = ElasticsearchFilterTranslator.Translate(node);

        q.TryGet<TermQuery>(out var term);
        term!.Field.ToString().Should().Be("Status");
        term.Value.Should().Be(FieldValue.String("Active"));
    }

    [Fact]
    public void Translate_DirectAndNode_ProducesBoolMustWithTwoChildren()
    {
        var left  = new EqualNode("IsActive", true, false);
        var right = new ComparisonNode("Age", 18, ComparisonOp.GreaterThan);
        var node  = new AndNode(left, right);

        Query q = ElasticsearchFilterTranslator.Translate(node);

        q.TryGet<BoolQuery>(out var bool_);
        bool_!.Must.Should().HaveCount(2);
    }

    // ── Type Coverage adicional ───────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_FloatValue_ProducesFieldValueDouble()
    {
        var node = new EqualNode("Score", 3.14f, false);

        Query q = ElasticsearchFilterTranslator.Translate(node);

        q.TryGet<TermQuery>(out var term);
        term.Should().NotBeNull();
        term!.Field.ToString().Should().Be("Score");
        term.Value.Should().Be(FieldValue.Double((double)3.14f));
    }

    [Fact]
    public void ToElasticsearch_DecimalValue_ConvertsToDouble()
    {
        var node = new EqualNode("Price", 9.99m, false);

        Query q = ElasticsearchFilterTranslator.Translate(node);

        q.TryGet<TermQuery>(out var term);
        term.Should().NotBeNull();
        term!.Field.ToString().Should().Be("Price");
        term.Value.Should().Be(FieldValue.Double((double)9.99m));
    }

    [Fact]
    public void ToElasticsearch_GuidValue_ProducesStringFieldValue()
    {
        var g    = Guid.NewGuid();
        var node = new EqualNode("Id", g, false);

        Query q = ElasticsearchFilterTranslator.Translate(node);

        q.TryGet<TermQuery>(out var term);
        term.Should().NotBeNull();
        term!.Field.ToString().Should().Be("Id");
        term.Value.Should().Be(FieldValue.String(g.ToString()));
    }

    [Fact]
    public void ToElasticsearch_EnumValue_ProducesFieldValueLong()
    {
        var node = new EqualNode("Status", TestStatusEnum.Active, false);

        Query q = ElasticsearchFilterTranslator.Translate(node);

        q.TryGet<TermQuery>(out var term);
        term.Should().NotBeNull();
        term!.Field.ToString().Should().Be("Status");
        term.Value.Should().Be(FieldValue.Long(1));
    }

    // ── Boolean false ─────────────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_EqualBoolFalse_ProducesTermQueryFalse()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive == false;

        Query q = expr.ToElasticsearch();

        q.TryGet<TermQuery>(out var term);
        term.Should().NotBeNull();
        term!.Field.ToString().Should().Be("IsActive");
        term.Value.Should().Be(FieldValue.Boolean(false));
    }

    // ── Lógica anidada ────────────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_NestedAndOr_ProducesCorrectBoolStructure()
    {
        // (x.IsActive && x.Age > 18) || x.Name == "Admin"
        Expression<Func<TestDocument, bool>> expr = x => (x.IsActive && x.Age > 18) || x.Name == "Admin";

        Query q = expr.ToElasticsearch();

        // Outer must be Should (OR)
        q.TryGet<BoolQuery>(out var outer);
        outer.Should().NotBeNull();
        var shouldClauses = outer!.Should;
        shouldClauses.Should().HaveCount(2);

        // First should clause must be BoolQuery.Must with 2 elements (AND)
        shouldClauses!.First().TryGet<BoolQuery>(out var inner);
        inner.Should().NotBeNull();
        inner!.Must.Should().HaveCount(2);
    }

    // ── Wildcard escaping ─────────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_ContainsWithAsterisk_EscapesWildcardChar()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.Contains("a*b");

        Query q = expr.ToElasticsearch();

        q.TryGet<WildcardQuery>(out var wq);
        wq.Should().NotBeNull();
        wq!.Value.Should().Be(@"*a\*b*");
    }

    // ── ValiFlow: pipeline de 3 condiciones ──────────────────────────────────

    [Fact]
    public void ToElasticsearch_ValiFlowThreeConditions_ProducesNestedBoolMust()
    {
        var flow = new ValiFlow<TestDocument>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThan(x => x.Age, 18)
            .EqualTo(x => x.Name, "Admin");

        Query q = flow.ToElasticsearch();

        // The pipeline combines with AND → final query must have Must clauses
        q.TryGet<BoolQuery>(out var bool_);
        bool_.Should().NotBeNull();
        bool_!.Must.Should().NotBeNullOrEmpty();
        bool_.Must!.Count().Should().BeGreaterThanOrEqualTo(2);
    }

    // ── Terms con null en lista ───────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_TermsWithNullValue_IncludesNullFieldValue()
    {
        var node = new InNode("Id", new List<object?> { 1, null, 2 });

        Query q = ElasticsearchFilterTranslator.Translate(node);

        // Must produce a TermsQuery (not fall into the empty-list branch)
        q.TryGet<TermsQuery>(out var terms);
        terms.Should().NotBeNull();
        terms!.Field.ToString().Should().Be("Id");
        terms.Term.Should().NotBeNull();
    }

    // ── Decimal range ─────────────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_DecimalGreaterThan_ProducesNumberRangeGt()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Price > 99.99m;

        Query q = expr.ToElasticsearch();

        q.TryGet<NumberRangeQuery>(out var range);
        range.Should().NotBeNull();
        range!.Field.ToString().Should().Be("Price");
        range.Gt.Should().BeApproximately((double)99.99m, 0.001);
        range.Gte.Should().BeNull();
    }

    [Fact]
    public void ToElasticsearch_DecimalLessThanOrEqual_ProducesNumberRangeLte()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Price <= 500m;

        Query q = expr.ToElasticsearch();

        q.TryGet<NumberRangeQuery>(out var range);
        range.Should().NotBeNull();
        range!.Lte.Should().BeApproximately((double)500m, 0.001);
        range.Lt.Should().BeNull();
    }

    // ── String IN list ────────────────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_StringListContains_ProducesTermsQuery()
    {
        var categories = new List<string> { "Electronics", "Books" };
        Expression<Func<TestDocument, bool>> expr = x => categories.Contains(x.Category);

        Query q = expr.ToElasticsearch();

        q.TryGet<TermsQuery>(out var terms);
        terms.Should().NotBeNull();
        terms!.Field.ToString().Should().Be("Category");
        terms.Term.Should().NotBeNull();
    }

    [Fact]
    public void ToElasticsearch_SingleItemList_ProducesTermsQuery()
    {
        var ids = new List<int> { 42 };
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        Query q = expr.ToElasticsearch();

        q.TryGet<TermsQuery>(out var terms);
        terms.Should().NotBeNull();
    }

    // ── Constant on left — todos los operadores ───────────────────────────────

    [Fact]
    public void ToElasticsearch_ConstantOnLeftLessThan_FlipsToGreaterThan()
    {
        // 65 > x.Age → x.Age < 65
        Expression<Func<TestDocument, bool>> expr = x => 65 > x.Age;

        Query q = expr.ToElasticsearch();

        q.TryGet<NumberRangeQuery>(out var range);
        range.Should().NotBeNull();
        range!.Field.ToString().Should().Be("Age");
        range.Lt.Should().Be(65.0);
    }

    [Fact]
    public void ToElasticsearch_ConstantOnLeftGreaterThanOrEqual_FlipsToLessThanOrEqual()
    {
        // 100 >= x.Age → x.Age <= 100
        Expression<Func<TestDocument, bool>> expr = x => 100 >= x.Age;

        Query q = expr.ToElasticsearch();

        q.TryGet<NumberRangeQuery>(out var range);
        range.Should().NotBeNull();
        range!.Lte.Should().Be(100.0);
    }

    // ── Lógica anidada profunda ───────────────────────────────────────────────

    [Fact]
    public void ToElasticsearch_NotOfAndOr_ProducesBoolMustNotWithNestedBool()
    {
        // NOT ((A AND B) OR C)
        var a    = new EqualNode("IsActive", true, false);
        var b    = new ComparisonNode("Age", 18, ComparisonOp.GreaterThan);
        var c    = new EqualNode("Name", "Admin", false);
        var node = new NotNode(new OrNode(new AndNode(a, b), c));

        Query q = ElasticsearchFilterTranslator.Translate(node);

        q.TryGet<BoolQuery>(out var outer);
        outer.Should().NotBeNull();
        outer!.MustNot.Should().HaveCount(1);
        outer.MustNot!.First().TryGet<BoolQuery>(out var inner);
        inner.Should().NotBeNull();
        // inner is an OR → should have Should clauses
        inner!.Should.Should().HaveCount(2);
    }

    [Fact]
    public void ToElasticsearch_FourLevelNesting_ProducesValidQuery()
    {
        // A AND (B OR (C AND D))
        var a    = new EqualNode("IsActive", true, false);
        var b    = new ComparisonNode("Age", 18, ComparisonOp.GreaterThan);
        var c    = new EqualNode("Category", "Books", false);
        var d    = new ComparisonNode("Price", 50m, ComparisonOp.LessThan);
        var node = new AndNode(a, new OrNode(b, new AndNode(c, d)));

        // Should not throw and produce a valid bool query
        Query q = ElasticsearchFilterTranslator.Translate(node);
        q.Should().NotBeNull();
        q.TryGet<BoolQuery>(out var root);
        root.Should().NotBeNull();
    }

    // ── ValiFlow pipeline de 4 condiciones ───────────────────────────────────

    [Fact]
    public void ToElasticsearch_ValiFlowFourConditions_ProducesNestedBoolMust()
    {
        var flow = new ValiFlow<TestDocument>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThan(x => x.Age, 18)
            .LessThanOrEqualTo(x => x.Price, 500m)
            .EqualTo(x => x.Category, "Electronics");

        Query q = flow.ToElasticsearch();

        q.Should().NotBeNull();
        q.TryGet<BoolQuery>(out var bool_);
        bool_.Should().NotBeNull();
        bool_!.Must.Should().NotBeNullOrEmpty();
    }

    // ── Null in NOT-EQUAL produces MustNot(Null check) ───────────────────────

    [Fact]
    public void ToElasticsearch_EqualToNull_ProducesMustNotExists()
    {
        var node = new NullNode("Email", NullCheckOp.IsNull);

        Query q = ElasticsearchFilterTranslator.Translate(node);

        q.TryGet<BoolQuery>(out var bool_);
        bool_.Should().NotBeNull();
        bool_!.MustNot.Should().HaveCount(1);
        bool_.MustNot!.First().TryGet<ExistsQuery>(out var exists);
        exists.Should().NotBeNull();
        exists!.Field.ToString().Should().Be("Email");
    }

    // ── NOT-EQUAL con null produce ExistsQuery ────────────────────────────────

    [Fact]
    public void ToElasticsearch_NotEqualNull_ProducesExistsQuery()
    {
        var node = new NullNode("Email", NullCheckOp.IsNotNull);

        Query q = ElasticsearchFilterTranslator.Translate(node);

        q.TryGet<ExistsQuery>(out var exists);
        exists.Should().NotBeNull();
        exists!.Field.ToString().Should().Be("Email");
    }

    private enum TestStatusEnum { Active = 1 }
}
