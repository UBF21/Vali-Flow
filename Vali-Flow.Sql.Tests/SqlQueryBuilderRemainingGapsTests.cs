using FluentAssertions;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Tests.Models;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for remaining uncovered paths in SqlQueryBuilder.</summary>
public sealed class SqlQueryBuilderRemainingGapsTests
{
    private static SqlQueryBuilder<TestUser> Sql()
        => new(new SqlServerDialect());

    // ── SelectRawIf — false condition (no-op) ────────────────────────────────

    [Fact]
    public void SelectRawIf_False_DoesNotAddRawColumn()
    {
        var result = Sql()
            .From("Users")
            .SelectRawIf(false, "1 AS Dummy")
            .Build();

        result.Sql.Should().Be("SELECT * FROM [Users]");
        result.Sql.Should().NotContain("Dummy");
    }

    [Fact]
    public void SelectRawIf_True_AddsRawColumn()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectRawIf(true, "1 AS IsAdmin")
            .Build();

        result.Sql.Should().Contain("1 AS IsAdmin");
    }

    [Fact]
    public void SelectRawIf_False_AfterOtherSelects_DoesNotAlterQuery()
    {
        var withTrue = Sql().From("Users").Select(x => x.Id).SelectRawIf(true,  "1 AS Flag").Build();
        var withFalse = Sql().From("Users").Select(x => x.Id).SelectRawIf(false, "1 AS Flag").Build();

        withTrue.Sql.Should().Contain("Flag");
        withFalse.Sql.Should().NotContain("Flag");
    }

    // ── Except — null guard ───────────────────────────────────────────────────

    [Fact]
    public void Except_NullQuery_ThrowsArgumentNullException()
    {
        var act = () => Sql().From("Users").Except(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("other");
    }

    [Fact]
    public void Except_ValidQuery_AppendsExceptClause()
    {
        var sub = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Archived").Build();

        var result = Sql()
            .From("Users")
            .Except(sub)
            .Build();

        result.Sql.Should().Contain("EXCEPT");
    }

    // ── Intersect — null guard ────────────────────────────────────────────────

    [Fact]
    public void Intersect_NullQuery_ThrowsArgumentNullException()
    {
        var act = () => Sql().From("Users").Intersect(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("other");
    }

    [Fact]
    public void Intersect_ValidQuery_AppendsIntersectClause()
    {
        var sub = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Premium").Build();

        var result = Sql()
            .From("Users")
            .Intersect(sub)
            .Build();

        result.Sql.Should().Contain("INTERSECT");
    }

    // ── HavingIf — raw string overload ────────────────────────────────────────

    [Fact]
    public void HavingIf_RawString_True_AppliesHavingClause()
    {
        var result = Sql()
            .From("Orders")
            .Select(x => x.Department)
            .GroupBy(x => x.Department)
            .HavingIf(true, "COUNT(*) > 5")
            .Build();

        result.Sql.Should().Contain("HAVING COUNT(*) > 5");
    }

    [Fact]
    public void HavingIf_RawString_False_SkipsHavingClause()
    {
        var result = Sql()
            .From("Orders")
            .Select(x => x.Department)
            .GroupBy(x => x.Department)
            .HavingIf(false, "COUNT(*) > 5")
            .Build();

        result.Sql.Should().NotContain("HAVING");
    }

    // ── Tag — null logger path ────────────────────────────────────────────────

    [Fact]
    public void Tag_WithNullLogger_EmbedsSqlCommentWithoutInvokingCallback()
    {
        var result = Sql()
            .From("Users")
            .Tag("load-all-users", null)
            .Build();

        // Tag is embedded as SQL comment; no callback to invoke
        result.Sql.Should().StartWith("-- load-all-users");
    }

    [Fact]
    public void Tag_WithNullLogger_DoesNotThrow()
    {
        var act = () => Sql()
            .From("Users")
            .Tag("my-query", (Action<string>?)null)
            .Build();

        act.Should().NotThrow();
    }
}
