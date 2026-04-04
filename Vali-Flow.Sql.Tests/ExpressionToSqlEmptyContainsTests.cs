using System.Linq.Expressions;
using FluentAssertions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>
/// Tests for empty-collection and non-empty-collection Enumerable.Contains / instance Contains
/// in <see cref="ExpressionToSqlVisitor"/>.
/// </summary>
public sealed class ExpressionToSqlEmptyContainsTests
{
    private static SqlResult Translate(Expression<Func<TestUser, bool>> expr, ISqlDialect? dialect = null)
        => ExpressionToSqlVisitor.Translate(expr, dialect ?? new SqlServerDialect());

    // ── Test 6: IEnumerable<int> (static Enumerable.Contains path) ───────────

    [Fact]
    public void EmptyIEnumerable_StaticContainsPath_Produces1Equals0()
    {
        // Typed as IEnumerable<int> → compiles to Enumerable.Contains<int>(ids, x.Id)
        IEnumerable<int> ids = Enumerable.Empty<int>();
        var result = Translate(x => ids.Contains(x.Id));

        result.Sql.Should().Be("1=0");
        result.Parameters.Should().BeEmpty();
    }

    // ── Test 7: List<int> empty (instance Contains path, regression) ─────────

    [Fact]
    public void EmptyList_InstanceContainsPath_Produces1Equals0()
    {
        // Typed as List<int> → instance method path
        List<int> ids = new List<int>();
        var result = Translate(x => ids.Contains(x.Id));

        result.Sql.Should().Be("1=0");
        result.Parameters.Should().BeEmpty();
    }

    // ── Test 8: List<int> with values → IN clause ────────────────────────────

    [Fact]
    public void NonEmptyList_InstanceContainsPath_ProducesInClause()
    {
        List<int> ids = new List<int> { 1, 2, 3 };
        var result = Translate(x => ids.Contains(x.Id));

        result.Sql.Should().Be("[Id] IN (@p0, @p1, @p2)");
        result.Parameters["p0"].Should().Be(1);
        result.Parameters["p1"].Should().Be(2);
        result.Parameters["p2"].Should().Be(3);
    }

    // ── Test 9: Array.Empty<int>() as IEnumerable<int> (static path) ─────────

    [Fact]
    public void EmptyArrayAsIEnumerable_StaticContainsPath_Produces1Equals0()
    {
        // Cast to IEnumerable<int> forces the static Enumerable.Contains path
        IEnumerable<int> ids = Array.Empty<int>();
        var result = Translate(x => ids.Contains(x.Id));

        result.Sql.Should().Be("1=0");
        result.Parameters.Should().BeEmpty();
    }
}
