using FluentAssertions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;
using Xunit;
using System.Linq.Expressions;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for Math function translation in ExpressionToSqlVisitor.</summary>
public sealed class ExpressionToSqlMathFunctionTests
{
    private static readonly ISqlDialect SqlServer = new SqlServerDialect();
    private static readonly ISqlDialect Postgres  = new PostgreSqlDialect();
    private static readonly ISqlDialect MySql     = new MySqlDialect();
    private static readonly ISqlDialect Sqlite    = new SqliteDialect();

    private static SqlResult Translate(Expression<Func<TestUser, bool>> expr, ISqlDialect dialect)
        => ExpressionToSqlVisitor.Translate(expr, dialect);

    // ── Math.Round ────────────────────────────────────────────────────────────

    [Fact]
    public void Round_SqlServer_EmitsROUND_WithDecimals()
    {
        var result = Translate(x => Math.Round(x.Salary, 2) == 1500m, SqlServer);
        result.Sql.Should().Be("ROUND([Salary], 2) = @p0");
        result.Parameters["p0"].Should().Be(1500m);
    }

    [Fact]
    public void Round_PostgreSQL_EmitsROUND()
    {
        var result = Translate(x => Math.Round(x.Salary, 2) == 1500m, Postgres);
        result.Sql.Should().Be("ROUND(\"Salary\", 2) = @p0");
    }

    [Fact]
    public void Round_MySQL_EmitsROUND()
    {
        var result = Translate(x => Math.Round(x.Salary, 2) == 1500m, MySql);
        result.Sql.Should().Be("ROUND(`Salary`, 2) = @p0");
    }

    [Fact]
    public void Round_SQLite_EmitsROUND()
    {
        var result = Translate(x => Math.Round(x.Salary, 2) == 1500m, Sqlite);
        result.Sql.Should().Be("ROUND(\"Salary\", 2) = @p0");
    }

    // ── Math.Ceiling ──────────────────────────────────────────────────────────

    [Fact]
    public void Ceiling_SqlServer_EmitsCEILING()
    {
        var result = Translate(x => Math.Ceiling(x.Salary) > 100m, SqlServer);
        result.Sql.Should().Be("CEILING([Salary]) > @p0");
    }

    [Fact]
    public void Ceiling_PostgreSQL_EmitsCEIL()
    {
        var result = Translate(x => Math.Ceiling(x.Salary) > 100m, Postgres);
        result.Sql.Should().Be("CEIL(\"Salary\") > @p0");
    }

    [Fact]
    public void Ceiling_MySQL_EmitsCEIL()
    {
        var result = Translate(x => Math.Ceiling(x.Salary) > 100m, MySql);
        result.Sql.Should().Be("CEIL(`Salary`) > @p0");
    }

    [Fact]
    public void Ceiling_SQLite_EmitsCEIL()
    {
        var result = Translate(x => Math.Ceiling(x.Salary) > 100m, Sqlite);
        result.Sql.Should().Be("CEIL(\"Salary\") > @p0");
    }

    // ── Math.Floor ────────────────────────────────────────────────────────────

    [Fact]
    public void Floor_SqlServer_EmitsFLOOR()
    {
        var result = Translate(x => Math.Floor(x.Salary) < 2000m, SqlServer);
        result.Sql.Should().Be("FLOOR([Salary]) < @p0");
    }

    [Fact]
    public void Floor_PostgreSQL_EmitsFLOOR()
    {
        var result = Translate(x => Math.Floor(x.Salary) < 2000m, Postgres);
        result.Sql.Should().Be("FLOOR(\"Salary\") < @p0");
    }

    [Fact]
    public void Floor_MySQL_EmitsFLOOR()
    {
        var result = Translate(x => Math.Floor(x.Salary) < 2000m, MySql);
        result.Sql.Should().Be("FLOOR(`Salary`) < @p0");
    }

    [Fact]
    public void Floor_SQLite_EmitsFLOOR()
    {
        var result = Translate(x => Math.Floor(x.Salary) < 2000m, Sqlite);
        result.Sql.Should().Be("FLOOR(\"Salary\") < @p0");
    }

    // ── Math.Sqrt ─────────────────────────────────────────────────────────────

    [Fact]
    public void Sqrt_SqlServer_EmitsSQRT()
    {
        var result = Translate(x => Math.Sqrt((double)x.Age) < 10.0, SqlServer);
        result.Sql.Should().Be("SQRT([Age]) < @p0");
    }

    [Fact]
    public void Sqrt_PostgreSQL_EmitsSQRT()
    {
        var result = Translate(x => Math.Sqrt((double)x.Age) < 10.0, Postgres);
        result.Sql.Should().Be("SQRT(\"Age\") < @p0");
    }

    [Fact]
    public void Sqrt_MySQL_EmitsSQRT()
    {
        var result = Translate(x => Math.Sqrt((double)x.Age) < 10.0, MySql);
        result.Sql.Should().Be("SQRT(`Age`) < @p0");
    }

    [Fact]
    public void Sqrt_SQLite_EmitsSQRT()
    {
        var result = Translate(x => Math.Sqrt((double)x.Age) < 10.0, Sqlite);
        result.Sql.Should().Be("SQRT(\"Age\") < @p0");
    }

    // ── Math.Pow ──────────────────────────────────────────────────────────────

    [Fact]
    public void Pow_SqlServer_EmitsPOWER()
    {
        var result = Translate(x => Math.Pow(x.Age, 2) > 100.0, SqlServer);
        result.Sql.Should().Be("POWER([Age], 2) > @p0");
    }

    [Fact]
    public void Pow_PostgreSQL_EmitsPOWER()
    {
        var result = Translate(x => Math.Pow(x.Age, 2) > 100.0, Postgres);
        result.Sql.Should().Be("POWER(\"Age\", 2) > @p0");
    }

    [Fact]
    public void Pow_MySQL_EmitsPOWER()
    {
        var result = Translate(x => Math.Pow(x.Age, 2) > 100.0, MySql);
        result.Sql.Should().Be("POWER(`Age`, 2) > @p0");
    }

    [Fact]
    public void Pow_SQLite_EmitsPOWER()
    {
        var result = Translate(x => Math.Pow(x.Age, 2) > 100.0, Sqlite);
        result.Sql.Should().Be("POWER(\"Age\", 2) > @p0");
    }
}
