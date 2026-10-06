using System.Diagnostics;
using FluentAssertions;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests.Builder;

/// <summary>Tests for <see cref="SqlInsertBuilder{T}"/>.</summary>
public sealed class SqlInsertBuilderTests
{
    private static readonly ISqlDialect Sql = new SqlServerDialect();
    private static readonly ISqlDialect Pg = new PostgreSqlDialect();
    private static readonly ISqlDialect My = new MySqlDialect();

    // Parents every captured activity under a private root so concurrently running tests (xunit
    // parallelizes test classes by default) can never pollute this test's capture, even though they
    // share the same process-wide ValiFlowDiagnostics.Source.
    private static (ActivityListener Listener, Activity Root, List<Activity> Captured) AttachScopedListener()
    {
        var captured = new List<Activity>();
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ValiFlowDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = captured.Add
        };
        ActivitySource.AddActivityListener(listener);

        var root = new Activity("TestRoot").Start();
        return (listener, root, captured);
    }

    private static Activity GetOwn(List<Activity> captured, Activity root)
        => captured.Single(a => a.ParentId == root.Id);

    // ── Basic INSERT ──────────────────────────────────────────────────────────

    [Fact]
    public void SingleSet_GeneratesInsertWithOneColumn()
    {
        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Alice")
            .Build();

        result.Sql.Should().Be("INSERT INTO [Users] ([Name]) VALUES (@pi0)");
        result.Parameters["pi0"].Should().Be("Alice");
    }

    [Fact]
    public void Into_UnsafeTableName_ThrowsArgumentException()
    {
        var act = () => new SqlInsertBuilder<TestUser>(Sql).Into("Users]; DROP TABLE Users;--");

        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    [Fact]
    public void Into_UnsafeSchema_ThrowsArgumentException()
    {
        var act = () => new SqlInsertBuilder<TestUser>(Sql).Into("Users", "dbo]; DROP TABLE Users;--");

        act.Should().Throw<ArgumentException>().WithParameterName("schema");
    }

    [Fact]
    public void MultipleSet_GeneratesCorrectColumnAndParamOrder()
    {
        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Bob")
            .Set(x => x.Age, 30)
            .Set(x => x.IsActive, true)
            .Build();

        result.Sql.Should().Be("INSERT INTO [Users] ([Name], [Age], [IsActive]) VALUES (@pi0, @pi1, @pi2)");
        result.Parameters["pi0"].Should().Be("Bob");
        result.Parameters["pi1"].Should().Be(30);
        result.Parameters["pi2"].Should().Be(true);
    }

    [Fact]
    public void NullValue_StoresDbNull()
    {
        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set<string?>(x => x.Email, null)
            .Build();

        result.Parameters["pi0"].Should().Be(DBNull.Value);
    }

    // ── Table name ────────────────────────────────────────────────────────────

    [Fact]
    public void Into_WithSchema_GeneratesSchemaQualifiedTable()
    {
        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users", "dbo")
            .Set(x => x.Name, "Charlie")
            .Build();

        result.Sql.Should().StartWith("INSERT INTO [dbo].[Users]");
    }

    [Fact]
    public void Into_NotCalled_UsesTypeName()
    {
        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Set(x => x.Name, "Default")
            .Build();

        result.Sql.Should().StartWith("INSERT INTO [TestUser]");
    }

    // ── OUTPUT INSERTED ───────────────────────────────────────────────────────

    [Fact]
    public void OutputInserted_AppendsOutputClause()
    {
        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Dana")
            .OutputInserted()
            .Build();

        result.Sql.Should().Be("INSERT INTO [Users] ([Name]) OUTPUT INSERTED.* VALUES (@pi0)");
    }

    // ── Dialects ──────────────────────────────────────────────────────────────

    [Fact]
    public void PostgreSql_UsesDoubleQuotes()
    {
        var result = new SqlInsertBuilder<TestUser>(Pg)
            .Into("Users")
            .Set(x => x.Name, "Eve")
            .Build();

        result.Sql.Should().Be("INSERT INTO \"Users\" (\"Name\") VALUES (@pi0)");
    }

    [Fact]
    public void MySql_UsesBackticks()
    {
        var result = new SqlInsertBuilder<TestUser>(My)
            .Into("Users")
            .Set(x => x.Name, "Frank")
            .Build();

        result.Sql.Should().Be("INSERT INTO `Users` (`Name`) VALUES (@pi0)");
    }

    // ── Tag ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Tag_PrependsSqlComment()
    {
        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Tagged")
            .Tag("Insert new user")
            .Build();

        result.Sql.Should().StartWith("-- Insert new user\n");
        result.Sql.Should().Contain("INSERT INTO [Users]");
    }

    [Fact]
    public void Tag_NullOrWhitespace_ThrowsArgumentException()
    {
        var builder = new SqlInsertBuilder<TestUser>(Sql).Into("Users").Set(x => x.Name, "X");
        builder.Invoking(b => b.Tag("   ")).Should().Throw<ArgumentException>();
        builder.Invoking(b => b.Tag("")).Should().Throw<ArgumentException>();
    }

    // ── Multi-row INSERT ──────────────────────────────────────────────────────

    [Fact]
    public void MultiRow_TwoRows_GeneratesMultipleValueSets()
    {
        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Alice").Set(x => x.Age, 30)
            .NextRow()
            .Set(x => x.Name, "Bob").Set(x => x.Age, 25)
            .Build();

        result.Sql.Should().Be("INSERT INTO [Users] ([Name], [Age]) VALUES (@pi0, @pi1), (@pi2, @pi3)");
        result.Parameters["pi0"].Should().Be("Alice");
        result.Parameters["pi1"].Should().Be(30);
        result.Parameters["pi2"].Should().Be("Bob");
        result.Parameters["pi3"].Should().Be(25);
    }

    [Fact]
    public void MultiRow_ThreeRows_GeneratesThreeValueSets()
    {
        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "A")
            .NextRow()
            .Set(x => x.Name, "B")
            .NextRow()
            .Set(x => x.Name, "C")
            .Build();

        result.Sql.Should().Contain("VALUES (@pi0), (@pi1), (@pi2)");
    }

    [Fact]
    public void MultiRow_MismatchedColumnCount_ThrowsInvalidOperationException()
    {
        var builder = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Alice").Set(x => x.Age, 30)
            .NextRow()
            .Set(x => x.Name, "Bob"); // missing Age

        builder.Invoking(b => b.Build()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NextRow_EmptyCurrentRow_ThrowsInvalidOperationException()
    {
        var builder = new SqlInsertBuilder<TestUser>(Sql).Into("Users");
        builder.Invoking(b => b.NextRow()).Should().Throw<InvalidOperationException>();
    }

    // ── RETURNING ─────────────────────────────────────────────────────────────

    [Fact]
    public void Returning_NoColumns_AppendsReturningAll()
    {
        var result = new SqlInsertBuilder<TestUser>(new SqliteDialect())
            .Into("Users")
            .Set(x => x.Name, "Eve")
            .Returning()
            .Build();

        result.Sql.Should().EndWith("RETURNING *");
    }

    [Fact]
    public void Returning_SpecificColumns_AppendsReturningColumnList()
    {
        var result = new SqlInsertBuilder<TestUser>(new PostgreSqlDialect())
            .Into("Users")
            .Set(x => x.Name, "Frank")
            .Returning(x => x.Id, x => x.Name)
            .Build();

        result.Sql.Should().EndWith("RETURNING \"Id\", \"Name\"");
    }

    [Fact]
    public void OutputInserted_SqlServer_Works()
    {
        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Test")
            .OutputInserted()
            .Build();

        result.Sql.Should().Contain("OUTPUT INSERTED.*");
        result.Sql.Should().Contain("VALUES");
    }

    [Fact]
    public void OutputInserted_NonSqlServer_ThrowsInvalidOperationException()
    {
        var builder = new SqlInsertBuilder<TestUser>(Pg)
            .Into("Users")
            .Set(x => x.Name, "Test")
            .OutputInserted();

        builder.Invoking(b => b.Build()).Should().Throw<InvalidOperationException>()
            .WithMessage("*SqlServerDialect*");
    }

    [Fact]
    public void Returning_SqlServer_ThrowsInvalidOperationException()
    {
        var builder = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Test")
            .Returning();

        builder.Invoking(b => b.Build()).Should().Throw<InvalidOperationException>()
            .WithMessage("*SqlServer*");
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public void Build_NoAssignments_ThrowsInvalidOperationException()
    {
        var builder = new SqlInsertBuilder<TestUser>(Sql).Into("Users");
        builder.Invoking(b => b.Build()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_NullDialect_ThrowsArgumentNullException()
    {
        var act = () => new SqlInsertBuilder<TestUser>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── Conflict resolution (UPSERT) ──────────────────────────────────────────

    [Fact]
    public void OrIgnore_SQLite_GeneratesInsertOrIgnore()
    {
        var result = new SqlInsertBuilder<TestUser>(new SqliteDialect())
            .Into("Users")
            .Set(x => x.Name, "Alice")
            .OrIgnore()
            .Build();

        result.Sql.Should().StartWith("INSERT OR IGNORE INTO");
    }

    [Fact]
    public void OrReplace_SQLite_GeneratesInsertOrReplace()
    {
        var result = new SqlInsertBuilder<TestUser>(new SqliteDialect())
            .Into("Users")
            .Set(x => x.Name, "Alice")
            .OrReplace()
            .Build();

        result.Sql.Should().StartWith("INSERT OR REPLACE INTO");
    }

    [Fact]
    public void OnConflictDoNothing_PostgreSQL_AppendsOnConflict()
    {
        var result = new SqlInsertBuilder<TestUser>(Pg)
            .Into("Users")
            .Set(x => x.Name, "Alice")
            .OnConflictDoNothing()
            .Build();

        result.Sql.Should().Contain("ON CONFLICT DO NOTHING");
    }

    [Fact]
    public void OnConflictDoUpdate_PostgreSQL_AppendsUpdateClause()
    {
        var result = new SqlInsertBuilder<TestUser>(Pg)
            .Into("Users")
            .Set(x => x.Name, "Alice")
            .Set(x => x.Age, 30)
            .OnConflictDoUpdate(
                keys => keys.AddConflictKey(x => x.Id),
                updates => updates.AddConflictUpdate(x => x.Name, "Alice Updated"))
            .Build();

        result.Sql.Should().Contain("ON CONFLICT (\"Id\") DO UPDATE SET");
        result.Sql.Should().Contain("\"Name\" = @pu2");
        result.Parameters["pu2"].Should().Be("Alice Updated");
    }

    [Fact]
    public void InsertIgnore_MySQL_GeneratesInsertIgnore()
    {
        var result = new SqlInsertBuilder<TestUser>(My)
            .Into("Users")
            .Set(x => x.Name, "Alice")
            .InsertIgnore()
            .Build();

        result.Sql.Should().StartWith("INSERT IGNORE INTO");
    }

    [Fact]
    public void OnDuplicateKeyUpdate_MySQL_AppendsClause()
    {
        var result = new SqlInsertBuilder<TestUser>(My)
            .Into("Users")
            .Set(x => x.Name, "Alice")
            .Set(x => x.Age, 25)
            .OnDuplicateKeyUpdate(cfg => cfg
                .AddDuplicateKeyAssignment(x => x.Name, "Alice Updated")
                .AddDuplicateKeyAssignment(x => x.Age, 26))
            .Build();

        result.Sql.Should().Contain("ON DUPLICATE KEY UPDATE");
        result.Sql.Should().Contain("`Name` = @pu2");
        result.Sql.Should().Contain("`Age` = @pu3");
        result.Parameters["pu2"].Should().Be("Alice Updated");
        result.Parameters["pu3"].Should().Be(26);
    }

    [Fact]
    public void OrIgnore_SqlServer_ThrowsInvalidOperationException()
    {
        var builder = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Alice");

        builder.Invoking(b => b.OrIgnore())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*SqlServer*");
    }

    [Fact]
    public void InsertIgnore_SqlServer_ThrowsInvalidOperationException()
    {
        var builder = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Alice");

        builder.Invoking(b => b.InsertIgnore())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*SqlServer*");
    }

    [Fact]
    public void OrIgnore_MySql_ThrowsInvalidOperationException()
    {
        // MySQL does not support the SQLite-only "INSERT OR IGNORE INTO" syntax.
        // MySqlDialect.InsertConflictPrefix is "IGNORE" (used for its own InsertIgnore() keyword),
        // which previously made the OrIgnore() guard pass incorrectly and emit invalid SQL
        // ("INSERT OR IGNORE INTO ..." instead of "INSERT IGNORE INTO ...").
        var builder = new SqlInsertBuilder<TestUser>(My)
            .Into("Users")
            .Set(x => x.Name, "Alice");

        builder.Invoking(b => b.OrIgnore())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*MySQL*");
    }

    [Fact]
    public void OrReplace_PostgreSql_ThrowsInvalidOperationException()
    {
        // PostgreSQL does not support the SQLite-only "INSERT OR REPLACE INTO" syntax either.
        // PostgreSqlDialect.SupportsOnConflict is true (for ON CONFLICT DO UPDATE/NOTHING),
        // which previously made the OrReplace() guard pass incorrectly.
        var builder = new SqlInsertBuilder<TestUser>(Pg)
            .Into("Users")
            .Set(x => x.Name, "Alice");

        builder.Invoking(b => b.OrReplace())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*PostgreSQL*");
    }

    // ── INSERT … SELECT ───────────────────────────────────────────────────────

    [Fact]
    public void SelectFrom_NoColumns_GeneratesInsertSelect()
    {
        var select = new SqlQueryBuilder<TestUser>(Sql)
            .From("Archive")
            .Build();

        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .SelectFrom(select)
            .Build();

        result.Sql.Should().Be("INSERT INTO [Users] SELECT * FROM [Archive]");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void SelectFrom_WithColumns_GeneratesInsertSelectWithColumnList()
    {
        var select = new SqlQueryBuilder<TestUser>(Sql)
            .From("Archive")
            .Select(x => x.Name)
            .Select(x => x.Age)
            .Build();

        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Columns(x => x.Name, x => x.Age)
            .SelectFrom(select)
            .Build();

        result.Sql.Should().Contain("INSERT INTO [Users] ([Name], [Age])");
        result.Sql.Should().Contain("SELECT");
        result.Sql.Should().Contain("[Name]");
    }

    [Fact]
    public void SelectFrom_ParametersFromSelectAreIncluded()
    {
        var select = new SqlQueryBuilder<TestUser>(Sql)
            .From("Archive")
            .Where(x => x.Age > 18)
            .Build();

        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .SelectFrom(select)
            .Build();

        result.Parameters.Should().NotBeEmpty();
        result.Parameters.Values.Should().Contain(18);
    }

    [Fact]
    public void SelectFrom_PostgreSql_UsesDoubleQuotes()
    {
        var select = new SqlQueryBuilder<TestUser>(Pg)
            .From("Archive")
            .Select(x => x.Name)
            .Build();

        var result = new SqlInsertBuilder<TestUser>(Pg)
            .Into("Users")
            .Columns(x => x.Name)
            .SelectFrom(select)
            .Build();

        result.Sql.Should().StartWith("INSERT INTO \"Users\"");
        result.Sql.Should().Contain("(\"Name\")");
    }

    [Fact]
    public void SelectFrom_CombinedWithTag_PrependsSqlComment()
    {
        var select = new SqlQueryBuilder<TestUser>(Sql)
            .From("Archive")
            .Build();

        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .SelectFrom(select)
            .Tag("Copy from archive")
            .Build();

        result.Sql.Should().StartWith("-- Copy from archive\n");
        result.Sql.Should().Contain("INSERT INTO [Users]");
    }

    [Fact]
    public void SelectFrom_AndSet_ThrowsInvalidOperationException()
    {
        var select = new SqlQueryBuilder<TestUser>(Sql)
            .From("Archive")
            .Build();

        var builder = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Alice");

        builder.Invoking(b => b.SelectFrom(select))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Set_AfterSelectFrom_ThrowsInvalidOperationException()
    {
        // Reverse of SelectFrom_AndSet_ThrowsInvalidOperationException: calling Set() after
        // SelectFrom() was previously NOT guarded. Build() always checks _selectQuery first and
        // routes into BuildInsertSelect(), silently discarding whatever was passed to Set() with
        // no error raised to the caller — an asymmetric, order-dependent validation gap.
        var select = new SqlQueryBuilder<TestUser>(Sql)
            .From("Archive")
            .Build();

        var builder = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .SelectFrom(select);

        builder.Invoking(b => b.Set(x => x.Name, "Alice"))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SelectFrom_NullQuery_ThrowsArgumentNullException()
    {
        var builder = new SqlInsertBuilder<TestUser>(Sql).Into("Users");
        builder.Invoking(b => b.SelectFrom(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Columns_EmptyArray_ThrowsArgumentException()
    {
        var builder = new SqlInsertBuilder<TestUser>(Sql).Into("Users");
        builder.Invoking(b => b.Columns())
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SelectFrom_MySql_UsesBackticks()
    {
        var select = new SqlQueryBuilder<TestUser>(My)
            .From("Archive")
            .Build();

        var result = new SqlInsertBuilder<TestUser>(My)
            .Into("Users")
            .Columns(x => x.Name)
            .SelectFrom(select)
            .Build();

        result.Sql.Should().StartWith("INSERT INTO `Users`");
        result.Sql.Should().Contain("(`Name`)");
    }

    // ── SetAllFrom ────────────────────────────────────────────────────────────

    [Fact]
    public void SetAllFrom_MapsAllPublicProperties()
    {
        var entity = new TestUser
        {
            Id = 1,
            Name = "Alice",
            Email = "alice@test.com",
            Age = 30,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1),
            Department = "Eng",
            Salary = 1000m,
            Status = 1,
            Counter = 0,
            NameBackup = "AliceBackup",
            UpdatedAt = new DateTime(2024, 1, 2)
        };

        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .SetAllFrom(entity)
            .Build();

        result.Sql.Should().Contain("[Id]").And.Contain("[Name]").And.Contain("[Email]");
        result.Parameters.Values.Should().Contain("Alice");
        result.Parameters.Values.Should().Contain("alice@test.com");
        result.Parameters.Values.Should().Contain(30);
        result.Parameters.Values.Should().Contain(true);
    }

    [Fact]
    public void SetAllFrom_WithExclusions_SkipsExcludedColumns()
    {
        var entity = new TestUser { Id = 1, Name = "Bob", Email = "bob@test.com" };

        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .SetAllFrom(entity, x => x.Id, x => x.CreatedAt, x => x.UpdatedAt)
            .Build();

        result.Sql.Should().NotContain("[Id]");
        result.Sql.Should().NotContain("[CreatedAt]");
        result.Sql.Should().NotContain("[UpdatedAt]");
        result.Sql.Should().Contain("[Name]");
    }

    [Fact]
    public void SetAllFrom_NullPropertyValue_StoresDbNull()
    {
        var entity = new TestUser { Id = 1, Name = "Carl", Email = null! };

        var result = new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .SetAllFrom(entity)
            .Build();

        result.Parameters.Values.Should().Contain(DBNull.Value);
    }

    [Fact]
    public void SetAllFrom_NullEntity_ThrowsArgumentNullException()
    {
        var builder = new SqlInsertBuilder<TestUser>(Sql).Into("Users");

        builder.Invoking(b => b.SetAllFrom(null!))
            .Should().Throw<ArgumentNullException>().WithParameterName("entity");
    }

    // ── Observability ─────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithTag_RecordsActivityWithTagAndEntityType()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;

        new SqlInsertBuilder<TestUser>(Sql)
            .Into("Users")
            .Set(x => x.Name, "Alice")
            .Tag("Insert new user")
            .Build();

        var seen = GetOwn(captured, root);
        seen.GetTagItem("vali_flow.tag").Should().Be("Insert new user");
        seen.GetTagItem("vali_flow.entity_type").Should().Be("TestUser");
        seen.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Fact]
    public void Build_WhenValidationThrows_RecordsErrorStatus()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;

        // No Set() calls at all triggers the existing "at least one column" guard in Build().
        var act = () => new SqlInsertBuilder<TestUser>(Sql).Into("Users").Build();

        act.Should().Throw<InvalidOperationException>();
        GetOwn(captured, root).Status.Should().Be(ActivityStatusCode.Error);
    }
}
