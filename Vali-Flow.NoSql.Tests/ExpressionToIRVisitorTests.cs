using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Extensions;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.Tests.Models;

namespace Vali_Flow.NoSql.Tests;

public sealed class ExpressionToIRVisitorTests
{
    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToNoSqlIR_EqualString_ReturnsEqualNode()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice";

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<EqualNode>().Subject;
        node.Field.Should().Be("Name");
        node.Value.Should().Be("Alice");
        node.IsNegated.Should().BeFalse();
    }

    [Fact]
    public void ToNoSqlIR_NotEqualString_ReturnsNegatedEqualNode()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name != "Bob";

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<EqualNode>().Subject;
        node.Field.Should().Be("Name");
        node.Value.Should().Be("Bob");
        node.IsNegated.Should().BeTrue();
    }

    [Fact]
    public void ToNoSqlIR_EqualInt_ReturnsEqualNode()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age == 30;

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<EqualNode>().Subject;
        node.Field.Should().Be("Age");
        node.Value.Should().Be(30);
    }

    // ── Bool member direct ────────────────────────────────────────────────────

    [Fact]
    public void ToNoSqlIR_BoolMemberDirect_ReturnsEqualNodeTrue()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive;

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<EqualNode>().Subject;
        node.Field.Should().Be("IsActive");
        node.Value.Should().Be(true);
        node.IsNegated.Should().BeFalse();
    }

    // ── Null checks ───────────────────────────────────────────────────────────

    [Fact]
    public void ToNoSqlIR_EqualNull_ReturnsIsNullNode()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email == null;

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<NullNode>().Subject;
        node.Field.Should().Be("Email");
        node.Check.Should().Be(NullCheckOp.IsNull);
    }

    [Fact]
    public void ToNoSqlIR_NotEqualNull_ReturnsIsNotNullNode()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Email != null;

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<NullNode>().Subject;
        node.Field.Should().Be("Email");
        node.Check.Should().Be(NullCheckOp.IsNotNull);
    }

    // ── Comparison ────────────────────────────────────────────────────────────

    [Fact]
    public void ToNoSqlIR_GreaterThan_ReturnsComparisonNodeGT()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age > 18;

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<ComparisonNode>().Subject;
        node.Field.Should().Be("Age");
        node.Value.Should().Be(18);
        node.Op.Should().Be(ComparisonOp.GreaterThan);
    }

    [Fact]
    public void ToNoSqlIR_GreaterThanOrEqual_ReturnsComparisonNodeGTE()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age >= 21;

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<ComparisonNode>().Subject;
        node.Op.Should().Be(ComparisonOp.GreaterThanOrEqual);
        node.Value.Should().Be(21);
    }

    [Fact]
    public void ToNoSqlIR_LessThan_ReturnsComparisonNodeLT()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Price < 100m;

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<ComparisonNode>().Subject;
        node.Field.Should().Be("Price");
        node.Op.Should().Be(ComparisonOp.LessThan);
    }

    [Fact]
    public void ToNoSqlIR_LessThanOrEqual_ReturnsComparisonNodeLTE()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age <= 65;

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<ComparisonNode>().Subject;
        node.Op.Should().Be(ComparisonOp.LessThanOrEqual);
    }

    [Fact]
    public void ToNoSqlIR_ConstantOnLeft_FlipsOperator()
    {
        // 18 < x.Age  →  x.Age > 18
        Expression<Func<TestDocument, bool>> expr = x => 18 < x.Age;

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<ComparisonNode>().Subject;
        node.Field.Should().Be("Age");
        node.Op.Should().Be(ComparisonOp.GreaterThan);
        node.Value.Should().Be(18);
    }

    // ── String pattern matching ───────────────────────────────────────────────

    [Fact]
    public void ToNoSqlIR_StringContains_ReturnsLikeNodeContains()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.Contains("ali");

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<LikeNode>().Subject;
        node.Field.Should().Be("Name");
        node.Pattern.Should().Be("ali");
        node.Op.Should().Be(LikeOp.Contains);
    }

    [Fact]
    public void ToNoSqlIR_StringStartsWith_ReturnsLikeNodeStartsWith()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.StartsWith("Al");

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<LikeNode>().Subject;
        node.Op.Should().Be(LikeOp.StartsWith);
        node.Pattern.Should().Be("Al");
    }

    [Fact]
    public void ToNoSqlIR_StringEndsWith_ReturnsLikeNodeEndsWith()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name.EndsWith("ce");

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<LikeNode>().Subject;
        node.Op.Should().Be(LikeOp.EndsWith);
        node.Pattern.Should().Be("ce");
    }

    // ── Collection membership ─────────────────────────────────────────────────

    [Fact]
    public void ToNoSqlIR_ListContains_ReturnsInNode()
    {
        var ids = new List<int> { 1, 2, 3 };
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<InNode>().Subject;
        node.Field.Should().Be("Id");
        node.Values.Should().BeEquivalentTo(new object[] { 1, 2, 3 });
    }

    [Fact]
    public void ToNoSqlIR_EmptyListContains_ReturnsInNodeWithEmptyValues()
    {
        var ids = new List<int>();
        Expression<Func<TestDocument, bool>> expr = x => ids.Contains(x.Id);

        var ir = expr.ToNoSqlIR();

        var node = ir.Should().BeOfType<InNode>().Subject;
        node.Values.Should().BeEmpty();
    }

    // ── Logical combinators ───────────────────────────────────────────────────

    [Fact]
    public void ToNoSqlIR_AndAlso_ReturnsAndNode()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.IsActive && x.Age > 18;

        var ir = expr.ToNoSqlIR();

        ir.Should().BeOfType<AndNode>();
        var and = (AndNode)ir;
        and.Left.Should().BeOfType<EqualNode>();
        and.Right.Should().BeOfType<ComparisonNode>();
    }

    [Fact]
    public void ToNoSqlIR_OrElse_ReturnsOrNode()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "Alice" || x.Name == "Bob";

        var ir = expr.ToNoSqlIR();

        ir.Should().BeOfType<OrNode>();
        var or = (OrNode)ir;
        or.Left.Should().BeOfType<EqualNode>();
        or.Right.Should().BeOfType<EqualNode>();
    }

    [Fact]
    public void ToNoSqlIR_Not_ReturnsNotNode()
    {
        Expression<Func<TestDocument, bool>> expr = x => !(x.Age > 18);

        var ir = expr.ToNoSqlIR();

        var not = ir.Should().BeOfType<NotNode>().Subject;
        not.Inner.Should().BeOfType<ComparisonNode>();
    }

    // ── ValiFlow<T> fluent API ────────────────────────────────────────────────

    [Fact]
    public void ToNoSqlIR_ValiFlowEqualTo_ReturnsEqualNode()
    {
        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.IsActive, true);

        var ir = flow.ToNoSqlIR();

        var node = ir.Should().BeOfType<EqualNode>().Subject;
        node.Field.Should().Be("IsActive");
        node.Value.Should().Be(true);
    }

    [Fact]
    public void ToNoSqlIR_ValiFlowNullArg_ThrowsArgumentNullException()
    {
        ValiFlow<TestDocument> flow = null!;

        Action act = () => flow.ToNoSqlIR();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToNoSqlIR_ExpressionNullArg_ThrowsArgumentNullException()
    {
        Expression<Func<TestDocument, bool>> expr = null!;

        Action act = () => expr.ToNoSqlIR();

        act.Should().Throw<ArgumentNullException>();
    }

    // ── EqualNode guard ───────────────────────────────────────────────────────

    [Fact]
    public void EqualNode_NullValue_ThrowsArgumentNullException()
    {
        Action act = () => new EqualNode("Field", null!, false);

        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*EqualNode.Value cannot be null*");
    }

    [Fact]
    public void EqualNode_NullField_ThrowsArgumentNullException()
    {
        Action act = () => new EqualNode(null!, "value", false);

        act.Should().Throw<ArgumentNullException>();
    }
}
