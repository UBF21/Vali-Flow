using FluentAssertions;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;
using Vali_Flow.Sql.Translators;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>
/// Tests for null argument guards and edge cases in the Sql library.
/// </summary>
public sealed class SqlNullGuardTests
{
    // ── ExpressionToSqlVisitor null guards ────────────────────────────────────

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
        var act = () => ExpressionToSqlVisitor.Translate<TestUser>(x => x.Id == 1, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dialect");
    }

    // ── SqlQueryBuilder null/empty guards ─────────────────────────────────────

    [Fact]
    public void From_NullTableName_ThrowsArgumentException()
    {
        var act = () => new SqlQueryBuilder<TestUser>(new SqlServerDialect()).From(null!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tableName");
    }

    [Fact]
    public void From_EmptyTableName_ThrowsArgumentException()
    {
        var act = () => new SqlQueryBuilder<TestUser>(new SqlServerDialect()).From("");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tableName");
    }

    // ── Empty Contains / list edge cases ─────────────────────────────────────

    /// <summary>
    /// An empty array used with the LINQ extension-method form (Enumerable.Contains) hits the
    /// static-method branch in ExpressionToSqlVisitor, which does NOT guard for empty collections.
    /// It produces "[Id] IN ()" — invalid SQL. This test documents the current behavior.
    ///
    /// The "1=0" guard only fires for the instance-method (.Contains on an ICollection object),
    /// not for the static Enumerable.Contains extension-method form.
    /// </summary>
    [Fact]
    public void EmptyArrayContains_EnumerableExtension_ProducesEmptyInClause()
    {
        var emptyIds = new int[] { };

        var result = ExpressionToSqlVisitor.Translate<TestUser>(
            x => emptyIds.Contains(x.Id),
            new SqlServerDialect());

        // Fixed behavior: empty collection now emits "1=0" (always false) instead of invalid "[Id] IN ()".
        result.Sql.Should().Be("1=0");
    }

    /// <summary>
    /// An IList.Contains instance-method call with an empty collection correctly emits "1=0"
    /// because the instance-method path explicitly guards for empty parameter lists.
    /// </summary>
    [Fact]
    public void EmptyListContains_InstanceMethod_GeneratesAlwaysFalseSql()
    {
        var emptyIds = new List<int>();

        var result = ExpressionToSqlVisitor.Translate<TestUser>(
            x => emptyIds.Contains(x.Id),
            new SqlServerDialect());

        result.Sql.Should().Be("1=0");
    }

    // ── SqlQueryBuilder chaining guards ──────────────────────────────────────

    [Fact]
    public void Take_Zero_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SqlQueryBuilder<TestUser>(new SqlServerDialect()).From("Users").Take(0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("count");
    }

    [Fact]
    public void Take_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SqlQueryBuilder<TestUser>(new SqlServerDialect()).From("Users").Take(-5);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("count");
    }

    [Fact]
    public void Skip_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SqlQueryBuilder<TestUser>(new SqlServerDialect()).From("Users").Skip(-1);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("count");
    }

    [Fact]
    public void Skip_Zero_DoesNotThrow()
    {
        var act = () => new SqlQueryBuilder<TestUser>(new SqlServerDialect()).From("Users").Skip(0);

        act.Should().NotThrow();
    }
}
