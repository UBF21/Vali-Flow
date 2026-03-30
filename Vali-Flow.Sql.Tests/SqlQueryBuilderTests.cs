using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for <see cref="SqlQueryBuilder{T}"/>.</summary>
public sealed class SqlQueryBuilderTests
{
    private static SqlQueryBuilder<TestUser> Sql(ISqlDialect? dialect = null)
        => new(dialect ?? new SqlServerDialect());

    private static SqlQueryBuilder<TestUser> PgSql()
        => new(new PostgreSqlDialect());

    // ── Basic SELECT ──────────────────────────────────────────────────────────

    [Fact]
    public void Build_SelectStar_WhenNoColumnsSpecified()
    {
        var result = Sql().From("Users").Build();
        result.Sql.Should().Be("SELECT * FROM [Users]");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Build_SelectSpecificColumns()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id, x => x.Name, x => x.Email)
            .Build();

        result.Sql.Should().Be("SELECT [Id], [Name], [Email] FROM [Users]");
    }

    [Fact]
    public void Build_SelectAll_ResetsToStar()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectAll()
            .Build();

        result.Sql.Should().Be("SELECT * FROM [Users]");
    }

    [Fact]
    public void Build_SelectDistinct()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Department)
            .Distinct()
            .Build();

        result.Sql.Should().Be("SELECT DISTINCT [Department] FROM [Users]");
    }

    // ── Column aliases ────────────────────────────────────────────────────────

    [Fact]
    public void Build_SelectWithAlias()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Name, "UserName")
            .Build();

        result.Sql.Should().Be("SELECT [Name] AS [UserName] FROM [Users]");
    }

    [Fact]
    public void Build_SelectWithNullAlias_OmitsAlias()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Name, alias: null)
            .Build();

        result.Sql.Should().Be("SELECT [Name] FROM [Users]");
    }

    // ── SelectRaw ─────────────────────────────────────────────────────────────

    [Fact]
    public void Build_SelectRaw_AppendsAsIs()
    {
        var result = Sql()
            .From("Users")
            .SelectRaw("GETDATE() AS [Now]")
            .Build();

        result.Sql.Should().Be("SELECT GETDATE() AS [Now] FROM [Users]");
    }

    // ── Aggregates ────────────────────────────────────────────────────────────

    [Fact]
    public void Build_SelectCount_WithoutAlias()
    {
        var result = Sql().From("Users").SelectCount().Build();
        result.Sql.Should().Be("SELECT COUNT(*) FROM [Users]");
    }

    [Fact]
    public void Build_SelectCount_WithAlias()
    {
        var result = Sql().From("Users").SelectCount("total").Build();
        result.Sql.Should().Be("SELECT COUNT(*) AS [total] FROM [Users]");
    }

    [Fact]
    public void Build_SelectSum()
    {
        var result = Sql().From("Users").SelectSum(x => x.Salary, "totalSalary").Build();
        result.Sql.Should().Be("SELECT SUM([Salary]) AS [totalSalary] FROM [Users]");
    }

    [Fact]
    public void Build_SelectAvg()
    {
        var result = Sql().From("Users").SelectAvg(x => x.Age, "avgAge").Build();
        result.Sql.Should().Be("SELECT AVG([Age]) AS [avgAge] FROM [Users]");
    }

    [Fact]
    public void Build_SelectMin()
    {
        var result = Sql().From("Users").SelectMin(x => x.Age).Build();
        result.Sql.Should().Be("SELECT MIN([Age]) FROM [Users]");
    }

    [Fact]
    public void Build_SelectMax()
    {
        var result = Sql().From("Users").SelectMax(x => x.Salary, "maxSalary").Build();
        result.Sql.Should().Be("SELECT MAX([Salary]) AS [maxSalary] FROM [Users]");
    }

    // ── From / Schema ─────────────────────────────────────────────────────────

    [Fact]
    public void Build_TableNameDefaultsToTypeName()
    {
        var result = Sql().Build();
        result.Sql.Should().Be("SELECT * FROM [TestUser]");
    }

    [Fact]
    public void Build_FromWithSchema_SqlServer()
    {
        var result = Sql()
            .From("Users", schema: "dbo")
            .Build();

        result.Sql.Should().Be("SELECT * FROM [dbo].[Users]");
    }

    [Fact]
    public void Build_FromWithSchema_PostgreSql()
    {
        var result = PgSql()
            .From("users", schema: "public")
            .Build();

        result.Sql.Should().Be("SELECT * FROM \"public\".\"users\"");
    }

    // ── WHERE ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhereFromLambda()
    {
        var result = Sql()
            .From("Users")
            .Where((TestUser x) => x.Age > 18)
            .Build();

        result.Sql.Should().Be("SELECT * FROM [Users] WHERE [Age] > @p0");
        result.Parameters["p0"].Should().Be(18);
    }

    [Fact]
    public void Build_WhereRaw()
    {
        var result = Sql()
            .From("Users")
            .WhereRaw("[IsDeleted] = 0")
            .Build();

        result.Sql.Should().Be("SELECT * FROM [Users] WHERE [IsDeleted] = 0");
    }

    [Fact]
    public void Build_WhereTypedAndRaw_CombinedWithAnd()
    {
        var result = Sql()
            .From("Users")
            .Where((TestUser x) => x.IsActive)
            .WhereRaw("[IsDeleted] = 0")
            .Build();

        result.Sql.Should().Be("SELECT * FROM [Users] WHERE ([IsActive] = 1) AND ([IsDeleted] = 0)");
    }

    // ── EXISTS ────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhereExists_InlinedCorrectly()
    {
        var subquery = Sql()
            .From("Orders")
            .Select(x => x.Id)
            .Where((TestUser x) => x.Age > 0)
            .Build();

        var result = Sql()
            .From("Users")
            .WhereExists(subquery)
            .Build();

        result.Sql.Should().Contain("EXISTS (SELECT [Id] FROM [Orders] WHERE [Age] > @p0)");
    }

    [Fact]
    public void Build_WhereNotExists()
    {
        var subquery = Sql().From("Bans").Build();

        var result = Sql()
            .From("Users")
            .WhereNotExists(subquery)
            .Build();

        result.Sql.Should().Contain("NOT EXISTS (SELECT * FROM [Bans])");
    }

    [Fact]
    public void Build_WhereExists_SubqueryParamsRemappedToAvoidCollisions()
    {
        // Main query uses p0; subquery also uses p0 — they should be remapped
        var subquery = Sql()
            .From("Orders")
            .Where((TestUser x) => x.Age > 0)
            .Build();

        var result = Sql()
            .From("Users")
            .Where((TestUser x) => x.Id == 1)
            .WhereExists(subquery)
            .Build();

        // The subquery p0 should be renamed (main query already has p0)
        result.Parameters.Count.Should().Be(2);
        result.Parameters.ContainsKey("p0").Should().BeTrue();
        result.Parameters.ContainsKey("p1").Should().BeTrue();
    }

    // ── JOINs ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_InnerJoin()
    {
        var result = Sql()
            .From("Users")
            .InnerJoin("Orders", "o", "[Users].[Id] = o.[UserId]")
            .Build();

        result.Sql.Should().Be("SELECT * FROM [Users] INNER JOIN [Orders] o ON [Users].[Id] = o.[UserId]");
    }

    [Fact]
    public void Build_LeftJoin()
    {
        var result = Sql()
            .From("Users")
            .LeftJoin("Orders", null, "[Users].[Id] = [Orders].[UserId]")
            .Build();

        result.Sql.Should().Be("SELECT * FROM [Users] LEFT JOIN [Orders] ON [Users].[Id] = [Orders].[UserId]");
    }

    [Fact]
    public void Build_RightJoin()
    {
        var result = Sql()
            .From("Users")
            .RightJoin("Departments", "d", "[Users].[DeptId] = d.[Id]")
            .Build();

        result.Sql.Should().Contain("RIGHT JOIN [Departments] d ON [Users].[DeptId] = d.[Id]");
    }

    [Fact]
    public void Build_FullOuterJoin()
    {
        var result = Sql()
            .From("Users")
            .FullOuterJoin("Profiles", null, "[Users].[Id] = [Profiles].[UserId]")
            .Build();

        result.Sql.Should().Contain("FULL OUTER JOIN");
    }

    [Fact]
    public void Build_CrossJoin()
    {
        var result = Sql()
            .From("Users")
            .CrossJoin("Roles")
            .Build();

        result.Sql.Should().Be("SELECT * FROM [Users] CROSS JOIN [Roles]");
    }

    [Fact]
    public void Build_MultipleJoins()
    {
        var result = Sql()
            .From("Users")
            .InnerJoin("Orders", "o", "[Users].[Id] = o.[UserId]")
            .LeftJoin("Products", "p", "o.[ProductId] = p.[Id]")
            .Build();

        result.Sql.Should().Contain("INNER JOIN [Orders] o ON [Users].[Id] = o.[UserId]");
        result.Sql.Should().Contain("LEFT JOIN [Products] p ON o.[ProductId] = p.[Id]");
    }

    // ── GROUP BY / HAVING ─────────────────────────────────────────────────────

    [Fact]
    public void Build_GroupBy()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Department)
            .SelectCount("count")
            .GroupBy(x => x.Department)
            .Build();

        result.Sql.Should().Be("SELECT [Department], COUNT(*) AS [count] FROM [Users] GROUP BY [Department]");
    }

    [Fact]
    public void Build_GroupByMultipleColumns()
    {
        var result = Sql()
            .From("Users")
            .GroupBy(x => x.Department, x => x.Status)
            .Build();

        result.Sql.Should().Contain("GROUP BY [Department], [Status]");
    }

    [Fact]
    public void Build_Having()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Department)
            .SelectCount("cnt")
            .GroupBy(x => x.Department)
            .Having("COUNT(*) > 5")
            .Build();

        result.Sql.Should().Contain("HAVING COUNT(*) > 5");
    }

    // ── ORDER BY ──────────────────────────────────────────────────────────────

    [Fact]
    public void Build_OrderByAscending()
    {
        var result = Sql()
            .From("Users")
            .OrderBy(x => x.Name)
            .Build();

        result.Sql.Should().Contain("ORDER BY [Name] ASC");
    }

    [Fact]
    public void Build_OrderByDescending()
    {
        var result = Sql()
            .From("Users")
            .OrderBy(x => x.CreatedAt, ascending: false)
            .Build();

        result.Sql.Should().Contain("ORDER BY [CreatedAt] DESC");
    }

    [Fact]
    public void Build_ThenBy()
    {
        var result = Sql()
            .From("Users")
            .OrderBy(x => x.Department)
            .ThenBy(x => x.Name, ascending: false)
            .Build();

        result.Sql.Should().Contain("ORDER BY [Department] ASC, [Name] DESC");
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    [Fact]
    public void Build_Take_SqlServer_UsesTop()
    {
        var result = Sql()
            .From("Users")
            .Take(10)
            .Build();

        result.Sql.Should().Be("SELECT TOP 10 * FROM [Users]");
    }

    [Fact]
    public void Build_Take_PostgreSql_UsesLimit()
    {
        var result = PgSql()
            .From("users")
            .Take(10)
            .Build();

        result.Sql.Should().Be("SELECT * FROM \"users\" LIMIT 10");
    }

    [Fact]
    public void Build_SkipAndTake_SqlServer_UsesOffsetFetch()
    {
        var result = Sql()
            .From("Users")
            .OrderBy(x => x.Id)
            .Skip(10)
            .Take(5)
            .Build();

        result.Sql.Should().Contain("OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY");
        result.Sql.Should().NotContain("TOP");
    }

    [Fact]
    public void Build_Page_CalculatesCorrectSkipAndTake()
    {
        var result = PgSql()
            .From("users")
            .Page(3, 20)
            .Build();

        // Page 3, size 20 → skip 40, take 20
        result.Sql.Should().Contain("LIMIT 20 OFFSET 40");
    }

    [Fact]
    public void Build_Page1_NoOffset()
    {
        var result = PgSql()
            .From("users")
            .Page(1, 10)
            .Build();

        result.Sql.Should().Be("SELECT * FROM \"users\" LIMIT 10");
    }

    [Fact]
    public void Build_Skip_WithoutOrder_SqlServer_ThrowsInvalidOperation()
    {
        var act = () => Sql()
            .From("Users")
            .Skip(10)
            .Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ORDER BY*");
    }

    // ── Table hints ───────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithHint_AppendsAfterTableName()
    {
        var result = Sql()
            .From("Users")
            .WithHint("NOLOCK")
            .Build();

        result.Sql.Should().Be("SELECT * FROM [Users] WITH (NOLOCK)");
    }

    [Fact]
    public void Build_WithHint_After_JoinsOrder_TableHintBeforeJoins()
    {
        var result = Sql()
            .From("Users")
            .WithHint("NOLOCK")
            .InnerJoin("Orders", "o", "[Users].[Id] = o.[UserId]")
            .Build();

        var tablePart = result.Sql.IndexOf("[Users]");
        var hintPart = result.Sql.IndexOf("WITH (NOLOCK)");
        var joinPart = result.Sql.IndexOf("INNER JOIN");

        hintPart.Should().BeGreaterThan(tablePart);
        joinPart.Should().BeGreaterThan(hintPart);
    }

    // ── CASE WHEN ─────────────────────────────────────────────────────────────

    [Fact]
    public void Build_SelectCase_GeneratesCaseExpression()
    {
        var caseExpr = CaseWhenBuilder
            .Create("[Status] = 1", "Active")
            .When("[Status] = 2", "Inactive")
            .Else("Unknown")
            .As("StatusLabel");

        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectCase(caseExpr)
            .Build();

        result.Sql.Should().Contain("CASE WHEN [Status] = 1 THEN 'Active' WHEN [Status] = 2 THEN 'Inactive' ELSE 'Unknown' END AS [StatusLabel]");
    }

    [Fact]
    public void Build_SelectCase_NoAlias()
    {
        var caseExpr = CaseWhenBuilder
            .Create("[IsActive] = 1", "Yes")
            .Else("No");

        var result = Sql()
            .From("Users")
            .SelectCase(caseExpr)
            .Build();

        result.Sql.Should().Contain("CASE WHEN [IsActive] = 1 THEN 'Yes' ELSE 'No' END");
        result.Sql.Should().NotContain(" AS ");
    }

    [Fact]
    public void CaseWhenBuilder_NumericValues_NotQuoted()
    {
        var expr = CaseWhenBuilder
            .Create("[Age] >= 18", 1)
            .Else(0)
            .Build();

        expr.Should().Be("CASE WHEN [Age] >= 18 THEN 1 ELSE 0 END");
    }

    // ── UNION ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_Union_AppendedAfterMainQuery()
    {
        var unionQuery = Sql().From("ArchivedUsers").Select(x => x.Id, x => x.Name).Build();

        var result = Sql()
            .From("Users")
            .Select(x => x.Id, x => x.Name)
            .Union(unionQuery)
            .Build();

        result.Sql.Should().Contain(" UNION ");
        result.Sql.Should().Contain("[ArchivedUsers]");
    }

    [Fact]
    public void Build_UnionAll_AppendedAfterMainQuery()
    {
        var unionQuery = Sql().From("ArchivedUsers").Build();

        var result = Sql()
            .From("Users")
            .UnionAll(unionQuery)
            .Build();

        result.Sql.Should().Contain(" UNION ALL ");
    }

    [Fact]
    public void Build_Union_ParametersRemapped_NoCollisions()
    {
        // Both main and union query have parameters
        var unionQuery = Sql()
            .From("ArchivedUsers")
            .Where((TestUser x) => x.Age > 0)
            .Build();

        var result = Sql()
            .From("Users")
            .Where((TestUser x) => x.Id == 1)
            .Union(unionQuery)
            .Build();

        // Each p0 should be uniquely remapped
        result.Parameters.Count.Should().Be(2);
        result.Parameters.Values.Should().Contain(1); // Id == 1
        result.Parameters.Values.Should().Contain(0); // Age > 0
    }

    // ── PostgreSQL dialect ────────────────────────────────────────────────────

    [Fact]
    public void Build_PostgreSql_UsesDoubleQuoteIdentifiers()
    {
        var result = PgSql()
            .From("users")
            .Select(x => x.Id, x => x.Name)
            .Where((TestUser x) => x.IsActive)
            .Build();

        result.Sql.Should().Be("SELECT \"Id\", \"Name\" FROM \"users\" WHERE \"IsActive\" = TRUE");
    }

    [Fact]
    public void Build_PostgreSql_LimitOffset()
    {
        var result = PgSql()
            .From("users")
            .OrderBy(x => x.Id)
            .Skip(5)
            .Take(10)
            .Build();

        result.Sql.Should().Contain("LIMIT 10 OFFSET 5");
    }

    // ── Full query test ───────────────────────────────────────────────────────

    [Fact]
    public void Build_FullQuery_SqlServer()
    {
        var result = Sql()
            .From("Users", schema: "dbo")
            .Select(x => x.Id)
            .Select(x => x.Name, "UserName")
            .SelectCount("cnt")
            .InnerJoin("Orders", "o", "[Users].[Id] = o.[UserId]")
            .Where((TestUser x) => x.IsActive)
            .GroupBy(x => x.Department)
            .Having("COUNT(*) > 2")
            .OrderBy(x => x.Department)
            .Page(2, 10)
            .WithHint("NOLOCK")
            .Build();

        result.Sql.Should().StartWith("SELECT");
        result.Sql.Should().Contain("[dbo].[Users]");
        result.Sql.Should().Contain("WITH (NOLOCK)");
        result.Sql.Should().Contain("INNER JOIN");
        result.Sql.Should().Contain("WHERE");
        result.Sql.Should().Contain("GROUP BY");
        result.Sql.Should().Contain("HAVING COUNT(*) > 2");
        result.Sql.Should().Contain("ORDER BY");
        result.Sql.Should().Contain("OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY");
    }

    // ── ToPreviewSql ──────────────────────────────────────────────────────────

    [Fact]
    public void ToPreviewSql_ReturnsSqlStringMidChain()
    {
        var builder = Sql().From("Users").Select(x => x.Id);
        var preview = builder.ToPreviewSql();
        preview.Should().Contain("SELECT [Id] FROM [Users]");
    }

    [Fact]
    public void ToPreviewSql_ReturnsErrorMessage_WhenStateIncomplete()
    {
        // Skip without OrderBy in SQL Server → BuildInternal throws
        var builder = Sql().From("Users").Skip(10);
        var preview = builder.ToPreviewSql();
        preview.Should().Contain("preview not available");
    }

    // ── Argument validation ───────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullDialect_ThrowsArgumentNull()
    {
        var act = () => new SqlQueryBuilder<TestUser>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dialect");
    }

    [Fact]
    public void Take_Zero_ThrowsArgumentOutOfRange()
    {
        var act = () => Sql().Take(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Skip_Negative_ThrowsArgumentOutOfRange()
    {
        var act = () => Sql().Skip(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Page_ZeroPageNumber_ThrowsArgumentOutOfRange()
    {
        var act = () => Sql().Page(0, 10);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Page_ZeroPageSize_ThrowsArgumentOutOfRange()
    {
        var act = () => Sql().Page(1, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
