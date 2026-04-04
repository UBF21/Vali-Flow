using System.Linq.Expressions;
using FluentAssertions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for bitwise operators (&amp;, |, ^) in <see cref="ExpressionToSqlVisitor"/>.</summary>
public sealed class ExpressionToSqlBitwiseTests
{
    private static SqlResult Translate(Expression<Func<TestUser, bool>> expr, ISqlDialect dialect)
        => ExpressionToSqlVisitor.Translate(expr, dialect);

    // ── Section 1: Bitwise AND (&) ────────────────────────────────────────────

    [Fact]
    public void BitwiseAnd_SqlServer_ProducesAndOperatorWithBrackets()
    {
        // (x.Status & 3) == 3  →  [Status] & @p0 = @p1
        var result = Translate(x => (x.Status & 3) == 3, new SqlServerDialect());

        result.Sql.Should().Contain("[Status] & @p0");
        result.Sql.Should().Contain("= @p1");
        result.Parameters["p0"].Should().Be(3);
        result.Parameters["p1"].Should().Be(3);
    }

    [Fact]
    public void BitwiseAnd_PostgreSql_ProducesAndOperatorWithDoubleQuotes()
    {
        // (x.Status & 1) == 1  →  "Status" & @p0 = @p1
        var result = Translate(x => (x.Status & 1) == 1, new PostgreSqlDialect());

        result.Sql.Should().Contain("\"Status\" & @p0");
        result.Parameters["p0"].Should().Be(1);
        result.Parameters["p1"].Should().Be(1);
    }

    [Fact]
    public void BitwiseAnd_MySql_ProducesAndOperatorWithBackticks()
    {
        // (x.Status & 4) == 4  →  `Status` & @p0 = @p1
        var result = Translate(x => (x.Status & 4) == 4, new MySqlDialect());

        result.Sql.Should().Contain("`Status` & @p0");
        result.Parameters["p0"].Should().Be(4);
        result.Parameters["p1"].Should().Be(4);
    }

    // ── Section 2: Bitwise OR (|) ─────────────────────────────────────────────

    [Fact]
    public void BitwiseOr_SqlServer_ProducesOrOperatorWithBrackets()
    {
        // (x.Status | 2) > 0  →  [Status] | @p0 > @p1
        var result = Translate(x => (x.Status | 2) > 0, new SqlServerDialect());

        result.Sql.Should().Contain("[Status] | @p0");
        result.Parameters["p0"].Should().Be(2);
        result.Parameters["p1"].Should().Be(0);
    }

    // ── Section 3: Bitwise XOR (^) ────────────────────────────────────────────

    [Fact]
    public void BitwiseXor_SqlServer_ProducesXorOperatorWithBrackets()
    {
        // (x.Status ^ 1) == 0  →  [Status] ^ @p0 = @p1
        var result = Translate(x => (x.Status ^ 1) == 0, new SqlServerDialect());

        result.Sql.Should().Contain("[Status] ^ @p0");
        result.Sql.Should().Contain("= @p1");
        result.Parameters["p0"].Should().Be(1);
        result.Parameters["p1"].Should().Be(0);
    }
}
