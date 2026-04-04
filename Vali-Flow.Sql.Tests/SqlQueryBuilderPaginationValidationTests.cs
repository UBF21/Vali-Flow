using FluentAssertions;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for pagination ORDER BY validation in SqlQueryBuilder.</summary>
public sealed class SqlQueryBuilderPaginationValidationTests
{
    private static SqlQueryBuilder<TestUser> Sql()   => new(new SqlServerDialect());
    private static SqlQueryBuilder<TestUser> PgSql() => new(new PostgreSqlDialect());
    private static SqlQueryBuilder<TestUser> MySql() => new(new MySqlDialect());
    private static SqlQueryBuilder<TestUser> Sqlite() => new(new SqliteDialect());

    // ── Take without ORDER BY → should throw ─────────────────────────────────

    [Fact]
    public void Build_TakeWithoutOrderBy_SqlServer_Throws()
    {
        var act = () => Sql().From("Users").Take(10).Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*LIMIT/TOP/OFFSET requires at least one ORDER BY*");
    }

    [Fact]
    public void Build_TakeWithoutOrderBy_PostgreSQL_Throws()
    {
        var act = () => PgSql().From("users").Take(10).Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Build_TakeWithoutOrderBy_MySQL_Throws()
    {
        var act = () => MySql().From("users").Take(10).Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Build_TakeWithoutOrderBy_SQLite_Throws()
    {
        var act = () => Sqlite().From("users").Take(10).Build();
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Skip without ORDER BY → should throw ─────────────────────────────────

    [Fact]
    public void Build_SkipWithoutOrderBy_SqlServer_Throws()
    {
        var act = () => Sql().From("Users").Skip(10).Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Build_SkipWithoutOrderBy_PostgreSQL_Throws()
    {
        var act = () => PgSql().From("users").Skip(10).Build();
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Take WITH ORDER BY → should succeed ──────────────────────────────────

    [Fact]
    public void Build_TakeWithOrderBy_SqlServer_DoesNotThrow()
    {
        var act = () => Sql().From("Users").OrderBy(x => x.Id).Take(10).Build();
        act.Should().NotThrow();
    }

    [Fact]
    public void Build_TakeWithOrderBy_PostgreSQL_DoesNotThrow()
    {
        var act = () => PgSql().From("users").OrderBy(x => x.Id).Take(10).Build();
        act.Should().NotThrow();
    }

    // ── Page 1 (no OFFSET) with ORDER BY → should succeed ────────────────────

    [Fact]
    public void Build_Page1WithOrderBy_PostgreSQL_ProducesLimitOnly()
    {
        var result = PgSql().From("users").OrderBy(x => x.Id).Page(1, 10).Build();
        result.Sql.Should().Contain("LIMIT 10");
        result.Sql.Should().NotContain("OFFSET");
    }

    // ── No Take / Skip → should succeed without ORDER BY ─────────────────────

    [Fact]
    public void Build_NoTakeNoSkip_WithoutOrderBy_DoesNotThrow()
    {
        var act = () => Sql().From("Users").Where(x => x.IsActive).Build();
        act.Should().NotThrow();
    }
}
