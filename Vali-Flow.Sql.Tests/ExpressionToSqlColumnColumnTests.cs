using System.Linq.Expressions;
using FluentAssertions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>
/// Tests for column-vs-column bitwise/arithmetic expressions and edge cases
/// (negative constants, <c>SqlQueryBuilder.Where(null)</c>).
/// </summary>
public sealed class ExpressionToSqlColumnColumnTests
{
    private static SqlResult Translate(Expression<Func<TestUser, bool>> expr, ISqlDialect dialect)
        => ExpressionToSqlVisitor.Translate(expr, dialect);

    // ── Section 1 — Column & Column (bitwise / arithmetic between two columns) ──

    /// <summary>
    /// (x.Status &amp; x.Counter) == 1 on SqlServer.
    /// The fix in IsColumnExpression allows the right operand of the arithmetic
    /// node to be emitted as a column reference instead of being evaluated as a constant.
    /// Expected: [Status] &amp; [Counter] = @p0  with p0 = 1 (no parameters for the two columns).
    /// </summary>
    [Fact]
    public void ColumnBitwiseAndColumn_SqlServer_EmitsBothColumnsNoExtraParams()
    {
        var result = Translate(x => (x.Status & x.Counter) == 1, new SqlServerDialect());

        result.Sql.Should().Be("[Status] & [Counter] = @p0");
        result.Parameters.Should().ContainSingle();
        result.Parameters["p0"].Should().Be(1);
    }

    /// <summary>
    /// (x.Status &amp; x.Counter) == 1 on PostgreSQL.
    /// Expected: "Status" &amp; "Counter" = @p0  with p0 = 1.
    /// </summary>
    [Fact]
    public void ColumnBitwiseAndColumn_PostgreSql_EmitsBothColumnsDoubleQuoted()
    {
        var result = Translate(x => (x.Status & x.Counter) == 1, new PostgreSqlDialect());

        result.Sql.Should().Be("\"Status\" & \"Counter\" = @p0");
        result.Parameters.Should().ContainSingle();
        result.Parameters["p0"].Should().Be(1);
    }

    /// <summary>
    /// (x.Age + x.Counter) &gt; 18 on SqlServer.
    /// Expected: [Age] + [Counter] &gt; @p0  with p0 = 18.
    /// </summary>
    [Fact]
    public void ColumnAddColumn_SqlServer_EmitsBothColumnsGreaterThan()
    {
        var result = Translate(x => (x.Age + x.Counter) > 18, new SqlServerDialect());

        result.Sql.Should().Be("[Age] + [Counter] > @p0");
        result.Parameters.Should().ContainSingle();
        result.Parameters["p0"].Should().Be(18);
    }

    /// <summary>
    /// (x.Age * x.Counter) &gt; 100 on SqlServer.
    /// Expected: [Age] * [Counter] &gt; @p0  with p0 = 100.
    /// </summary>
    [Fact]
    public void ColumnMultiplyColumn_SqlServer_EmitsBothColumnsGreaterThan()
    {
        var result = Translate(x => (x.Age * x.Counter) > 100, new SqlServerDialect());

        result.Sql.Should().Be("[Age] * [Counter] > @p0");
        result.Parameters.Should().ContainSingle();
        result.Parameters["p0"].Should().Be(100);
    }

    /// <summary>
    /// (x.Status | x.Counter) &gt; 0 on SqlServer.
    /// Expected: [Status] | [Counter] &gt; @p0  with p0 = 0.
    /// </summary>
    [Fact]
    public void ColumnBitwiseOrColumn_SqlServer_EmitsBothColumnsGreaterThanZero()
    {
        var result = Translate(x => (x.Status | x.Counter) > 0, new SqlServerDialect());

        result.Sql.Should().Be("[Status] | [Counter] > @p0");
        result.Parameters.Should().ContainSingle();
        result.Parameters["p0"].Should().Be(0);
    }

    // ── Section 2 — Negative constants ──────────────────────────────────────────

    /// <summary>
    /// x.Age &gt; -1 on SqlServer.
    /// The unary negation should be folded into the constant; the parameter value must be -1.
    /// Expected: [Age] &gt; @p0  with p0 = -1.
    /// </summary>
    [Fact]
    public void NegativeIntConstant_SqlServer_ParameterValueIsNegativeOne()
    {
        var result = Translate(x => x.Age > -1, new SqlServerDialect());

        result.Sql.Should().Be("[Age] > @p0");
        result.Parameters.Should().ContainSingle();
        result.Parameters["p0"].Should().Be(-1);
    }

    /// <summary>
    /// x.Salary &gt; -100.5m on SqlServer.
    /// Expected: [Salary] &gt; @p0  with p0 = -100.5m (decimal).
    /// </summary>
    [Fact]
    public void NegativeDecimalConstant_SqlServer_ParameterValueIsNegativeDecimal()
    {
        var result = Translate(x => x.Salary > -100.5m, new SqlServerDialect());

        result.Sql.Should().Be("[Salary] > @p0");
        result.Parameters.Should().ContainSingle();
        result.Parameters["p0"].Should().Be(-100.5m);
    }

    // ── Section 3 — SqlQueryBuilder.Where(null) behavior ────────────────────────

    /// <summary>
    /// <c>SqlQueryBuilder&lt;T&gt;.Where(ValiFlow&lt;T&gt; filter)</c> guards against null by throwing
    /// <see cref="ArgumentNullException"/> immediately — the null is NOT silently ignored.
    /// This documents the deliberate defensive behavior: passing null is a programming error.
    /// </summary>
    [Fact]
    public void SqlQueryBuilderWhereValiFlow_NullFilter_ThrowsArgumentNullException()
    {
        var builder = new Vali_Flow.Sql.Builder.SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users");

        var act = () => builder.Where((Vali_Flow.Core.Builder.ValiFlow<TestUser>)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("filter");
    }

    /// <summary>
    /// <c>SqlQueryBuilder&lt;T&gt;.Where(Expression&lt;Func&lt;T,bool&gt;&gt; predicate)</c> also throws
    /// <see cref="ArgumentNullException"/> when passed null — consistent with the ValiFlow overload.
    /// </summary>
    [Fact]
    public void SqlQueryBuilderWherePredicate_NullExpression_ThrowsArgumentNullException()
    {
        var builder = new Vali_Flow.Sql.Builder.SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users");

        var act = () => builder.Where((Expression<Func<TestUser, bool>>)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("predicate");
    }
}
