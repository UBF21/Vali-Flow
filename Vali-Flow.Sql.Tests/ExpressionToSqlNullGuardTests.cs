using FluentAssertions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>
/// Tests for null guards and null-value propagation in <see cref="ExpressionToSqlVisitor"/>.
/// </summary>
public sealed class ExpressionToSqlNullGuardTests
{
    // ── Null argument guards ──────────────────────────────────────────────────

    [Fact]
    public void Translate_NullExpression_ThrowsArgumentNullException()
    {
        var act = () => ExpressionToSqlVisitor.Translate<TestUser>(null!, new SqlServerDialect());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("expression");
    }

    [Fact]
    public void Translate_NullDialect_ThrowsArgumentNullException()
    {
        var act = () => ExpressionToSqlVisitor.Translate<TestUser>(x => x.Age > 0, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dialect");
    }

    // ── Captured null variable behavior ──────────────────────────────────────

    /// <summary>
    /// When a closed-over variable holds null, the expression tree carries a MemberExpression
    /// pointing to the capture field (not a ConstantExpression with null), so IsNullConstant
    /// does NOT fire. The visitor evaluates the closed-over value (null), stores it as DBNull.Value,
    /// and emits a parameterized comparison: [Name] = @p0 with p0 = DBNull.Value.
    ///
    /// This documents current behavior: the translator does NOT rewrite null-valued closed-over
    /// variables into IS NULL / IS NOT NULL. Callers must use literal null (x => x.Name == null)
    /// if they want IS NULL semantics.
    /// </summary>
    [Fact]
    public void CapturedNullVariable_ProducesParameterizedComparison_NotIsNull()
    {
        string? name = null;

        var result = ExpressionToSqlVisitor.Translate<TestUser>(
            x => x.Name == name,
            new SqlServerDialect());

        // The closed-over null becomes DBNull.Value in the parameter dictionary,
        // NOT an IS NULL clause.
        result.Sql.Should().Be("[Name] = @p0");
        result.Parameters["p0"].Should().Be(DBNull.Value);
    }

    /// <summary>
    /// A literal null in the expression body (ConstantExpression with Value == null) IS detected
    /// by IsNullConstant and correctly emits [Name] IS NULL.
    /// </summary>
    [Fact]
    public void LiteralNull_ProducesIsNullSql()
    {
        var result = ExpressionToSqlVisitor.Translate<TestUser>(
            x => x.Name == null,
            new SqlServerDialect());

        result.Sql.Should().Be("[Name] IS NULL");
        result.Parameters.Should().BeEmpty();
    }
}
