using FluentAssertions;
using System.Linq.Expressions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>
/// Tests for ExpressionType.Negate unary, constant-on-left arithmetic, and nested CASE WHEN expressions.
/// </summary>
public sealed class ExpressionToSqlUnaryAndNestedTests
{
    private static SqlResult Translate(Expression<Func<TestUser, bool>> expr, ISqlDialect dialect)
        => ExpressionToSqlVisitor.Translate(expr, dialect);

    private static readonly ISqlDialect SqlServer = new SqlServerDialect();
    private static readonly ISqlDialect Postgres  = new PostgreSqlDialect();

    // ── Section 1 — ExpressionType.Negate unary ──────────────────────────────

    /// <summary>
    /// -x.Age &gt; -5 on SqlServer.
    /// Negate wraps the column: -([Age]) &gt; @p0 with p0 = -5.
    /// </summary>
    [Fact]
    public void Negate_AgeGreaterThanNegativeFive_SqlServer_EmitsNegatedColumn()
    {
        var result = Translate(x => -x.Age > -5, SqlServer);

        result.Sql.Should().Contain("-([Age])");
        result.Sql.Should().Contain("> @p0");
        result.Parameters["p0"].Should().Be(-5);
    }

    /// <summary>
    /// -x.Age &gt; 0 on SqlServer.
    /// Expected: -([Age]) &gt; @p0 with p0 = 0.
    /// </summary>
    [Fact]
    public void Negate_AgeGreaterThanZero_SqlServer_EmitsNegatedColumn()
    {
        var result = Translate(x => -x.Age > 0, SqlServer);

        result.Sql.Should().Be("-([Age]) > @p0");
        result.Parameters["p0"].Should().Be(0);
    }

    /// <summary>
    /// -x.Age &gt; -5 on PostgreSQL.
    /// Double-quoted column: -("Age") &gt; @p0 with p0 = -5.
    /// </summary>
    [Fact]
    public void Negate_AgeGreaterThanNegativeFive_PostgreSQL_EmitsDoubleQuotedColumn()
    {
        var result = Translate(x => -x.Age > -5, Postgres);

        result.Sql.Should().Contain("-(\"Age\")");
        result.Parameters["p0"].Should().Be(-5);
    }

    /// <summary>
    /// (-x.Age + 10) &gt; 0 on SqlServer.
    /// Negate combined with arithmetic: -([Age]) + @p0 &gt; @p1 with p0 = 10, p1 = 0.
    /// </summary>
    [Fact]
    public void Negate_CombinedWithArithmetic_SqlServer_EmitsNegatedColumnPlusConstant()
    {
        var result = Translate(x => (-x.Age + 10) > 0, SqlServer);

        result.Sql.Should().Be("-([Age]) + @p0 > @p1");
        result.Parameters["p0"].Should().Be(10);
        result.Parameters["p1"].Should().Be(0);
    }

    // ── Section 2 — Constant on left in arithmetic (flip logic) ──────────────

    /// <summary>
    /// 100 - x.Age &gt; 50 on SqlServer.
    /// The flip logic promotes the column to the left: [Age] - @p0 &gt; @p1 with p0 = 100, p1 = 50.
    /// </summary>
    [Fact]
    public void ConstantOnLeft_Subtract_SqlServer_FlipsColumnToLeft()
    {
        var result = Translate(x => 100 - x.Age > 50, SqlServer);

        result.Sql.Should().Be("[Age] - @p0 > @p1");
        result.Parameters["p0"].Should().Be(100);
        result.Parameters["p1"].Should().Be(50);
    }

    // ── Section 3 — Nested CASE WHEN ─────────────────────────────────────────

    /// <summary>
    /// (x.IsActive ? (x.Age &gt; 18 ? 1 : 0) : -1) &gt; 0 on SqlServer.
    /// Outer CASE WHEN wraps an inner CASE WHEN in the THEN branch.
    /// The literal -1 compiles to Negate(Const(1)), emitting -(@pN) for the ELSE.
    /// </summary>
    [Fact]
    public void NestedCaseWhen_IsActiveThenAgeCheck_SqlServer_EmitsNestedCaseWhen()
    {
        var result = Translate(x => (x.IsActive ? (x.Age > 18 ? 1 : 0) : -1) > 0, SqlServer);

        result.Sql.Should().StartWith("CASE WHEN [IsActive] = 1 THEN CASE WHEN [Age] > @p0 THEN @p1 ELSE @p2 END ELSE");
        result.Sql.Should().EndWith("END > @p4");
        result.Parameters["p0"].Should().Be(18);
        result.Parameters["p1"].Should().Be(1);
        result.Parameters["p2"].Should().Be(0);
        result.Parameters["p4"].Should().Be(0);
    }

    /// <summary>
    /// x.IsActive ? x.Name.Length &gt; 5 : x.Name.Length &gt; 0 on SqlServer.
    /// Nested with string.Length in both branches.
    /// Expected: CASE WHEN [IsActive] = 1 THEN LEN([Name]) &gt; @p0 ELSE LEN([Name]) &gt; @p1 END
    /// </summary>
    [Fact]
    public void NestedCaseWhen_IsActiveThenNameLengthCheck_SqlServer_EmitsLenInBothBranches()
    {
        var result = Translate(x => x.IsActive ? x.Name.Length > 5 : x.Name.Length > 0, SqlServer);

        result.Sql.Should().Be("CASE WHEN [IsActive] = 1 THEN LEN([Name]) > @p0 ELSE LEN([Name]) > @p1 END");
        result.Parameters["p0"].Should().Be(5);
        result.Parameters["p1"].Should().Be(0);
    }
}
