using FluentAssertions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;
using Xunit;
using System.Linq.Expressions;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for DateTime.AddDays/AddMonths/AddYears translation in ExpressionToSqlVisitor.</summary>
public sealed class ExpressionToSqlDateFunctionTests
{
    private static readonly ISqlDialect SqlServer = new SqlServerDialect();
    private static readonly ISqlDialect Postgres  = new PostgreSqlDialect();
    private static readonly ISqlDialect MySql     = new MySqlDialect();
    private static readonly ISqlDialect Sqlite    = new SqliteDialect();

    private static readonly DateTime Threshold = new DateTime(2024, 1, 1);

    private static SqlResult Translate(Expression<Func<TestUser, bool>> expr, ISqlDialect dialect)
        => ExpressionToSqlVisitor.Translate(expr, dialect);

    // ── AddDays ───────────────────────────────────────────────────────────────

    [Fact]
    public void AddDays_SqlServer_EmitsDATEADD()
    {
        var result = Translate(x => x.CreatedAt.AddDays(5) > Threshold, SqlServer);
        result.Sql.Should().Be("DATEADD(DAY, 5, [CreatedAt]) > @p0");
        result.Parameters["p0"].Should().Be(Threshold);
    }

    [Fact]
    public void AddDays_PostgreSQL_EmitsIntervalArithmetic()
    {
        var result = Translate(x => x.CreatedAt.AddDays(5) > Threshold, Postgres);
        result.Sql.Should().Be("(\"CreatedAt\" + (5 * INTERVAL '1 day')) > @p0");
    }

    [Fact]
    public void AddDays_MySQL_EmitsDATE_ADD()
    {
        var result = Translate(x => x.CreatedAt.AddDays(5) > Threshold, MySql);
        result.Sql.Should().Be("DATE_ADD(`CreatedAt`, INTERVAL 5 DAY) > @p0");
    }

    [Fact]
    public void AddDays_SQLite_EmitsDateFunction()
    {
        var result = Translate(x => x.CreatedAt.AddDays(5) > Threshold, Sqlite);
        result.Sql.Should().Be("date(\"CreatedAt\", '5 day') > @p0");
    }

    // ── AddMonths ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddMonths_SqlServer_EmitsDATEADD_Month()
    {
        var result = Translate(x => x.CreatedAt.AddMonths(3) > Threshold, SqlServer);
        result.Sql.Should().Be("DATEADD(MONTH, 3, [CreatedAt]) > @p0");
    }

    [Fact]
    public void AddMonths_PostgreSQL_EmitsIntervalArithmetic()
    {
        var result = Translate(x => x.CreatedAt.AddMonths(3) > Threshold, Postgres);
        result.Sql.Should().Be("(\"CreatedAt\" + (3 * INTERVAL '1 month')) > @p0");
    }

    [Fact]
    public void AddMonths_MySQL_EmitsDATE_ADD_Month()
    {
        var result = Translate(x => x.CreatedAt.AddMonths(3) > Threshold, MySql);
        result.Sql.Should().Be("DATE_ADD(`CreatedAt`, INTERVAL 3 MONTH) > @p0");
    }

    [Fact]
    public void AddMonths_SQLite_EmitsDateFunction_Month()
    {
        var result = Translate(x => x.CreatedAt.AddMonths(3) > Threshold, Sqlite);
        result.Sql.Should().Be("date(\"CreatedAt\", '3 month') > @p0");
    }

    // ── AddYears ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddYears_SqlServer_EmitsDATEADD_Year()
    {
        var result = Translate(x => x.CreatedAt.AddYears(1) > Threshold, SqlServer);
        result.Sql.Should().Be("DATEADD(YEAR, 1, [CreatedAt]) > @p0");
    }

    [Fact]
    public void AddYears_PostgreSQL_EmitsIntervalArithmetic()
    {
        var result = Translate(x => x.CreatedAt.AddYears(1) > Threshold, Postgres);
        result.Sql.Should().Be("(\"CreatedAt\" + (1 * INTERVAL '1 year')) > @p0");
    }

    [Fact]
    public void AddYears_MySQL_EmitsDATE_ADD_Year()
    {
        var result = Translate(x => x.CreatedAt.AddYears(1) > Threshold, MySql);
        result.Sql.Should().Be("DATE_ADD(`CreatedAt`, INTERVAL 1 YEAR) > @p0");
    }

    [Fact]
    public void AddYears_SQLite_EmitsDateFunction_Year()
    {
        var result = Translate(x => x.CreatedAt.AddYears(1) > Threshold, Sqlite);
        result.Sql.Should().Be("date(\"CreatedAt\", '1 year') > @p0");
    }
}
