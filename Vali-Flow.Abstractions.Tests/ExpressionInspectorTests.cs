using System.Linq.Expressions;
using Vali_Flow.Abstractions.Helpers;

namespace Vali_Flow.Abstractions.Tests;

public sealed class ExpressionInspectorTests
{
    private sealed class TestEntity
    {
        public int Age { get; set; }
        public string? Name { get; set; }
    }

    // ── IsColumnExpression ──────────────────────────────────────────────

    [Fact]
    public void IsColumnExpression_DirectMember_ReturnsTrue()
    {
        Expression<Func<TestEntity, int>> expr = x => x.Age;
        ExpressionInspector.IsColumnExpression(expr.Body).Should().BeTrue();
    }

    [Fact]
    public void IsColumnExpression_MemberWrappedInConvert_ReturnsTrue()
    {
        Expression<Func<TestEntity, object>> expr = x => x.Age;
        // x.Age boxed to object produces Convert(MemberExpression)
        ExpressionInspector.IsColumnExpression(expr.Body).Should().BeTrue();
    }

    [Fact]
    public void IsColumnExpression_Constant_ReturnsFalse()
    {
        Expression<Func<TestEntity, int>> expr = _ => 18;
        ExpressionInspector.IsColumnExpression(expr.Body).Should().BeFalse();
    }

    [Fact]
    public void IsColumnExpression_Null_ThrowsArgumentNullException()
    {
        var act = () => ExpressionInspector.IsColumnExpression(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── IsNullConstant ───────────────────────────────────────────────────

    [Fact]
    public void IsNullConstant_NullLiteral_ReturnsTrue()
    {
        Expression<Func<TestEntity, bool>> expr = x => x.Name == null;
        var binary = (BinaryExpression)expr.Body;
        ExpressionInspector.IsNullConstant(binary.Right).Should().BeTrue();
    }

    [Fact]
    public void IsNullConstant_NonNullConstant_ReturnsFalse()
    {
        Expression<Func<TestEntity, bool>> expr = x => x.Name == "a";
        var binary = (BinaryExpression)expr.Body;
        ExpressionInspector.IsNullConstant(binary.Right).Should().BeFalse();
    }

    [Fact]
    public void IsNullConstant_Null_ThrowsArgumentNullException()
    {
        var act = () => ExpressionInspector.IsNullConstant(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsNullConstant_NullWrappedInConvertChecked_ReturnsTrue()
    {
        // Mirrors the ConvertChecked handling already present in IsColumnExpression.
        var convertChecked = Expression.ConvertChecked(Expression.Constant(null, typeof(object)), typeof(string));
        ExpressionInspector.IsNullConstant(convertChecked).Should().BeTrue();
    }

    // ── EvaluateExpression ───────────────────────────────────────────────

    [Fact]
    public void EvaluateExpression_Constant_ReturnsValue()
    {
        Expression<Func<TestEntity, int>> expr = _ => 42;
        ExpressionInspector.EvaluateExpression(expr.Body).Should().Be(42);
    }

    [Fact]
    public void EvaluateExpression_ClosureCapturedVariable_ReturnsRuntimeValue()
    {
        int captured = 7;
        Expression<Func<TestEntity, int>> expr = _ => captured + 1;
        ExpressionInspector.EvaluateExpression(expr.Body).Should().Be(8);
    }

    [Fact]
    public void EvaluateExpression_ExpressionReferencingUnboundParameter_ThrowsInvalidOperationException()
    {
        Expression<Func<TestEntity, int>> expr = x => x.Age;

        var act = () => ExpressionInspector.EvaluateExpression(expr.Body);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EvaluateExpression_Null_ThrowsArgumentNullException()
    {
        var act = () => ExpressionInspector.EvaluateExpression(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── FlipComparisonOperator ───────────────────────────────────────────

    [Theory]
    [InlineData(ExpressionType.GreaterThan, ExpressionType.LessThan)]
    [InlineData(ExpressionType.GreaterThanOrEqual, ExpressionType.LessThanOrEqual)]
    [InlineData(ExpressionType.LessThan, ExpressionType.GreaterThan)]
    [InlineData(ExpressionType.LessThanOrEqual, ExpressionType.GreaterThanOrEqual)]
    [InlineData(ExpressionType.Equal, ExpressionType.Equal)]
    [InlineData(ExpressionType.NotEqual, ExpressionType.NotEqual)]
    public void FlipComparisonOperator_ReturnsExpectedMirror(ExpressionType input, ExpressionType expected)
    {
        ExpressionInspector.FlipComparisonOperator(input).Should().Be(expected);
    }

    [Fact]
    public void FlipComparisonOperator_SemanticsPreserved_ConstantOnLeft()
    {
        // 18 < x.Age  ==  x.Age > 18
        const int left = 18;
        const int age = 25;

        bool original = left < age;
        var flipped = ExpressionInspector.FlipComparisonOperator(ExpressionType.LessThan);
        bool flippedResult = flipped switch
        {
            ExpressionType.GreaterThan => age > left,
            _ => throw new InvalidOperationException("unexpected flip result")
        };

        flippedResult.Should().Be(original);
    }
}
