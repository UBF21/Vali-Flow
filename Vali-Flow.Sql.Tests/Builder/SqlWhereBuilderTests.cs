using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests.Builder;

/// <summary>Tests for <see cref="SqlWhereBuilder{T}"/>.</summary>
public sealed class SqlWhereBuilderTests
{
    private static SqlWhereBuilder<TestUser> Where() => new();
    private static readonly ISqlDialect Sql = new SqlServerDialect();
    private static readonly ISqlDialect Pg = new PostgreSqlDialect();
    private static readonly ISqlDialect My = new MySqlDialect();

    private static (string Sql, IReadOnlyDictionary<string, object> Params) Build(SqlWhereBuilder<TestUser> b)
        => b.Build(Sql);

    // ── Comparison operators ──────────────────────────────────────────────────

    [Fact]
    public void EqualTo_ProducesEqualsCondition()
    {
        var (sql, p) = Build(Where().EqualTo(x => x.Name, "alice"));
        sql.Should().Be("[Name] = @pw0");
        p["pw0"].Should().Be("alice");
    }

    [Fact]
    public void NotEqualTo_ProducesNotEqualsCondition()
    {
        var (sql, p) = Build(Where().NotEqualTo(x => x.Name, "alice"));
        sql.Should().Be("[Name] != @pw0");
        p["pw0"].Should().Be("alice");
    }

    [Fact]
    public void GreaterThan_ProducesGtCondition()
    {
        var (sql, p) = Build(Where().GreaterThan(x => x.Salary, 5000m));
        sql.Should().Be("[Salary] > @pw0");
        p["pw0"].Should().Be(5000m);
    }

    [Fact]
    public void GreaterThanOrEqualTo_ProducesGteCondition()
    {
        var (sql, p) = Build(Where().GreaterThanOrEqualTo(x => x.Salary, 5000m));
        sql.Should().Be("[Salary] >= @pw0");
        p["pw0"].Should().Be(5000m);
    }

    [Fact]
    public void LessThan_ProducesLtCondition()
    {
        var (sql, p) = Build(Where().LessThan(x => x.Salary, 5000m));
        sql.Should().Be("[Salary] < @pw0");
        p["pw0"].Should().Be(5000m);
    }

    [Fact]
    public void LessThanOrEqualTo_ProducesLteCondition()
    {
        var (sql, p) = Build(Where().LessThanOrEqualTo(x => x.Salary, 5000m));
        sql.Should().Be("[Salary] <= @pw0");
        p["pw0"].Should().Be(5000m);
    }

    // ── Null checks ───────────────────────────────────────────────────────────

    [Fact]
    public void IsNull_ProducesIsNullCondition()
    {
        var (sql, p) = Build(Where().IsNull(x => x.Name));
        sql.Should().Be("[Name] IS NULL");
        p.Should().BeEmpty();
    }

    [Fact]
    public void IsNotNull_ProducesIsNotNullCondition()
    {
        var (sql, p) = Build(Where().IsNotNull(x => x.Name));
        sql.Should().Be("[Name] IS NOT NULL");
        p.Should().BeEmpty();
    }

    // ── LIKE ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Contains_ProducesLikeWithWildcards()
    {
        var (sql, p) = Build(Where().Contains(x => x.Name, "alice"));
        sql.Should().Be("[Name] LIKE @pw0");
        p["pw0"].Should().Be("%alice%");
    }

    [Fact]
    public void NotContains_ProducesNotLikeWithWildcards()
    {
        var (sql, p) = Build(Where().NotContains(x => x.Name, "alice"));
        sql.Should().Be("[Name] NOT LIKE @pw0");
        p["pw0"].Should().Be("%alice%");
    }

    [Fact]
    public void StartsWith_ProducesLikeWithTrailingWildcard()
    {
        var (sql, p) = Build(Where().StartsWith(x => x.Name, "alice"));
        sql.Should().Be("[Name] LIKE @pw0");
        p["pw0"].Should().Be("alice%");
    }

    [Fact]
    public void EndsWith_ProducesLikeWithLeadingWildcard()
    {
        var (sql, p) = Build(Where().EndsWith(x => x.Name, "alice"));
        sql.Should().Be("[Name] LIKE @pw0");
        p["pw0"].Should().Be("%alice");
    }

    // ── BETWEEN ───────────────────────────────────────────────────────────────

    [Fact]
    public void Between_ProducesBetweenCondition()
    {
        var (sql, p) = Build(Where().Between(x => x.Salary, 1000m, 5000m));
        sql.Should().Be("[Salary] BETWEEN @pw0 AND @pw1");
        p["pw0"].Should().Be(1000m);
        p["pw1"].Should().Be(5000m);
    }

    [Fact]
    public void NotBetween_ProducesNotBetweenCondition()
    {
        var (sql, p) = Build(Where().NotBetween(x => x.Salary, 1000m, 5000m));
        sql.Should().Be("[Salary] NOT BETWEEN @pw0 AND @pw1");
        p["pw0"].Should().Be(1000m);
        p["pw1"].Should().Be(5000m);
    }

    // ── IN ────────────────────────────────────────────────────────────────────

    [Fact]
    public void In_WithValues_ProducesInCondition()
    {
        var (sql, p) = Build(Where().In(x => x.Id, new[] { 1, 2, 3 }));
        sql.Should().Be("[Id] IN (@pw0, @pw1, @pw2)");
        p["pw0"].Should().Be(1);
        p["pw1"].Should().Be(2);
        p["pw2"].Should().Be(3);
    }

    [Fact]
    public void In_Empty_ProducesAlwaysFalse()
    {
        var (sql, p) = Build(Where().In(x => x.Id, Array.Empty<int>()));
        sql.Should().Be("1=0");
        p.Should().BeEmpty();
    }

    [Fact]
    public void NotIn_WithValues_ProducesNotInCondition()
    {
        var (sql, p) = Build(Where().NotIn(x => x.Id, new[] { 10, 20 }));
        sql.Should().Be("[Id] NOT IN (@pw0, @pw1)");
        p["pw0"].Should().Be(10);
        p["pw1"].Should().Be(20);
    }

    [Fact]
    public void NotIn_Empty_ProducesAlwaysTrue()
    {
        var (sql, p) = Build(Where().NotIn(x => x.Id, Array.Empty<int>()));
        sql.Should().Be("1=1");
        p.Should().BeEmpty();
    }

    // ── AND/OR grouping ───────────────────────────────────────────────────────

    [Fact]
    public void SingleCondition_NoExtraParens()
    {
        var (sql, _) = Build(Where().EqualTo(x => x.Name, "alice"));
        sql.Should().Be("[Name] = @pw0");
    }

    [Fact]
    public void TwoConditions_And_SingleGroup_NoParens()
    {
        var (sql, _) = Build(Where()
            .EqualTo(x => x.Name, "alice")
            .EqualTo(x => x.IsActive, true));
        sql.Should().Be("[Name] = @pw0 AND [IsActive] = @pw1");
    }

    [Fact]
    public void TwoConditions_Or_OuterParens()
    {
        var (sql, _) = Build(Where()
            .EqualTo(x => x.Name, "alice")
            .Or()
            .EqualTo(x => x.IsActive, true));
        sql.Should().Be("([Name] = @pw0 OR [IsActive] = @pw1)");
    }

    [Fact]
    public void AAndB_Or_C_CorrectParenthesization()
    {
        // (A AND B) OR C
        var (sql, _) = Build(Where()
            .EqualTo(x => x.Name, "a")
            .EqualTo(x => x.IsActive, true)
            .Or()
            .EqualTo(x => x.Age, 42));
        // groups: [Name=@pw0 AND IsActive=@pw1], [Age=@pw2]
        // multipleGroups → outer parens, first group has 2 items → inner parens
        sql.Should().Be("(([Name] = @pw0 AND [IsActive] = @pw1) OR [Age] = @pw2)");
    }

    [Fact]
    public void A_Or_BAndC_CorrectParenthesization()
    {
        // A OR (B AND C) — use SubGroup for the right-hand side
        var (sql, _) = Build(Where()
            .EqualTo(x => x.Name, "a")
            .Or()
            .AddSubGroup(g => g
                .EqualTo(x => x.IsActive, true)
                .EqualTo(x => x.Age, 42)));
        sql.Should().Be("([Name] = @pw0 OR ([IsActive] = @pw1 AND [Age] = @pw2))");
    }

    // ── SubGroup ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddSubGroup_InnerAnd_SingleOuterParens()
    {
        // builder.EqualTo(x => x.Id, 1).Or().AddSubGroup(g => g.EqualTo(x => x.Name, "a").EqualTo(x => x.IsActive, true))
        // Expected: ([Id] = @pw0 OR ([Name] = @pw1 AND [IsActive] = @pw2))
        var (sql, p) = Build(Where()
            .EqualTo(x => x.Id, 1)
            .Or()
            .AddSubGroup(g => g
                .EqualTo(x => x.Name, "a")
                .EqualTo(x => x.IsActive, true)));

        sql.Should().Be("([Id] = @pw0 OR ([Name] = @pw1 AND [IsActive] = @pw2))");
        p["pw0"].Should().Be(1);
        p["pw1"].Should().Be("a");
        p["pw2"].Should().Be(true);
    }

    // ── Empty builder ─────────────────────────────────────────────────────────

    [Fact]
    public void EmptyBuilder_ReturnsEmptyString()
    {
        var (sql, _) = Build(Where());
        sql.Should().Be(string.Empty);
    }

    // ── Parameter names ───────────────────────────────────────────────────────

    [Fact]
    public void Parameters_UsePwPrefix_NoAtSign()
    {
        var (_, p) = Build(Where().EqualTo(x => x.Name, "x").EqualTo(x => x.Age, 5));
        p.Keys.Should().Contain("pw0");
        p.Keys.Should().Contain("pw1");
        p.Keys.Should().NotContain("@pw0");
    }

    // ── Multiple dialects ─────────────────────────────────────────────────────

    [Fact]
    public void PostgreSql_UsesDoubleQuoteIdentifiers()
    {
        var (sql, _) = Where().EqualTo(x => x.Name, "bob").Build(Pg);
        sql.Should().Be("\"Name\" = @pw0");
    }

    [Fact]
    public void MySql_UsesBacktickIdentifiers()
    {
        var (sql, _) = Where().EqualTo(x => x.Name, "bob").Build(My);
        sql.Should().Be("`Name` = @pw0");
    }

    // ── Case-insensitive LIKE ─────────────────────────────────────────────────

    [Fact]
    public void ContainsIgnoreCase_SqlServer_UsesLower()
    {
        var (sql, p) = Where().ContainsIgnoreCase(x => x.Name, "alice").Build(Sql);
        sql.Should().Be("LOWER([Name]) LIKE LOWER(@pw0)");
        p["pw0"].Should().Be("%alice%");
    }

    [Fact]
    public void ContainsIgnoreCase_PostgreSQL_UsesILike()
    {
        var (sql, p) = Where().ContainsIgnoreCase(x => x.Name, "alice").Build(Pg);
        sql.Should().Be("\"Name\" ILIKE @pw0");
        p["pw0"].Should().Be("%alice%");
    }

    [Fact]
    public void StartsWithIgnoreCase_GeneratesLikeWithTrailingWildcard()
    {
        var (sql, p) = Where().StartsWithIgnoreCase(x => x.Name, "alice").Build(Sql);
        sql.Should().Be("LOWER([Name]) LIKE LOWER(@pw0)");
        p["pw0"].Should().Be("alice%");
    }

    [Fact]
    public void EndsWithIgnoreCase_GeneratesLikeWithLeadingWildcard()
    {
        var (sql, p) = Where().EndsWithIgnoreCase(x => x.Name, "alice").Build(Sql);
        sql.Should().Be("LOWER([Name]) LIKE LOWER(@pw0)");
        p["pw0"].Should().Be("%alice");
    }

    // ── Date part filtering ───────────────────────────────────────────────────

    [Fact]
    public void DatePartEquals_Year_GeneratesYearExpression()
    {
        var (sql, p) = Where().DatePartEquals(x => x.CreatedAt, "YEAR", 2024).Build(Sql);
        sql.Should().Be("YEAR([CreatedAt]) = @pw0");
        p["pw0"].Should().Be(2024);
    }

    [Fact]
    public void DatePartGreaterThan_Month_GeneratesMonthExpression()
    {
        var (sql, p) = Where().DatePartGreaterThan(x => x.CreatedAt, "MONTH", 6).Build(Sql);
        sql.Should().Be("MONTH([CreatedAt]) > @pw0");
        p["pw0"].Should().Be(6);
    }

    [Fact]
    public void DatePartBetween_Year_GeneratesBetweenCondition()
    {
        var (sql, p) = Where().DatePartBetween(x => x.CreatedAt, "YEAR", 2020, 2024).Build(Sql);
        sql.Should().Be("YEAR([CreatedAt]) BETWEEN @pw0 AND @pw1");
        p["pw0"].Should().Be(2020);
        p["pw1"].Should().Be(2024);
    }

    [Fact]
    public void DatePart_SqlServer_UsesYEARFunction()
    {
        var (sql, _) = Where().DatePartEquals(x => x.CreatedAt, "YEAR", 2023).Build(Sql);
        sql.Should().StartWith("YEAR(");
    }

    [Fact]
    public void DatePart_SQLite_UsesStrftime()
    {
        var sqlite = new SqliteDialect();
        var (sql, _) = Where().DatePartEquals(x => x.CreatedAt, "YEAR", 2023).Build(sqlite);
        sql.Should().StartWith("strftime('%Y',");
    }

    // ── CastCompare ───────────────────────────────────────────────────────────

    [Fact]
    public void CastEqualTo_SqlServer_GeneratesCastCondition()
    {
        var (sql, p) = Where().CastEqualTo(x => x.Age, "INT", 42).Build(Sql);
        sql.Should().Be("CAST([Age] AS INT) = @pw0");
        p["pw0"].Should().Be(42);
    }

    [Fact]
    public void CastGreaterThan_PostgreSQL_UsesDoubleQuotes()
    {
        var (sql, p) = Where().CastGreaterThan(x => x.Salary, "BIGINT", 1000L).Build(Pg);
        sql.Should().Be("CAST(\"Salary\" AS BIGINT) > @pw0");
        p["pw0"].Should().Be(1000L);
    }

    [Fact]
    public void CastLessThan_GeneratesCorrectOperator()
    {
        var (sql, p) = Where().CastLessThan(x => x.Age, "SMALLINT", 100).Build(Sql);
        sql.Should().Be("CAST([Age] AS SMALLINT) < @pw0");
        p["pw0"].Should().Be(100);
    }

    // ── IsDistinctFrom ────────────────────────────────────────────────────────

    [Fact]
    public void IsDistinctFrom_PostgreSQL_UsesNativeSyntax()
    {
        var (sql, p) = Where().IsDistinctFrom(x => x.Name, "alice").Build(Pg);
        sql.Should().Be("\"Name\" IS DISTINCT FROM @pw0");
        p["pw0"].Should().Be("alice");
    }

    [Fact]
    public void IsDistinctFrom_SqlServer_UsesFallback()
    {
        var (sql, p) = Where().IsDistinctFrom(x => x.Name, "alice").Build(Sql);
        sql.Should().Be("([Name] <> @pw0 OR [Name] IS NULL)");
        p["pw0"].Should().Be("alice");
    }

    [Fact]
    public void IsNotDistinctFrom_PostgreSQL_UsesNativeSyntax()
    {
        var (sql, p) = Where().IsNotDistinctFrom(x => x.Name, "alice").Build(Pg);
        sql.Should().Be("\"Name\" IS NOT DISTINCT FROM @pw0");
        p["pw0"].Should().Be("alice");
    }

    [Fact]
    public void IsNotDistinctFrom_SqlServer_UsesFallback()
    {
        var (sql, p) = Where().IsNotDistinctFrom(x => x.Name, "alice").Build(Sql);
        sql.Should().Be("([Name] = @pw0 OR ([Name] IS NULL AND @pw0 IS NULL))");
        p["pw0"].Should().Be("alice");
    }

    [Fact]
    public void IsDistinctFrom_ParameterPrefix_IsCorrect()
    {
        var (sql, _) = Where().IsDistinctFrom(x => x.Age, 10).Build(Sql);
        sql.Should().Contain("@pw0");
    }

    // ── LIKE escaping ─────────────────────────────────────────────────────────

    [Fact]
    public void Contains_WithPercentSign_EscapesPercent()
    {
        var (sql, p) = Build(Where().Contains(x => x.Name, "100%"));
        sql.Should().Be("[Name] LIKE @pw0");
        p["pw0"].Should().Be("%100\\%%");
    }

    [Fact]
    public void Contains_WithUnderscore_EscapesUnderscore()
    {
        var (sql, p) = Build(Where().Contains(x => x.Name, "some_value"));
        sql.Should().Be("[Name] LIKE @pw0");
        p["pw0"].Should().Be("%some\\_value%");
    }

    [Fact]
    public void StartsWith_WithPercentSign_EscapesPercent()
    {
        var (sql, p) = Build(Where().StartsWith(x => x.Name, "50%"));
        sql.Should().Be("[Name] LIKE @pw0");
        p["pw0"].Should().Be("50\\%%");
    }

    [Fact]
    public void EndsWith_WithPercentSign_EscapesPercent()
    {
        var (sql, p) = Build(Where().EndsWith(x => x.Name, "50%"));
        sql.Should().Be("[Name] LIKE @pw0");
        p["pw0"].Should().Be("%50\\%");
    }

    [Fact]
    public void NotContains_WithSpecialChars_EscapesCorrectly()
    {
        var (sql, p) = Build(Where().NotContains(x => x.Name, "a%b_c"));
        sql.Should().Be("[Name] NOT LIKE @pw0");
        p["pw0"].Should().Be("%a\\%b\\_c%");
    }

    [Fact]
    public void ContainsIgnoreCase_WithPercentSign_EscapesPercent()
    {
        var (sql, p) = Where().ContainsIgnoreCase(x => x.Name, "100%").Build(Sql);
        sql.Should().Be("LOWER([Name]) LIKE LOWER(@pw0)");
        p["pw0"].Should().Be("%100\\%%");
    }

    // ── CoalesceEqualTo ───────────────────────────────────────────────────────

    [Fact]
    public void CoalesceEqualTo_SqlServer_GeneratesCoalesceCondition()
    {
        var (sql, p) = Where().CoalesceEqualTo(x => x.Department, "'N/A'", "N/A").Build(Sql);
        sql.Should().Be("COALESCE([Department], 'N/A') = @pw0");
        p["pw0"].Should().Be("N/A");
    }

    [Fact]
    public void CoalesceEqualTo_PostgreSQL_UsesDoubleQuotes()
    {
        var (sql, p) = Where().CoalesceEqualTo(x => x.Department, "'N/A'", "N/A").Build(Pg);
        sql.Should().Be("COALESCE(\"Department\", 'N/A') = @pw0");
        p["pw0"].Should().Be("N/A");
    }

    [Fact]
    public void CoalesceEqualTo_ParameterHasCorrectValue()
    {
        var (_, p) = Where().CoalesceEqualTo(x => x.Department, "'Unknown'", "Unknown").Build(Sql);
        p["pw0"].Should().Be("Unknown");
    }
}
