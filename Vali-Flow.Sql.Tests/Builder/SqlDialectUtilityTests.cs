using FluentAssertions;
using Vali_Flow.Sql.Dialects;

namespace Vali_Flow.Sql.Tests.Builder;

/// <summary>Tests for <see cref="ISqlDialect"/> utility expression methods.</summary>
public sealed class SqlDialectUtilityTests
{
    private static readonly ISqlDialect SqlServer = new SqlServerDialect();
    private static readonly ISqlDialect PostgreSql = new PostgreSqlDialect();
    private static readonly ISqlDialect MySql = new MySqlDialect();
    private static readonly ISqlDialect Sqlite = new SqliteDialect();

    // ── CastExpression ────────────────────────────────────────────────────────

    [Fact]
    public void CastExpression_Default_ReturnsCastFragment()
    {
        SqlServer.CastExpression("[col]", "INT").Should().Be("CAST([col] AS INT)");
    }

    [Fact]
    public void CastExpression_PostgreSQL_ReturnsCastFragment()
    {
        PostgreSql.CastExpression("\"col\"", "INTEGER").Should().Be("CAST(\"col\" AS INTEGER)");
    }

    [Fact]
    public void CastExpression_MySQL_ReturnsCastFragment()
    {
        MySql.CastExpression("`col`", "UNSIGNED").Should().Be("CAST(`col` AS UNSIGNED)");
    }

    [Fact]
    public void CastExpression_SQLite_ReturnsCastFragment()
    {
        Sqlite.CastExpression("\"col\"", "REAL").Should().Be("CAST(\"col\" AS REAL)");
    }

    // ── CoalesceExpression ────────────────────────────────────────────────────

    [Fact]
    public void CoalesceExpression_Default_ReturnsCoalesceFragment()
    {
        SqlServer.CoalesceExpression("[col]", "'N/A'").Should().Be("COALESCE([col], 'N/A')");
    }

    [Fact]
    public void CoalesceExpression_PostgreSQL_ReturnsCoalesceFragment()
    {
        PostgreSql.CoalesceExpression("\"col\"", "'N/A'").Should().Be("COALESCE(\"col\", 'N/A')");
    }

    // ── ConcatExpression ──────────────────────────────────────────────────────

    [Fact]
    public void ConcatExpression_SqlServer_UsesPlusOperator()
    {
        SqlServer.ConcatExpression("[a]", "[b]").Should().Be("[a] + [b]");
    }

    [Fact]
    public void ConcatExpression_PostgreSQL_UsesPipeOperator()
    {
        PostgreSql.ConcatExpression("\"a\"", "\"b\"").Should().Be("\"a\" || \"b\"");
    }

    [Fact]
    public void ConcatExpression_MySQL_UsesConcatFunction()
    {
        MySql.ConcatExpression("`a`", "`b`").Should().Be("CONCAT(`a`, `b`)");
    }

    [Fact]
    public void ConcatExpression_SQLite_UsesPipeOperator()
    {
        Sqlite.ConcatExpression("\"a\"", "\"b\"").Should().Be("\"a\" || \"b\"");
    }

    [Fact]
    public void ConcatExpression_SqlServer_ThreeParts_UsesPlusOperator()
    {
        SqlServer.ConcatExpression("[a]", "[b]", "[c]").Should().Be("[a] + [b] + [c]");
    }

    [Fact]
    public void ConcatExpression_MySQL_ThreeParts_UsesConcatFunction()
    {
        MySql.ConcatExpression("`a`", "`b`", "`c`").Should().Be("CONCAT(`a`, `b`, `c`)");
    }

    // ── EscapeLikeValue ───────────────────────────────────────────────────────

    [Fact]
    public void EscapeLikeValue_Default_EscapesPercentAndUnderscore()
    {
        PostgreSql.EscapeLikeValue("50% off_sale").Should().Be("50\\% off\\_sale");
    }

    [Fact]
    public void EscapeLikeValue_SqlServer_AlsoEscapesBracket()
    {
        SqlServer.EscapeLikeValue("50% [off]_sale").Should().Be("50\\% \\[off]\\_sale");
    }

    [Fact]
    public void EscapeLikeValue_SQLite_EscapesPercentAndUnderscore()
    {
        Sqlite.EscapeLikeValue("100%_done").Should().Be("100\\%\\_done");
    }

    // ── CurrentTimestamp ──────────────────────────────────────────────────────

    [Fact]
    public void CurrentTimestamp_SqlServer_ReturnsGetDate()
    {
        SqlServer.CurrentTimestamp.Should().Be("GETDATE()");
    }

    [Fact]
    public void CurrentTimestamp_PostgreSQL_ReturnsNow()
    {
        PostgreSql.CurrentTimestamp.Should().Be("NOW()");
    }

    [Fact]
    public void CurrentTimestamp_MySQL_ReturnsNow()
    {
        MySql.CurrentTimestamp.Should().Be("NOW()");
    }

    [Fact]
    public void CurrentTimestamp_SQLite_ReturnsCurrentTimestamp()
    {
        Sqlite.CurrentTimestamp.Should().Be("CURRENT_TIMESTAMP");
    }
}
