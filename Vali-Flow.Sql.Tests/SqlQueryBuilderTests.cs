using System.Linq.Expressions;
using FluentAssertions;
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
            .OrderBy(x => x.Id)
            .Take(10)
            .Build();

        result.Sql.Should().Contain("TOP 10").And.Contain("ORDER BY");
    }

    [Fact]
    public void Build_Take_PostgreSql_UsesLimit()
    {
        var result = PgSql()
            .From("users")
            .OrderBy(x => x.Id)
            .Take(10)
            .Build();

        result.Sql.Should().Contain("LIMIT 10").And.Contain("ORDER BY");
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
            .OrderBy(x => x.Id)
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
            .OrderBy(x => x.Id)
            .Page(1, 10)
            .Build();

        result.Sql.Should().Contain("LIMIT 10").And.Contain("ORDER BY");
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

    // ── Inline Where / Having overloads ───────────────────────────────────────

    [Fact]
    public void Where_InlineAction_GeneratesWhereClause()
    {
        var result = Sql()
            .From("Users")
            .Where(w => w.EqualTo(x => x.IsActive, true))
            .Build();

        result.Sql.Should().Contain("WHERE [IsActive] = @pw0");
        result.Parameters["pw0"].Should().Be(true);
    }

    [Fact]
    public void Where_InlineAction_MultipleConditions_AndedCorrectly()
    {
        var result = Sql()
            .From("Users")
            .Where(w => w.EqualTo(x => x.IsActive, true).GreaterThan(x => x.Age, 18))
            .Build();

        result.Sql.Should().Contain("WHERE [IsActive] = @pw0 AND [Age] > @pw1");
    }

    [Fact]
    public void Where_InlineAction_OrCondition_WrapsInParens()
    {
        var result = Sql()
            .From("Users")
            .Where(w => w.EqualTo(x => x.Id, 1).Or().EqualTo(x => x.Id, 2))
            .Build();

        result.Sql.Should().Contain("WHERE ([Id] = @pw0 OR [Id] = @pw1)");
    }

    [Fact]
    public void Where_InlineAction_NullAction_ThrowsArgumentNullException()
    {
        var act = () => Sql().Where((Action<SqlWhereBuilder<TestUser>>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Having_InlineAction_GeneratesHavingClause()
    {
        var result = Sql()
            .From("Orders")
            .GroupBy(x => x.Department)
            .Having(h => h.CountGreaterThan(5))
            .Build();

        result.Sql.Should().Contain("HAVING COUNT(*) > @ph0");
        result.Parameters["ph0"].Should().Be(5);
    }

    [Fact]
    public void Having_InlineAction_MultipleConditions_AndedCorrectly()
    {
        var result = Sql()
            .From("Orders")
            .GroupBy(x => x.Department)
            .Having(h => h.CountGreaterThan(3).And().SumGreaterThan(x => x.Salary, 1000m))
            .Build();

        result.Sql.Should().Contain("HAVING COUNT(*) > @ph0 AND SUM([Salary]) > @ph1");
    }

    [Fact]
    public void Having_InlineAction_NullAction_ThrowsArgumentNullException()
    {
        var act = () => Sql().Having((Action<SqlHavingBuilder<TestUser>>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── CTE (WITH clause) ─────────────────────────────────────────────────────

    [Fact]
    public void WithCte_PrependsCteBeforeSelect()
    {
        var cteQuery = Sql().From("Users").Where(w => w.EqualTo(x => x.IsActive, true)).Build();
        var result = Sql().WithCte("ActiveUsers", cteQuery).From("ActiveUsers").Build();

        result.Sql.Should().StartWith("WITH [ActiveUsers] AS (");
        result.Sql.Should().Contain("SELECT * FROM [ActiveUsers]");
    }

    [Fact]
    public void WithCte_InlineAction_BuildsSubquery()
    {
        var result = Sql()
            .WithCte("TopUsers", sub => sub.From("Users").OrderBy(x => x.Salary, false).Take(10))
            .From("TopUsers")
            .Build();

        result.Sql.Should().StartWith("WITH [TopUsers] AS (");
    }

    [Fact]
    public void WithCte_Multiple_CommaJoined()
    {
        var q1 = Sql().From("A").Build();
        var q2 = Sql().From("B").Build();
        var result = Sql().WithCte("Cte1", q1).WithCte("Cte2", q2).From("Cte1").Build();

        result.Sql.Should().MatchRegex(@"WITH \[Cte1\] AS \(.*\), \[Cte2\] AS \(.*\)");
    }

    [Fact]
    public void WithCte_CteParams_RemappedToAvoidCollisions()
    {
        var cteQuery = Sql().From("Users").Where(w => w.EqualTo(x => x.IsActive, true)).Build();
        var result = Sql()
            .WithCte("A", cteQuery)
            .From("A")
            .Where(w => w.GreaterThan(x => x.Age, 18))
            .Build();

        // All parameter names must be unique
        var keys = result.Parameters.Keys.ToList();
        keys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void WithCte_EmptyName_ThrowsArgumentException()
    {
        var act = () => Sql().WithCte("  ", Sql().From("A").Build());
        act.Should().Throw<ArgumentException>();
    }

    // ── Subquery in FROM ──────────────────────────────────────────────────────

    [Fact]
    public void From_Subquery_GeneratesFromSubquerySql()
    {
        var inner = Sql().From("Users").Where(w => w.EqualTo(x => x.IsActive, true)).Build();
        var result = Sql().From(inner, "sub").Select(x => x.Name).Build();

        result.Sql.Should().MatchRegex(@"FROM \(SELECT \* FROM \[Users\] WHERE .+\) sub");
    }

    [Fact]
    public void From_Subquery_ParamsRemapped()
    {
        var inner = Sql().From("Users").Where(w => w.EqualTo(x => x.IsActive, true)).Build();
        var result = Sql().From(inner, "sub").Where(w => w.GreaterThan(x => x.Age, 18)).Build();

        result.Parameters.Keys.Should().OnlyHaveUniqueItems();
        result.Parameters.Count.Should().Be(2); // one from inner WHERE, one from outer WHERE
    }

    [Fact]
    public void From_Subquery_EmptyAlias_ThrowsArgumentException()
    {
        var inner = Sql().From("Users").Build();
        var act = () => Sql().From(inner, "");
        act.Should().Throw<ArgumentException>();
    }

    // ── WhereInSubquery / WhereNotInSubquery ──────────────────────────────────

    [Fact]
    public void WhereInSubquery_GeneratesInSubquerySql()
    {
        var sub = Sql().From("Orders").Select(x => x.Id).Build();
        var result = Sql().From("Users").WhereInSubquery(x => x.Id, sub).Build();

        result.Sql.Should().Contain("[Id] IN (SELECT [Id] FROM [Orders])");
    }

    [Fact]
    public void WhereNotInSubquery_GeneratesNotInSubquerySql()
    {
        var sub = Sql().From("Banned").Select(x => x.Id).Build();
        var result = Sql().From("Users").WhereNotInSubquery(x => x.Id, sub).Build();

        result.Sql.Should().Contain("[Id] NOT IN (SELECT [Id] FROM [Banned])");
    }

    [Fact]
    public void WhereInSubquery_WithParams_RemapsParams()
    {
        var sub = Sql().From("Orders").Where(w => w.EqualTo(x => x.Status, 1)).Build();
        var result = Sql()
            .From("Users")
            .WhereInSubquery(x => x.Id, sub)
            .Where(w => w.GreaterThan(x => x.Age, 18))
            .Build();

        result.Parameters.Keys.Should().OnlyHaveUniqueItems();
        result.Parameters.Count.Should().Be(2);
    }

    [Fact]
    public void WhereInSubquery_NullColumn_ThrowsArgumentNullException()
    {
        var sub = Sql().From("A").Build();
        var act = () => Sql().WhereInSubquery((Expression<Func<TestUser, int>>)null!, sub);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WhereNotInSubquery_NullSubquery_ThrowsArgumentNullException()
    {
        var act = () => Sql().WhereNotInSubquery(x => x.Id, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── SelectCount(column) / SelectCountDistinct ─────────────────────────────

    [Fact]
    public void SelectCount_Column_GeneratesCountColumn()
    {
        var result = Sql().From("Users").SelectCount(x => x.Id).Build();
        result.Sql.Should().Be("SELECT COUNT([Id]) FROM [Users]");
    }

    [Fact]
    public void SelectCount_Column_WithAlias_GeneratesCountColumnWithAlias()
    {
        var result = Sql().From("Users").SelectCount(x => x.Id, "total").Build();
        result.Sql.Should().Be("SELECT COUNT([Id]) AS [total] FROM [Users]");
    }

    [Fact]
    public void SelectCountDistinct_Column_GeneratesCountDistinctColumn()
    {
        var result = Sql().From("Users").SelectCountDistinct(x => x.Department).Build();
        result.Sql.Should().Be("SELECT COUNT(DISTINCT [Department]) FROM [Users]");
    }

    [Fact]
    public void SelectCountDistinct_Column_WithAlias_GeneratesCountDistinctColumnWithAlias()
    {
        var result = Sql().From("Users").SelectCountDistinct(x => x.Department, "uniqueDepts").Build();
        result.Sql.Should().Be("SELECT COUNT(DISTINCT [Department]) AS [uniqueDepts] FROM [Users]");
    }

    // ── EXCEPT / INTERSECT ────────────────────────────────────────────────────

    [Fact]
    public void Except_AppendsExceptKeyword()
    {
        var other = Sql().From("ArchivedUsers").Select(x => x.Id, x => x.Name).Build();
        var result = Sql()
            .From("Users")
            .Select(x => x.Id, x => x.Name)
            .Except(other)
            .Build();

        result.Sql.Should().Contain(" EXCEPT ");
        result.Sql.Should().Contain("[ArchivedUsers]");
    }

    [Fact]
    public void Intersect_AppendsIntersectKeyword()
    {
        var other = Sql().From("PremiumUsers").Select(x => x.Id, x => x.Name).Build();
        var result = Sql()
            .From("Users")
            .Select(x => x.Id, x => x.Name)
            .Intersect(other)
            .Build();

        result.Sql.Should().Contain(" INTERSECT ");
        result.Sql.Should().Contain("[PremiumUsers]");
    }

    [Fact]
    public void Except_ParametersRemapped_NoCollisions()
    {
        var other = Sql().From("ArchivedUsers").Where((TestUser x) => x.Age > 0).Build();
        var result = Sql()
            .From("Users")
            .Where((TestUser x) => x.Id == 1)
            .Except(other)
            .Build();

        result.Parameters.Count.Should().Be(2);
        result.Parameters.Keys.Should().OnlyHaveUniqueItems();
    }

    // ── Window functions ──────────────────────────────────────────────────────

    [Fact]
    public void SelectWindowRaw_AddsRawWindowExpression()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectWindowRaw("ROW_NUMBER() OVER (ORDER BY [CreatedAt] DESC)", "RowNum")
            .Build();

        result.Sql.Should().Contain("ROW_NUMBER() OVER (ORDER BY [CreatedAt] DESC) AS [RowNum]");
    }

    [Fact]
    public void SelectRowNumber_NoPartition_GeneratesRowNumber()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectRowNumber((Expression<Func<TestUser, object>>?)null, x => x.CreatedAt, ascending: false)
            .Build();

        result.Sql.Should().Contain("ROW_NUMBER() OVER (ORDER BY [CreatedAt] DESC) AS [RowNum]");
    }

    [Fact]
    public void SelectRowNumber_WithPartition_GeneratesPartitionBy()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectRowNumber(x => x.Department, x => x.Salary, ascending: false, alias: "Rn")
            .Build();

        result.Sql.Should().Contain("ROW_NUMBER() OVER (PARTITION BY [Department] ORDER BY [Salary] DESC) AS [Rn]");
    }

    [Fact]
    public void SelectRank_GeneratesRankExpression()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectRank(null, x => x.Salary, ascending: false)
            .Build();

        result.Sql.Should().Contain("RANK() OVER (ORDER BY [Salary] DESC) AS [Rank]");
    }

    [Fact]
    public void SelectDenseRank_GeneratesDenseRankExpression()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectDenseRank(x => x.Department, x => x.Salary, ascending: false)
            .Build();

        result.Sql.Should().Contain("DENSE_RANK() OVER (PARTITION BY [Department] ORDER BY [Salary] DESC) AS [DenseRank]");
    }

    // ── WithRecursive ─────────────────────────────────────────────────────────

    [Fact]
    public void WithRecursive_SqlServer_NoRECURSIVEKeyword()
    {
        var anchor = Sql().From("Employees").Where(w => w.EqualTo(x => x.Id, 1)).Build();
        var recursive = Sql().From("Employees").Build();

        var result = Sql()
            .WithRecursive("cte", anchor, recursive)
            .From("cte")
            .Build();

        result.Sql.Should().StartWith("WITH [cte] AS (");
        result.Sql.Should().NotContain("RECURSIVE");
    }

    [Fact]
    public void WithRecursive_PostgreSQL_HasRECURSIVEKeyword()
    {
        var anchor = PgSql().From("Employees").Where(w => w.EqualTo(x => x.Id, 1)).Build();
        var recursive = PgSql().From("Employees").Build();

        var result = PgSql()
            .WithRecursive("cte", anchor, recursive)
            .From("cte")
            .Build();

        result.Sql.Should().StartWith("WITH RECURSIVE \"cte\" AS (");
    }

    [Fact]
    public void WithRecursive_SQLite_HasRECURSIVEKeyword()
    {
        var sqlite = new SqliteDialect();
        var builder = new SqlQueryBuilder<TestUser>(sqlite);
        var anchorBuilder = new SqlQueryBuilder<TestUser>(sqlite);
        var recursiveBuilder = new SqlQueryBuilder<TestUser>(sqlite);

        var anchor = anchorBuilder.From("Employees").Where(w => w.EqualTo(x => x.Id, 1)).Build();
        var recursive = recursiveBuilder.From("Employees").Build();

        var result = builder
            .WithRecursive("cte", anchor, recursive)
            .From("cte")
            .Build();

        result.Sql.Should().StartWith("WITH RECURSIVE \"cte\" AS (");
    }

    [Fact]
    public void WithRecursive_NullAnchor_ThrowsArgumentNullException()
    {
        var recursive = Sql().From("Employees").Build();
        var act = () => Sql().WithRecursive("cte", null!, recursive);
        act.Should().Throw<ArgumentNullException>().WithParameterName("anchor");
    }

    [Fact]
    public void WithRecursive_NullName_ThrowsArgumentException()
    {
        var anchor = Sql().From("Employees").Build();
        var recursive = Sql().From("Employees").Build();
        var act = () => Sql().WithRecursive(null!, anchor, recursive);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void WithRecursive_ParametersRemapped_NoCollisions()
    {
        var anchor = Sql().From("Employees").Where(w => w.EqualTo(x => x.Id, 1)).Build();
        var recursive = Sql().From("Employees").Where(w => w.EqualTo(x => x.IsActive, true)).Build();

        var result = Sql()
            .WithRecursive("cte", anchor, recursive)
            .From("cte")
            .Where(w => w.EqualTo(x => x.Age, 30))
            .Build();

        result.Parameters.Keys.Should().OnlyHaveUniqueItems();
        result.Parameters.Count.Should().Be(3);
    }

    // ── SelectIf ──────────────────────────────────────────────────────────────

    [Fact]
    public void SelectIf_True_IncludesColumn()
    {
        var result = Sql()
            .From("Users")
            .SelectIf(true, x => x.Name)
            .Build();

        result.Sql.Should().Contain("[Name]");
    }

    [Fact]
    public void SelectIf_False_SkipsColumn()
    {
        var result = Sql()
            .From("Users")
            .SelectIf(false, x => x.Name)
            .Build();

        result.Sql.Should().NotContain("[Name]");
    }

    [Fact]
    public void SelectIf_True_WithAlias_IncludesColumnWithAlias()
    {
        var result = Sql()
            .From("Users")
            .SelectIf(true, x => x.Name, "UserName")
            .Build();

        result.Sql.Should().Contain("[Name] AS [UserName]");
    }

    // ── WhereIf (Action) ──────────────────────────────────────────────────────

    [Fact]
    public void WhereIf_Action_True_AppliesWhere()
    {
        var result = Sql()
            .From("Users")
            .WhereIf(true, w => w.EqualTo(x => x.IsActive, true))
            .Build();

        result.Sql.Should().Contain("WHERE");
        result.Sql.Should().Contain("[IsActive]");
    }

    [Fact]
    public void WhereIf_Action_False_SkipsWhere()
    {
        var result = Sql()
            .From("Users")
            .WhereIf(false, w => w.EqualTo(x => x.IsActive, true))
            .Build();

        result.Sql.Should().NotContain("WHERE");
    }

    // ── WhereIf (Expression) ──────────────────────────────────────────────────

    [Fact]
    public void WhereIf_Expression_True_AppliesWhere()
    {
        var result = Sql()
            .From("Users")
            .WhereIf(true, x => x.Age > 18)
            .Build();

        result.Sql.Should().Contain("WHERE");
    }

    [Fact]
    public void WhereIf_Expression_False_SkipsWhere()
    {
        var result = Sql()
            .From("Users")
            .WhereIf(false, x => x.Age > 18)
            .Build();

        result.Sql.Should().NotContain("WHERE");
    }

    // ── OrderByIf ─────────────────────────────────────────────────────────────

    [Fact]
    public void OrderByIf_True_AppliesOrderBy()
    {
        var result = Sql()
            .From("Users")
            .OrderByIf(true, x => x.Name)
            .Build();

        result.Sql.Should().Contain("ORDER BY [Name]");
    }

    [Fact]
    public void OrderByIf_False_SkipsOrderBy()
    {
        var result = Sql()
            .From("Users")
            .OrderByIf(false, x => x.Name)
            .Build();

        result.Sql.Should().NotContain("ORDER BY");
    }

    // ── NULLS FIRST / NULLS LAST ──────────────────────────────────────────────

    [Fact]
    public void NullsFirst_PostgreSQL_GeneratesNullsFirstClause()
    {
        var result = PgSql()
            .From("Users")
            .OrderBy(x => x.Name, ascending: true, NullsOrder.First)
            .Build();

        result.Sql.Should().Contain("ORDER BY \"Name\" ASC NULLS FIRST");
    }

    [Fact]
    public void NullsLast_PostgreSQL_GeneratesNullsLastClause()
    {
        var result = PgSql()
            .From("Users")
            .OrderBy(x => x.Name, ascending: false, NullsOrder.Last)
            .Build();

        result.Sql.Should().Contain("ORDER BY \"Name\" DESC NULLS LAST");
    }

    [Fact]
    public void NullsOrder_SqlServer_Ignored()
    {
        var result = Sql()
            .From("Users")
            .OrderBy(x => x.Name, ascending: true, NullsOrder.First)
            .Build();

        result.Sql.Should().Contain("ORDER BY [Name] ASC");
        result.Sql.Should().NotContain("NULLS");
    }

    // ── FOR UPDATE / FOR SHARE ────────────────────────────────────────────────

    [Fact]
    public void ForUpdate_PostgreSQL_GeneratesForUpdateSuffix()
    {
        var result = PgSql()
            .From("Users")
            .ForUpdate()
            .Build();

        result.Sql.Should().EndWith("FOR UPDATE");
    }

    [Fact]
    public void ForShare_PostgreSQL_GeneratesForShareSuffix()
    {
        var result = PgSql()
            .From("Users")
            .ForShare()
            .Build();

        result.Sql.Should().EndWith("FOR SHARE");
    }

    [Fact]
    public void ForUpdate_SQLite_Ignored()
    {
        var result = new SqlQueryBuilder<TestUser>(new SqliteDialect())
            .From("Users")
            .ForUpdate()
            .Build();

        result.Sql.Should().NotContain("FOR UPDATE");
        result.Sql.Should().NotContain("FOR SHARE");
    }

    [Fact]
    public void ForShare_SQLite_Ignored()
    {
        var result = new SqlQueryBuilder<TestUser>(new SqliteDialect())
            .From("Users")
            .ForShare()
            .Build();

        result.Sql.Should().NotContain("FOR UPDATE");
        result.Sql.Should().NotContain("FOR SHARE");
    }

    [Fact]
    public void ForUpdate_SqlServer_Ignored()
    {
        var result = Sql()
            .From("Users")
            .ForUpdate()
            .Build();

        result.Sql.Should().NotContain("FOR UPDATE");
        result.Sql.Should().NotContain("FOR SHARE");
    }

    [Fact]
    public void ForShare_SqlServer_Ignored()
    {
        var result = Sql()
            .From("Users")
            .ForShare()
            .Build();

        result.Sql.Should().NotContain("FOR UPDATE");
        result.Sql.Should().NotContain("FOR SHARE");
    }

    [Fact]
    public void ForUpdate_MySQL_GeneratesForUpdateSuffix()
    {
        var result = new SqlQueryBuilder<TestUser>(new MySqlDialect())
            .From("Users")
            .ForUpdate()
            .Build();

        result.Sql.Should().EndWith("FOR UPDATE");
    }

    [Fact]
    public void ForShare_MySQL_GeneratesForShareSuffix()
    {
        var result = new SqlQueryBuilder<TestUser>(new MySqlDialect())
            .From("Users")
            .ForShare()
            .Build();

        result.Sql.Should().EndWith("FOR SHARE");
    }

    // ── Feature 1: SelectCoalesce / SelectCast / SelectConcat / SelectRawIf ───

    [Fact]
    public void SelectCoalesce_SqlServer_GeneratesCoalesceInSelect()
    {
        var result = Sql()
            .From("Users")
            .SelectCoalesce(x => x.Department, "'N/A'", "Dept")
            .Build();

        result.Sql.Should().Contain("COALESCE([Department], 'N/A') AS [Dept]");
    }

    [Fact]
    public void SelectCast_GeneratesCastInSelect()
    {
        var result = Sql()
            .From("Users")
            .SelectCast(x => x.Age, "FLOAT", "AgeFloat")
            .Build();

        result.Sql.Should().Contain("CAST([Age] AS FLOAT) AS [AgeFloat]");
    }

    [Fact]
    public void SelectConcat_SqlServer_UsesPlus()
    {
        var result = Sql()
            .From("Users")
            .SelectConcat("FullName", x => x.Name, x => x.Email)
            .Build();

        result.Sql.Should().Contain("[Name] + [Email] AS [FullName]");
    }

    [Fact]
    public void SelectConcat_PostgreSQL_UsesPipe()
    {
        var result = PgSql()
            .From("users")
            .SelectConcat("FullName", x => x.Name, x => x.Email)
            .Build();

        result.Sql.Should().Contain("\"Name\" || \"Email\" AS \"FullName\"");
    }

    [Fact]
    public void SelectRawIf_True_IncludesRaw()
    {
        var result = Sql()
            .From("Users")
            .SelectRawIf(true, "GETDATE() AS [Now]")
            .Build();

        result.Sql.Should().Contain("GETDATE() AS [Now]");
    }

    [Fact]
    public void SelectRawIf_False_ExcludesRaw()
    {
        var result = Sql()
            .From("Users")
            .SelectRawIf(false, "GETDATE() AS [Now]")
            .Build();

        result.Sql.Should().NotContain("GETDATE()");
    }

    // ── Feature 2: GroupByIf / HavingIf ──────────────────────────────────────

    [Fact]
    public void GroupByIf_True_AppliesGroupBy()
    {
        var result = Sql()
            .From("Users")
            .SelectCount("cnt")
            .GroupByIf(true, x => x.Department)
            .Build();

        result.Sql.Should().Contain("GROUP BY [Department]");
    }

    [Fact]
    public void GroupByIf_False_SkipsGroupBy()
    {
        var result = Sql()
            .From("Users")
            .SelectCount("cnt")
            .GroupByIf(false, x => x.Department)
            .Build();

        result.Sql.Should().NotContain("GROUP BY");
    }

    [Fact]
    public void HavingIf_True_AppliesHaving()
    {
        var result = Sql()
            .From("Users")
            .GroupBy(x => x.Department)
            .HavingIf(true, h => h.CountGreaterThan(5))
            .Build();

        result.Sql.Should().Contain("HAVING COUNT(*) > @ph0");
        result.Parameters["ph0"].Should().Be(5);
    }

    [Fact]
    public void HavingIf_False_SkipsHaving()
    {
        var result = Sql()
            .From("Users")
            .GroupBy(x => x.Department)
            .HavingIf(false, h => h.CountGreaterThan(5))
            .Build();

        result.Sql.Should().NotContain("HAVING");
    }

    // ── Feature 3: OrderByRaw ─────────────────────────────────────────────────

    [Fact]
    public void OrderByRaw_AppendsRawOrderByClause()
    {
        var result = Sql()
            .From("Users")
            .OrderByRaw("[Salary] DESC, [Name] ASC")
            .Build();

        result.Sql.Should().Contain("ORDER BY [Salary] DESC, [Name] ASC");
    }

    [Fact]
    public void OrderByRaw_NullOrWhitespace_Throws()
    {
        var act = () => Sql().OrderByRaw("  ");
        act.Should().Throw<ArgumentException>().WithParameterName("rawSql");
    }

    // ── Feature 4a: LAG / LEAD / FIRST_VALUE / LAST_VALUE ────────────────────

    [Fact]
    public void SelectLag_GeneratesLagExpression()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectLag(x => x.Salary, 1, x => x.Department, x => x.CreatedAt, ascending: false, alias: "PrevSalary")
            .Build();

        result.Sql.Should().Contain("LAG([Salary], 1) OVER (PARTITION BY [Department] ORDER BY [CreatedAt] DESC) AS [PrevSalary]");
    }

    [Fact]
    public void SelectLead_GeneratesLeadExpression()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectLead(x => x.Salary, 1, null, x => x.CreatedAt, ascending: true, alias: "NextSalary")
            .Build();

        result.Sql.Should().Contain("LEAD([Salary], 1) OVER (ORDER BY [CreatedAt] ASC) AS [NextSalary]");
    }

    [Fact]
    public void SelectFirstValue_GeneratesFirstValueExpression()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectFirstValue(x => x.Salary, x => x.Department, x => x.CreatedAt, ascending: true, alias: "FirstSalary")
            .Build();

        result.Sql.Should().Contain("FIRST_VALUE([Salary]) OVER (PARTITION BY [Department] ORDER BY [CreatedAt] ASC) AS [FirstSalary]");
    }

    [Fact]
    public void SelectLastValue_GeneratesLastValueExpression()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectLastValue(x => x.Salary, null, x => x.CreatedAt, ascending: false, alias: "LastSalary")
            .Build();

        result.Sql.Should().Contain("LAST_VALUE([Salary]) OVER (ORDER BY [CreatedAt] DESC) AS [LastSalary]");
    }

    // ── Feature 4b: SUM/AVG/COUNT OVER ───────────────────────────────────────

    [Fact]
    public void SelectSumOver_GeneratesSumWindowExpression()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectSumOver(x => x.Salary, x => x.Department, x => x.CreatedAt, ascending: true, alias: "RunningTotal")
            .Build();

        result.Sql.Should().Contain("SUM([Salary]) OVER (PARTITION BY [Department] ORDER BY [CreatedAt] ASC) AS [RunningTotal]");
    }

    [Fact]
    public void SelectAvgOver_GeneratesAvgWindowExpression()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectAvgOver(x => x.Salary, null, null, alias: "AvgSalary")
            .Build();

        result.Sql.Should().Contain("AVG([Salary]) OVER () AS [AvgSalary]");
    }

    [Fact]
    public void SelectCountOver_GeneratesCountWindowExpression()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectCountOver(x => x.Id, x => x.Department, null, alias: "DeptCount")
            .Build();

        result.Sql.Should().Contain("COUNT([Id]) OVER (PARTITION BY [Department]) AS [DeptCount]");
    }

    // ── Feature 4c: ROW_NUMBER with multiple PARTITION BY columns ─────────────

    [Fact]
    public void SelectRowNumber_MultiplePartitionColumns_GeneratesPartitionByList()
    {
        var result = Sql()
            .From("Users")
            .Select(x => x.Id)
            .SelectRowNumber(
                new Expression<Func<TestUser, object>>[] { x => x.Department, x => x.Status },
                x => x.Salary,
                ascending: false,
                alias: "Rn")
            .Build();

        result.Sql.Should().Contain("ROW_NUMBER() OVER (PARTITION BY [Department], [Status] ORDER BY [Salary] DESC) AS [Rn]");
    }

    // ── Feature 4d: Tag with logger callback ──────────────────────────────────

    [Fact]
    public void Tag_WithLogger_InvokesLoggerOnBuild()
    {
        var logged = new List<string>();

        Sql()
            .From("Users")
            .Tag("Get all users", msg => logged.Add(msg))
            .Build();

        logged.Should().ContainSingle()
            .Which.Should().Be("[SQL] Get all users");
    }

    // ── JoinSubquery ──────────────────────────────────────────────────────────

    [Fact]
    public void InnerJoinSubquery_GeneratesCorrectSql()
    {
        var subquery = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Orders")
            .Select(x => x.Id)
            .Build();

        var result = Sql()
            .From("Users")
            .Select(x => x.Name)
            .InnerJoinSubquery(subquery, "o", "[Users].[Id] = [o].[Id]")
            .Build();

        result.Sql.Should().Contain("INNER JOIN (");
        result.Sql.Should().Contain(") [o] ON [Users].[Id] = [o].[Id]");
    }

    [Fact]
    public void LeftJoinSubquery_GeneratesCorrectSql()
    {
        var subquery = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Orders")
            .Build();

        var result = Sql()
            .From("Users")
            .LeftJoinSubquery(subquery, "o", "[Users].[Id] = [o].[Id]")
            .Build();

        result.Sql.Should().Contain("LEFT JOIN (");
    }

    [Fact]
    public void RightJoinSubquery_GeneratesCorrectSql()
    {
        var subquery = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Orders")
            .Build();

        var result = Sql()
            .From("Users")
            .RightJoinSubquery(subquery, "o", "[Users].[Id] = [o].[Id]")
            .Build();

        result.Sql.Should().Contain("RIGHT JOIN (");
    }

    [Fact]
    public void FullOuterJoinSubquery_GeneratesCorrectSql()
    {
        var subquery = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Orders")
            .Build();

        var result = Sql()
            .From("Users")
            .FullOuterJoinSubquery(subquery, "o", "[Users].[Id] = [o].[Id]")
            .Build();

        result.Sql.Should().Contain("FULL OUTER JOIN (");
    }

    [Fact]
    public void JoinSubquery_ParametersRemappedCorrectly()
    {
        // Subquery has its own parameter @p0 (Age = 18)
        var subquery = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Orders")
            .Where(x => x.Age > 18)
            .Build();

        // Outer query also has a WHERE that will produce its own parameter
        var result = Sql()
            .From("Users")
            .Where(x => x.IsActive == true)
            .InnerJoinSubquery(subquery, "o", "[Users].[Id] = [o].[Id]")
            .Build();

        // The subquery parameters should have been remapped — no key collisions
        result.Parameters.Should().NotBeEmpty();
        // All parameter keys must be unique (no overwrite)
        result.Parameters.Keys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void JoinSubquery_SubqueryWithParameters_IncludedInResult()
    {
        var subquery = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Archive")
            .Where(x => x.Age > 25)
            .Build();

        var result = Sql()
            .From("Users")
            .InnerJoinSubquery(subquery, "arch", "[Users].[Id] = [arch].[Id]")
            .Build();

        result.Parameters.Should().NotBeEmpty();
        result.Parameters.Values.Should().Contain(25);
    }

    [Fact]
    public void JoinSubquery_CombinedWithRegularJoin_BothAppear()
    {
        var subquery = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Orders")
            .Build();

        var result = Sql()
            .From("Users")
            .InnerJoin("Departments", "d", "[Users].[Department] = [d].[Name]")
            .InnerJoinSubquery(subquery, "o", "[Users].[Id] = [o].[Id]")
            .Build();

        result.Sql.Should().Contain("INNER JOIN [Departments]");
        result.Sql.Should().Contain("INNER JOIN (");
    }

    [Fact]
    public void JoinSubquery_NullSubquery_ThrowsArgumentNullException()
    {
        var builder = Sql().From("Users");
        builder.Invoking(b => b.InnerJoinSubquery(null!, "alias", "ON 1=1"))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void JoinSubquery_EmptyAlias_ThrowsArgumentException()
    {
        var subquery = new SqlQueryBuilder<TestUser>(new SqlServerDialect()).From("Orders").Build();
        var builder = Sql().From("Users");
        builder.Invoking(b => b.InnerJoinSubquery(subquery, "  ", "ON 1=1"))
            .Should().Throw<ArgumentException>();
    }
}
