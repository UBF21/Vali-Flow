using FluentAssertions;
using Vali_Flow.Core.Builder;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests.Builder;

/// <summary>Tests for <see cref="SqlUpdateBuilder{T}"/>.</summary>
public sealed class SqlUpdateBuilderTests
{
    private static readonly ISqlDialect Sql = new SqlServerDialect();
    private static readonly ISqlDialect Pg = new PostgreSqlDialect();
    private static readonly ISqlDialect My = new MySqlDialect();

    // ── Basic UPDATE ──────────────────────────────────────────────────────────

    [Fact]
    public void SingleSet_NoWhere_GeneratesUpdateWithoutWhere()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.Name, "Alice")
            .AllowUpdateAll()
            .Build();

        result.Sql.Should().Be("UPDATE [Users] SET [Name] = @pu0");
        result.Parameters["pu0"].Should().Be("Alice");
    }

    [Fact]
    public void MultipleSet_GeneratesCorrectSetList()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.Name, "Bob")
            .Set(x => x.Age, 25)
            .Set(x => x.IsActive, false)
            .AllowUpdateAll()
            .Build();

        result.Sql.Should().Be("UPDATE [Users] SET [Name] = @pu0, [Age] = @pu1, [IsActive] = @pu2");
    }

    [Fact]
    public void NullValue_StoresDbNull()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set<string?>(x => x.Email, null)
            .AllowUpdateAll()
            .Build();

        result.Parameters["pu0"].Should().Be(DBNull.Value);
    }

    // ── WHERE variants ────────────────────────────────────────────────────────

    [Fact]
    public void Where_WithSqlWhereBuilder_AppendsWhereClause()
    {
        var wb = new SqlWhereBuilder<TestUser>().EqualTo(x => x.Id, 42);
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.Name, "Charlie")
            .Where(wb)
            .Build();

        result.Sql.Should().Be("UPDATE [Users] SET [Name] = @pu0 WHERE [Id] = @pw0");
        result.Parameters["pu0"].Should().Be("Charlie");
        result.Parameters["pw0"].Should().Be(42);
    }

    [Fact]
    public void Where_InlineAction_AppendsWhereClause()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.IsActive, false)
            .Where(w => w.EqualTo(x => x.Id, 7))
            .Build();

        result.Sql.Should().Be("UPDATE [Users] SET [IsActive] = @pu0 WHERE [Id] = @pw0");
    }

    [Fact]
    public void Where_LambdaExpression_AppendsWhereClause()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.Name, "Delta")
            .Where(x => x.Age > 18)
            .Build();

        result.Sql.Should().Contain("WHERE");
        result.Sql.Should().Contain("[Age]");
    }

    [Fact]
    public void Where_ValiFlow_AppendsWhereClause()
    {
        var filter = new ValiFlow<TestUser>().EqualTo(x => x.IsActive, true);
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.Name, "Filtered")
            .Where(filter)
            .Build();

        result.Sql.Should().Contain("WHERE");
        result.Sql.Should().Contain("[IsActive]");
    }

    // ── Table + schema ────────────────────────────────────────────────────────

    [Fact]
    public void Table_WithSchema_GeneratesSchemaQualifiedName()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users", "dbo")
            .Set(x => x.Name, "Schema")
            .AllowUpdateAll()
            .Build();

        result.Sql.Should().StartWith("UPDATE [dbo].[Users]");
    }

    [Fact]
    public void Table_NotCalled_UsesTypeName()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Set(x => x.Name, "Default")
            .AllowUpdateAll()
            .Build();

        result.Sql.Should().StartWith("UPDATE [TestUser]");
    }

    // ── Dialects ──────────────────────────────────────────────────────────────

    [Fact]
    public void PostgreSql_UsesDoubleQuotes()
    {
        var result = new SqlUpdateBuilder<TestUser>(Pg)
            .Table("Users")
            .Set(x => x.Name, "PG")
            .Where(w => w.EqualTo(x => x.Id, 1))
            .Build();

        result.Sql.Should().StartWith("UPDATE \"Users\"");
        result.Sql.Should().Contain("\"Name\"");
    }

    [Fact]
    public void MySql_UsesBackticks()
    {
        var result = new SqlUpdateBuilder<TestUser>(My)
            .Table("Users")
            .Set(x => x.Name, "MySQL")
            .AllowUpdateAll()
            .Build();

        result.Sql.Should().StartWith("UPDATE `Users`");
    }

    // ── Tag ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Tag_PrependsSqlComment()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.IsActive, false)
            .AllowUpdateAll()
            .Tag("Deactivate user")
            .Build();

        result.Sql.Should().StartWith("-- Deactivate user\n");
        result.Sql.Should().Contain("UPDATE [Users]");
    }

    // ── OutputUpdated / Returning ─────────────────────────────────────────────

    [Fact]
    public void OutputUpdated_AppendsOutputAfterSet()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.IsActive, false)
            .OutputUpdated()
            .Where(w => w.EqualTo(x => x.Id, 1))
            .Build();

        // SQL Server: UPDATE [Users] SET [IsActive] = @pu0 OUTPUT INSERTED.* WHERE [Id] = @pw0
        result.Sql.Should().Contain("OUTPUT INSERTED.*");
        result.Sql.Should().MatchRegex(@"SET .+ OUTPUT INSERTED\.\*");
    }

    [Fact]
    public void Returning_NoColumns_AppendsReturningAll()
    {
        var result = new SqlUpdateBuilder<TestUser>(new SqliteDialect())
            .Table("Users")
            .Set(x => x.Name, "Updated")
            .Where(w => w.EqualTo(x => x.Id, 5))
            .Returning()
            .Build();

        result.Sql.Should().EndWith("RETURNING *");
    }

    [Fact]
    public void Returning_SpecificColumns_AppendsColumnList()
    {
        var result = new SqlUpdateBuilder<TestUser>(new PostgreSqlDialect())
            .Table("Users")
            .Set(x => x.Name, "PG")
            .AllowUpdateAll()
            .Returning(x => x.Id, x => x.Name)
            .Build();

        result.Sql.Should().EndWith("RETURNING \"Id\", \"Name\"");
    }

    [Fact]
    public void OutputUpdated_NonSqlServer_ThrowsInvalidOperationException()
    {
        var builder = new SqlUpdateBuilder<TestUser>(new PostgreSqlDialect())
            .Table("Users")
            .Set(x => x.Name, "X")
            .Where(w => w.EqualTo(x => x.Id, 1))
            .OutputUpdated();

        builder.Invoking(b => b.Build()).Should().Throw<InvalidOperationException>()
            .WithMessage("*SqlServerDialect*");
    }

    [Fact]
    public void Returning_SqlServer_ThrowsInvalidOperationException()
    {
        var builder = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.Name, "X")
            .Where(w => w.EqualTo(x => x.Id, 1))
            .Returning();

        builder.Invoking(b => b.Build()).Should().Throw<InvalidOperationException>()
            .WithMessage("*SqlServer*");
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public void Build_NoAssignments_ThrowsInvalidOperationException()
    {
        var builder = new SqlUpdateBuilder<TestUser>(Sql).Table("Users");
        builder.Invoking(b => b.Build()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_NullDialect_ThrowsArgumentNullException()
    {
        var act = () => new SqlUpdateBuilder<TestUser>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── SetRaw ────────────────────────────────────────────────────────────────

    [Fact]
    public void SetRaw_GeneratesRawExpressionInSet()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .SetRaw(x => x.Counter, "Counter + 1")
            .Where(w => w.EqualTo(x => x.Id, 1))
            .Build();

        result.Sql.Should().Contain("SET [Counter] = Counter + 1");
        result.Sql.Should().Contain("WHERE");
    }

    [Fact]
    public void SetRaw_NullExpression_ThrowsArgumentException()
    {
        var builder = new SqlUpdateBuilder<TestUser>(Sql).Table("Users");
        builder.Invoking(b => b.SetRaw(null!, "GETDATE()"))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetColumn_GeneratesColumnCopyInSet()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .SetColumn(x => x.NameBackup, x => x.Name)
            .AllowUpdateAll()
            .Build();

        result.Sql.Should().Contain("SET [NameBackup] = [Name]");
    }

    [Fact]
    public void SetRaw_CombinedWithSet_BothInSetClause()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.Name, "Alice")
            .SetRaw(x => x.UpdatedAt, "GETDATE()")
            .AllowUpdateAll()
            .Build();

        result.Sql.Should().Contain("[Name] = @pu0");
        result.Sql.Should().Contain("[UpdatedAt] = GETDATE()");
        result.Parameters["pu0"].Should().Be("Alice");
    }

    // ── UPDATE FROM / JOIN ────────────────────────────────────────────────────

    [Fact]
    public void FromTable_SqlServer_GeneratesUpdateFromSyntax()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.Name, "Updated")
            .FromTable("Staging")
            .AllowUpdateAll()
            .Build();

        result.Sql.Should().Contain("UPDATE [Users]");
        result.Sql.Should().Contain("SET [Name] = @pu0");
        result.Sql.Should().Contain("FROM [Staging]");
    }

    [Fact]
    public void FromTable_PostgreSql_GeneratesUpdateFromSyntax()
    {
        var result = new SqlUpdateBuilder<TestUser>(Pg)
            .Table("Users")
            .Set(x => x.Name, "Updated")
            .FromTable("Staging")
            .Where(w => w.EqualTo(x => x.Id, 1))
            .Build();

        result.Sql.Should().Contain("UPDATE \"Users\"");
        result.Sql.Should().Contain("FROM \"Staging\"");
        result.Sql.Should().Contain("WHERE");
    }

    [Fact]
    public void FromTable_MySql_GeneratesUpdateJoinSyntax()
    {
        var result = new SqlUpdateBuilder<TestUser>(My)
            .Table("Users")
            .Set(x => x.Name, "Updated")
            .FromTable("Staging", "s")
            .JoinOn("INNER JOIN", "`Staging` ON `Users`.`Id` = `Staging`.`Id`")
            .AllowUpdateAll()
            .Build();

        result.Sql.Should().Contain("UPDATE `Users`");
        // MySQL: JOIN appears before SET
        result.Sql.Should().Contain("SET `Name`");
        // JOIN should appear before SET in the SQL string
        var joinIndex = result.Sql.IndexOf("INNER JOIN", StringComparison.Ordinal);
        var setIndex = result.Sql.IndexOf("SET", StringComparison.Ordinal);
        joinIndex.Should().BeLessThan(setIndex);
    }

    [Fact]
    public void FromTable_WithAlias_IncludesAlias()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.Name, "Updated")
            .FromTable("Staging", "s")
            .AllowUpdateAll()
            .Build();

        result.Sql.Should().Contain("FROM [Staging] [s]");
    }

    [Fact]
    public void FromTable_WithJoinOn_AppendsJoinClause()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.Name, "Updated")
            .FromTable("Staging", "s")
            .JoinOn("INNER JOIN", "[s].[Id] = [Users].[Id]")
            .AllowUpdateAll()
            .Build();

        result.Sql.Should().Contain("FROM [Staging] [s]");
        result.Sql.Should().Contain("INNER JOIN [s].[Id] = [Users].[Id]");
    }

    [Fact]
    public void FromTable_CombinedWithWhereClause_BothPresent()
    {
        var result = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.IsActive, false)
            .FromTable("Inactive")
            .Where(w => w.EqualTo(x => x.Id, 99))
            .Build();

        result.Sql.Should().Contain("FROM [Inactive]");
        result.Sql.Should().Contain("WHERE [Id] = @pw0");
    }

    [Fact]
    public void FromTable_SQLite_ThrowsInvalidOperationException()
    {
        var builder = new SqlUpdateBuilder<TestUser>(new SqliteDialect())
            .Table("Users")
            .Set(x => x.Name, "X");

        builder.Invoking(b => b.FromTable("Other"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*SQLite*");
    }

    [Fact]
    public void FromTable_Oracle_ThrowsInvalidOperationException()
    {
        var builder = new SqlUpdateBuilder<TestUser>(new OracleDialect())
            .Table("Users")
            .Set(x => x.Name, "X");

        builder.Invoking(b => b.FromTable("Other"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Oracle*");
    }

    [Fact]
    public void JoinOn_EmptyJoinType_ThrowsArgumentException()
    {
        var builder = new SqlUpdateBuilder<TestUser>(Sql)
            .Table("Users")
            .Set(x => x.Name, "X")
            .FromTable("Staging");

        builder.Invoking(b => b.JoinOn("", "condition"))
            .Should().Throw<ArgumentException>();
    }
}
