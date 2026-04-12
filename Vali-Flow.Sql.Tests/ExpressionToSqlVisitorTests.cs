using System.Linq.Expressions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for <see cref="ExpressionToSqlVisitor"/>.</summary>
public sealed class ExpressionToSqlVisitorTests
{
    private static SqlResult Translate(Expression<Func<TestUser, bool>> expr, ISqlDialect? dialect = null)
        => ExpressionToSqlVisitor.Translate(expr, dialect ?? new SqlServerDialect());

    // ── Basic comparisons ─────────────────────────────────────────────────────

    [Fact]
    public void Equal_GeneratesEqualOperator()
    {
        var result = Translate(x => x.Id == 5);
        result.Sql.Should().Be("[Id] = @p0");
        result.Parameters["p0"].Should().Be(5);
    }

    [Fact]
    public void NotEqual_GeneratesNotEqualOperator()
    {
        var result = Translate(x => x.Status != 0);
        result.Sql.Should().Be("[Status] != @p0");
        result.Parameters["p0"].Should().Be(0);
    }

    [Fact]
    public void GreaterThan()
    {
        var result = Translate(x => x.Age > 18);
        result.Sql.Should().Be("[Age] > @p0");
        result.Parameters["p0"].Should().Be(18);
    }

    [Fact]
    public void GreaterThanOrEqual()
    {
        var result = Translate(x => x.Age >= 21);
        result.Sql.Should().Be("[Age] >= @p0");
    }

    [Fact]
    public void LessThan()
    {
        var result = Translate(x => x.Age < 65);
        result.Sql.Should().Be("[Age] < @p0");
    }

    [Fact]
    public void LessThanOrEqual()
    {
        var result = Translate(x => x.Age <= 100);
        result.Sql.Should().Be("[Age] <= @p0");
    }

    // ── BUG-07: constant on left side ─────────────────────────────────────────

    [Fact]
    public void BUG07_ConstantOnLeftSide_LessThan_FlipsToGreaterThan()
    {
        // 18 < x.Age  should become  [Age] > @p0
        var result = Translate(x => 18 < x.Age);
        result.Sql.Should().Be("[Age] > @p0");
        result.Parameters["p0"].Should().Be(18);
    }

    [Fact]
    public void BUG07_ConstantOnLeftSide_GreaterThan_FlipsToLessThan()
    {
        var result = Translate(x => 100 > x.Age);
        result.Sql.Should().Be("[Age] < @p0");
    }

    // ── BUG-08: bool member as condition ──────────────────────────────────────

    [Fact]
    public void BUG08_BooleanMemberAsCondition_GeneratesEqualsOne()
    {
        var result = Translate(x => x.IsActive);
        result.Sql.Should().Be("[IsActive] = 1");
    }

    // ── NULL checks ───────────────────────────────────────────────────────────

    [Fact]
    public void NullCheck_EqualToNull_GeneratesIsNull()
    {
        var result = Translate(x => x.Name == null);
        result.Sql.Should().Be("[Name] IS NULL");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void NullCheck_NotEqualToNull_GeneratesIsNotNull()
    {
        var result = Translate(x => x.Name != null);
        result.Sql.Should().Be("[Name] IS NOT NULL");
    }

    // ── AND / OR ──────────────────────────────────────────────────────────────

    [Fact]
    public void AndAlso_GeneratesAndWithGrouping()
    {
        var result = Translate(x => x.Age > 18 && x.IsActive);
        result.Sql.Should().Be("([Age] > @p0 AND [IsActive] = 1)");
    }

    [Fact]
    public void OrElse_GeneratesOrWithGrouping()
    {
        var result = Translate(x => x.Age < 18 || x.Age > 65);
        result.Sql.Should().Be("([Age] < @p0 OR [Age] > @p1)");
    }

    [Fact]
    public void Not_GeneratesNotWithParentheses()
    {
        var result = Translate(x => !(x.Age > 18));
        result.Sql.Should().Be("NOT ([Age] > @p0)");
    }

    // ── String methods ────────────────────────────────────────────────────────

    [Fact]
    public void Contains_GeneratesLikeWithPercentBoth()
    {
        var result = Translate(x => x.Name.Contains("john"));
        result.Sql.Should().Be(@"[Name] LIKE @p0 ESCAPE '\'");
        result.Parameters["p0"].Should().Be("%john%");
    }

    [Fact]
    public void StartsWith_GeneratesLikeWithTrailingPercent()
    {
        var result = Translate(x => x.Name.StartsWith("Jo"));
        result.Sql.Should().Be(@"[Name] LIKE @p0 ESCAPE '\'");
        result.Parameters["p0"].Should().Be("Jo%");
    }

    [Fact]
    public void EndsWith_GeneratesLikeWithLeadingPercent()
    {
        var result = Translate(x => x.Name.EndsWith("son"));
        result.Sql.Should().Be(@"[Name] LIKE @p0 ESCAPE '\'");
        result.Parameters["p0"].Should().Be("%son");
    }

    [Fact]
    public void ToLower_GeneratesLower()
    {
        var result = Translate(x => x.Name.ToLower() == "john");
        result.Sql.Should().Be("LOWER([Name]) = @p0");
    }

    [Fact]
    public void ToUpper_GeneratesUpper()
    {
        var result = Translate(x => x.Name.ToUpper() == "JOHN");
        result.Sql.Should().Be("UPPER([Name]) = @p0");
    }

    // ── Arithmetic ────────────────────────────────────────────────────────────

    [Fact]
    public void Arithmetic_Multiply_GeneratesInlineExpression()
    {
        var result = Translate(x => x.Salary * 1.1m == 50000m);
        result.Sql.Should().Contain("[Salary] *");
    }

    // ── Math.Abs ──────────────────────────────────────────────────────────────

    [Fact]
    public void MathAbs_GeneratesAbsFunction()
    {
        var result = Translate(x => Math.Abs(x.Age) > 0);
        result.Sql.Should().Be("ABS([Age]) > @p0");
    }

    // ── IEnumerable.Contains ──────────────────────────────────────────────────

    [Fact]
    public void EnumerableContains_GeneratesInClause()
    {
        var ids = new List<int> { 1, 2, 3 };
        var result = Translate(x => ids.Contains(x.Id));
        result.Sql.Should().Be("[Id] IN (@p0, @p1, @p2)");
        result.Parameters["p0"].Should().Be(1);
        result.Parameters["p1"].Should().Be(2);
        result.Parameters["p2"].Should().Be(3);
    }

    [Fact]
    public void EmptyCollection_GeneratesFalseCondition()
    {
        var ids = new List<int>();
        var result = Translate(x => ids.Contains(x.Id));
        result.Sql.Should().Be("1=0");
    }

    // ── DateTime parts ────────────────────────────────────────────────────────

    [Fact]
    public void DateTimePart_Year_SqlServer()
    {
        var result = Translate(x => x.CreatedAt.Year > 2020);
        result.Sql.Should().Be("YEAR([CreatedAt]) > @p0");
    }

    [Fact]
    public void DateTimePart_Month_PostgreSql()
    {
        var result = Translate(x => x.CreatedAt.Month == 12, new PostgreSqlDialect());
        result.Sql.Should().Be("EXTRACT(MONTH FROM \"CreatedAt\") = @p0");
    }

    [Fact]
    public void DateTimePart_Day_Sqlite()
    {
        var result = Translate(x => x.CreatedAt.Day == 1, new SqliteDialect());
        result.Sql.Should().Be("strftime('%d', \"CreatedAt\") = @p0");
    }

    // ── Captured variables ────────────────────────────────────────────────────

    [Fact]
    public void CapturedVariable_TreatedAsConstant()
    {
        int minAge = 18;
        var result = Translate(x => x.Age >= minAge);
        result.Sql.Should().Be("[Age] >= @p0");
        result.Parameters["p0"].Should().Be(18);
    }

    // ── PostgreSQL dialect ────────────────────────────────────────────────────

    [Fact]
    public void PostgreSql_UsesDoubleQuotes()
    {
        var result = Translate(x => x.Id == 1, new PostgreSqlDialect());
        result.Sql.Should().Be("\"Id\" = @p0");
    }

    [Fact]
    public void PostgreSql_BoolTrueIsLiteralTrue()
    {
        var result = Translate(x => x.IsActive, new PostgreSqlDialect());
        result.Sql.Should().Be("\"IsActive\" = TRUE");
    }

    // ── MySQL dialect ─────────────────────────────────────────────────────────

    [Fact]
    public void MySql_UsesBacktickQuotes()
    {
        var result = Translate(x => x.Age > 18, new MySqlDialect());
        result.Sql.Should().Be("`Age` > @p0");
    }
}
