using System.Linq.Expressions;
using FluentAssertions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for remaining uncovered paths in ExpressionToSqlVisitor.</summary>
public sealed class ExpressionToSqlRemainingGapsTests
{
    private static readonly ISqlDialect SqlServer = new SqlServerDialect();
    private static readonly ISqlDialect Sqlite    = new SqliteDialect();
    private static readonly ISqlDialect Postgres  = new PostgreSqlDialect();

    private static SqlResult Translate<T>(Expression<Func<T, bool>> expr, ISqlDialect dialect)
        => ExpressionToSqlVisitor.Translate(expr, dialect);

    // ── Substring — single-argument overload ─────────────────────────────────

    [Fact]
    public void Substring_SingleArg_SqlServer_EmitsSubstringFromStart()
    {
        var result = Translate<TestUser>(
            x => x.Name.Substring(2) == "hn",
            SqlServer);

        // 0-indexed start 2 → 1-indexed start 3
        result.Sql.Should().Be("SUBSTRING([Name], 3) = @p0");
        result.Parameters["p0"].Should().Be("hn");
    }

    [Fact]
    public void Substring_SingleArg_SQLite_EmitsSubstr()
    {
        var result = Translate<TestUser>(
            x => x.Name.Substring(0) == "John",
            Sqlite);

        result.Sql.Should().Be("SUBSTR(\"Name\", 1) = @p0");
        result.Parameters["p0"].Should().Be("John");
    }

    [Fact]
    public void Substring_TwoArgs_SqlServer_EmitsSubstringWithLength()
    {
        var result = Translate<TestUser>(
            x => x.Name.Substring(1, 3) == "obn",
            SqlServer);

        result.Sql.Should().Be("SUBSTRING([Name], 2, 3) = @p0");
    }

    // ── DateTime.Hour / .Minute / .Second ────────────────────────────────────

    [Fact]
    public void DateTime_Hour_SqlServer_EmitsHourFunction()
    {
        var result = Translate<TestUser>(
            x => x.CreatedAt.Hour == 10,
            SqlServer);

        result.Sql.Should().Be("HOUR([CreatedAt]) = @p0");
        result.Parameters["p0"].Should().Be(10);
    }

    [Fact]
    public void DateTime_Minute_SqlServer_EmitsMinuteFunction()
    {
        var result = Translate<TestUser>(
            x => x.CreatedAt.Minute > 30,
            SqlServer);

        result.Sql.Should().Be("MINUTE([CreatedAt]) > @p0");
        result.Parameters["p0"].Should().Be(30);
    }

    [Fact]
    public void DateTime_Second_SqlServer_EmitsSecondFunction()
    {
        var result = Translate<TestUser>(
            x => x.CreatedAt.Second == 0,
            SqlServer);

        result.Sql.Should().Be("SECOND([CreatedAt]) = @p0");
        result.Parameters["p0"].Should().Be(0);
    }

    [Fact]
    public void DateTime_Hour_PostgreSQL_EmitsHourFunction()
    {
        var result = Translate<TestUser>(
            x => x.CreatedAt.Hour >= 9,
            Postgres);

        // PostgreSQL uses EXTRACT(HOUR FROM col) instead of HOUR(col)
        result.Sql.Should().Be("EXTRACT(HOUR FROM \"CreatedAt\") >= @p0");
    }

    // ── Math.Round — single-argument (no decimals) ───────────────────────────

    [Fact]
    public void MathRound_NoDecimals_SqlServer_EmitsRoundWithZero()
    {
        var result = Translate<TestUser>(
            x => Math.Round(x.Salary) > 100m,
            SqlServer);

        result.Sql.Should().Be("ROUND([Salary], 0) > @p0");
        result.Parameters["p0"].Should().Be(100m);
    }

    [Fact]
    public void MathRound_TwoDecimals_SqlServer_EmitsRoundWithDecimals()
    {
        var result = Translate<TestUser>(
            x => Math.Round(x.Salary, 2) > 100m,
            SqlServer);

        result.Sql.Should().Be("ROUND([Salary], 2) > @p0");
    }

    // ── ExpressionType.NegateChecked ─────────────────────────────────────────

    [Fact]
    public void NegateChecked_SqlServer_GeneratesNegationExpression()
    {
        // Build the expression tree manually: x => -(checked)x.Age > 0
        var param = Expression.Parameter(typeof(TestUser), "x");
        var prop = Expression.Property(param, nameof(TestUser.Age));
        var negChecked = Expression.NegateChecked(prop);
        var body = Expression.GreaterThan(negChecked, Expression.Constant(0));
        var lambda = Expression.Lambda<Func<TestUser, bool>>(body, param);

        var result = ExpressionToSqlVisitor.Translate(lambda, SqlServer);

        result.Sql.Should().Be("-([Age]) > @p0");
        result.Parameters["p0"].Should().Be(0);
    }

    [Fact]
    public void NegateChecked_PostgreSQL_GeneratesNegationExpression()
    {
        var param = Expression.Parameter(typeof(TestUser), "x");
        var prop = Expression.Property(param, nameof(TestUser.Age));
        var negChecked = Expression.NegateChecked(prop);
        var body = Expression.LessThan(negChecked, Expression.Constant(-5));
        var lambda = Expression.Lambda<Func<TestUser, bool>>(body, param);

        var result = ExpressionToSqlVisitor.Translate(lambda, Postgres);

        result.Sql.Should().Be("-(\"Age\") < @p0");
        result.Parameters["p0"].Should().Be(-5);
    }

    // ── StartsWith / EndsWith with empty string ───────────────────────────────

    [Fact]
    public void StartsWith_EmptyString_SqlServer_EmitsLikeWithPercent()
    {
        var result = Translate<TestUser>(
            x => x.Name.StartsWith(""),
            SqlServer);

        // value="" → pattern "%", matches everything
        result.Sql.Should().Be(@"[Name] LIKE @p0 ESCAPE '\'");
        result.Parameters["p0"].Should().Be("%");
    }

    [Fact]
    public void EndsWith_EmptyString_SqlServer_EmitsLikeWithPercent()
    {
        var result = Translate<TestUser>(
            x => x.Name.EndsWith(""),
            SqlServer);

        result.Sql.Should().Be(@"[Name] LIKE @p0 ESCAPE '\'");
        result.Parameters["p0"].Should().Be("%");
    }

    [Fact]
    public void StartsWith_NonEmpty_CombinedWithAnd_SqlServer()
    {
        var result = Translate<TestUser>(
            x => x.Name.StartsWith("Jo") && x.Age > 18,
            SqlServer);

        result.Sql.Should().Be(@"([Name] LIKE @p0 ESCAPE '\' AND [Age] > @p1)");
        result.Parameters["p0"].Should().Be("Jo%");
        result.Parameters["p1"].Should().Be(18);
    }
}
