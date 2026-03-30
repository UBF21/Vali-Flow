using Vali_Flow.Sql.Dialects;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for all four SQL dialects.</summary>
public sealed class DialectTests
{
    // ── SqlServerDialect ──────────────────────────────────────────────────────

    [Fact]
    public void SqlServer_QuoteIdentifier()
        => new SqlServerDialect().QuoteIdentifier("Name").Should().Be("[Name]");

    [Fact]
    public void SqlServer_NullCheck()
        => new SqlServerDialect().NullCheck("[Name]").Should().Be("[Name] IS NULL");

    [Fact]
    public void SqlServer_NotNullCheck()
        => new SqlServerDialect().NotNullCheck("[Name]").Should().Be("[Name] IS NOT NULL");

    [Fact]
    public void SqlServer_SelectTop_WithValue()
        => new SqlServerDialect().SelectTop(10).Should().Be("TOP 10 ");

    [Fact]
    public void SqlServer_SelectTop_Null_ReturnsEmpty()
        => new SqlServerDialect().SelectTop(null).Should().BeEmpty();

    [Fact]
    public void SqlServer_LimitOffset_TopOnly_ReturnsEmpty()
        => new SqlServerDialect().LimitOffset(10, 0).Should().BeEmpty();

    [Fact]
    public void SqlServer_LimitOffset_WithOffset()
        => new SqlServerDialect().LimitOffset(10, 5).Should().Be("OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY");

    [Fact]
    public void SqlServer_LimitOffset_OffsetOnly()
        => new SqlServerDialect().LimitOffset(null, 5).Should().Be("OFFSET 5 ROWS");

    [Fact]
    public void SqlServer_DatePartExpression_Year()
        => new SqlServerDialect().DatePartExpression("[CreatedAt]", "YEAR").Should().Be("YEAR([CreatedAt])");

    [Fact]
    public void SqlServer_TrueValue()
        => new SqlServerDialect().TrueValue.Should().Be("1");

    [Fact]
    public void SqlServer_FalseValue()
        => new SqlServerDialect().FalseValue.Should().Be("0");

    // ── PostgreSqlDialect ─────────────────────────────────────────────────────

    [Fact]
    public void PostgreSql_QuoteIdentifier()
        => new PostgreSqlDialect().QuoteIdentifier("Name").Should().Be("\"Name\"");

    [Fact]
    public void PostgreSql_ILikeExpression()
        => new PostgreSqlDialect().ILikeExpression("\"Name\"", "@p0").Should().Be("\"Name\" ILIKE @p0");

    [Fact]
    public void PostgreSql_LimitOffset_Both()
        => new PostgreSqlDialect().LimitOffset(10, 5).Should().Be("LIMIT 10 OFFSET 5");

    [Fact]
    public void PostgreSql_LimitOffset_TakeOnly()
        => new PostgreSqlDialect().LimitOffset(10, 0).Should().Be("LIMIT 10");

    [Fact]
    public void PostgreSql_LimitOffset_OffsetOnly()
        => new PostgreSqlDialect().LimitOffset(null, 5).Should().Be("OFFSET 5");

    [Fact]
    public void PostgreSql_LimitOffset_None()
        => new PostgreSqlDialect().LimitOffset(null, 0).Should().BeEmpty();

    [Fact]
    public void PostgreSql_DatePart_Year()
        => new PostgreSqlDialect().DatePartExpression("\"CreatedAt\"", "YEAR").Should().Be("EXTRACT(YEAR FROM \"CreatedAt\")");

    [Fact]
    public void PostgreSql_TrueValue()
        => new PostgreSqlDialect().TrueValue.Should().Be("TRUE");

    [Fact]
    public void PostgreSql_SelectTop_AlwaysEmpty()
        => ((ISqlDialect)new PostgreSqlDialect()).SelectTop(10).Should().BeEmpty();

    // ── MySqlDialect ──────────────────────────────────────────────────────────

    [Fact]
    public void MySql_QuoteIdentifier()
        => new MySqlDialect().QuoteIdentifier("Name").Should().Be("`Name`");

    [Fact]
    public void MySql_LimitOffset_Both()
        => new MySqlDialect().LimitOffset(10, 5).Should().Be("LIMIT 10 OFFSET 5");

    [Fact]
    public void MySql_LimitOffset_TakeOnly()
        => new MySqlDialect().LimitOffset(10, 0).Should().Be("LIMIT 10");

    [Fact]
    public void MySql_LimitOffset_OffsetOnly_UsesMaxBigint()
    {
        var result = new MySqlDialect().LimitOffset(null, 5);
        result.Should().Contain("OFFSET 5");
        result.Should().Contain("18446744073709551615");
    }

    [Fact]
    public void MySql_DatePart_Month()
        => new MySqlDialect().DatePartExpression("`CreatedAt`", "MONTH").Should().Be("MONTH(`CreatedAt`)");

    // ── SqliteDialect ─────────────────────────────────────────────────────────

    [Fact]
    public void Sqlite_QuoteIdentifier()
        => new SqliteDialect().QuoteIdentifier("Name").Should().Be("\"Name\"");

    [Fact]
    public void Sqlite_LimitOffset_Both()
        => new SqliteDialect().LimitOffset(10, 5).Should().Be("LIMIT 10 OFFSET 5");

    [Fact]
    public void Sqlite_DatePart_Year()
        => new SqliteDialect().DatePartExpression("\"CreatedAt\"", "YEAR").Should().Be("strftime('%Y', \"CreatedAt\")");

    [Fact]
    public void Sqlite_DatePart_Month()
        => new SqliteDialect().DatePartExpression("\"dt\"", "MONTH").Should().Be("strftime('%m', \"dt\")");

    [Fact]
    public void Sqlite_DatePart_Day()
        => new SqliteDialect().DatePartExpression("\"dt\"", "DAY").Should().Be("strftime('%d', \"dt\")");

    [Fact]
    public void Sqlite_DatePart_Hour()
        => new SqliteDialect().DatePartExpression("\"dt\"", "HOUR").Should().Be("strftime('%H', \"dt\")");

    [Fact]
    public void Sqlite_LimitOffset_OffsetOnly()
        => new SqliteDialect().LimitOffset(null, 3).Should().Be("OFFSET 3");

    // ── Default interface implementations ─────────────────────────────────────

    [Fact]
    public void DefaultOrderByAscending_IsAsc()
        => ((ISqlDialect)new SqlServerDialect()).OrderByAscending.Should().Be("ASC");

    [Fact]
    public void DefaultOrderByDescending_IsDesc()
        => ((ISqlDialect)new SqlServerDialect()).OrderByDescending.Should().Be("DESC");

    [Fact]
    public void DefaultParameterPrefix_IsAt()
    {
        ((ISqlDialect)new SqlServerDialect()).ParameterPrefix.Should().Be("@");
        ((ISqlDialect)new PostgreSqlDialect()).ParameterPrefix.Should().Be("@");
        ((ISqlDialect)new MySqlDialect()).ParameterPrefix.Should().Be("@");
        ((ISqlDialect)new SqliteDialect()).ParameterPrefix.Should().Be("@");
    }
}
