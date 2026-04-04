using FluentAssertions;
using Vali_Flow.Core.Builder;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests.Builder;

/// <summary>Tests for <see cref="SqlDeleteBuilder{T}"/>.</summary>
public sealed class SqlDeleteBuilderTests
{
    private static readonly ISqlDialect Sql = new SqlServerDialect();
    private static readonly ISqlDialect Pg = new PostgreSqlDialect();
    private static readonly ISqlDialect My = new MySqlDialect();

    // ── Basic DELETE ──────────────────────────────────────────────────────────

    [Fact]
    public void NoWhere_GeneratesDeleteWithoutWhere()
    {
        var result = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .AllowDeleteAll()
            .Build();

        result.Sql.Should().Be("DELETE FROM [Users]");
        result.Parameters.Should().BeEmpty();
    }

    // ── WHERE variants ────────────────────────────────────────────────────────

    [Fact]
    public void Where_WithSqlWhereBuilder_AppendsWhereClause()
    {
        var wb = new SqlWhereBuilder<TestUser>().EqualTo(x => x.Id, 99);
        var result = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .Where(wb)
            .Build();

        result.Sql.Should().Be("DELETE FROM [Users] WHERE [Id] = @pw0");
        result.Parameters["pw0"].Should().Be(99);
    }

    [Fact]
    public void Where_InlineAction_AppendsWhereClause()
    {
        var result = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .Where(w => w.EqualTo(x => x.Id, 5).And().EqualTo(x => x.IsActive, false))
            .Build();

        result.Sql.Should().Contain("DELETE FROM [Users] WHERE");
        result.Sql.Should().Contain("[Id] = @pw0");
        result.Sql.Should().Contain("[IsActive] = @pw1");
    }

    [Fact]
    public void Where_LambdaExpression_AppendsWhereClause()
    {
        var result = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .Where(x => x.Age < 18)
            .Build();

        result.Sql.Should().Contain("WHERE");
        result.Sql.Should().Contain("[Age]");
    }

    [Fact]
    public void Where_ValiFlow_AppendsWhereClause()
    {
        var filter = new ValiFlow<TestUser>().EqualTo(x => x.Department, "Sales");
        var result = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .Where(filter)
            .Build();

        result.Sql.Should().Contain("WHERE");
        result.Sql.Should().Contain("[Department]");
    }

    [Fact]
    public void Where_BothPredicateAndBuilder_AreAnded()
    {
        var result = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .Where(x => x.IsActive == false)
            .Where(w => w.LessThan(x => x.Age, 10))
            .Build();

        result.Sql.Should().Contain("AND");
    }

    // ── Table + schema ────────────────────────────────────────────────────────

    [Fact]
    public void From_WithSchema_GeneratesSchemaQualifiedName()
    {
        var result = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users", "dbo")
            .AllowDeleteAll()
            .Build();

        result.Sql.Should().Be("DELETE FROM [dbo].[Users]");
    }

    [Fact]
    public void From_NotCalled_UsesTypeName()
    {
        var result = new SqlDeleteBuilder<TestUser>(Sql).AllowDeleteAll().Build();
        result.Sql.Should().Be("DELETE FROM [TestUser]");
    }

    // ── Dialects ──────────────────────────────────────────────────────────────

    [Fact]
    public void PostgreSql_UsesDoubleQuotes()
    {
        var result = new SqlDeleteBuilder<TestUser>(Pg)
            .From("Users")
            .Where(w => w.EqualTo(x => x.Id, 1))
            .Build();

        result.Sql.Should().StartWith("DELETE FROM \"Users\"");
    }

    [Fact]
    public void MySql_UsesBackticks()
    {
        var result = new SqlDeleteBuilder<TestUser>(My)
            .From("Users")
            .AllowDeleteAll()
            .Build();

        result.Sql.Should().Be("DELETE FROM `Users`");
    }

    // ── Tag ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Tag_PrependsSqlComment()
    {
        var result = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .Where(w => w.EqualTo(x => x.Id, 1))
            .Tag("Delete inactive user")
            .Build();

        result.Sql.Should().StartWith("-- Delete inactive user\n");
        result.Sql.Should().Contain("DELETE FROM [Users]");
    }

    [Fact]
    public void Tag_NullOrWhitespace_ThrowsArgumentException()
    {
        var builder = new SqlDeleteBuilder<TestUser>(Sql).From("Users");
        builder.Invoking(b => b.Tag("  ")).Should().Throw<ArgumentException>();
    }

    // ── OutputDeleted / Returning ─────────────────────────────────────────────

    [Fact]
    public void OutputDeleted_AppendsOutputClause()
    {
        var result = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .OutputDeleted()
            .Where(w => w.EqualTo(x => x.Id, 1))
            .Build();

        // DELETE FROM [Users] OUTPUT DELETED.* WHERE [Id] = @pw0
        result.Sql.Should().Contain("OUTPUT DELETED.*");
        result.Sql.Should().MatchRegex(@"DELETE FROM .+ OUTPUT DELETED\.\*");
    }

    [Fact]
    public void Returning_NoColumns_AppendsReturningAll()
    {
        var result = new SqlDeleteBuilder<TestUser>(new SqliteDialect())
            .From("Users")
            .Where(w => w.EqualTo(x => x.Id, 3))
            .Returning()
            .Build();

        result.Sql.Should().EndWith("RETURNING *");
    }

    [Fact]
    public void Returning_SpecificColumns_AppendsColumnList()
    {
        var result = new SqlDeleteBuilder<TestUser>(new PostgreSqlDialect())
            .From("Users")
            .Where(w => w.EqualTo(x => x.Id, 3))
            .Returning(x => x.Id, x => x.Name)
            .Build();

        result.Sql.Should().EndWith("RETURNING \"Id\", \"Name\"");
    }

    [Fact]
    public void OutputDeleted_NonSqlServer_ThrowsInvalidOperationException()
    {
        var builder = new SqlDeleteBuilder<TestUser>(new PostgreSqlDialect())
            .From("Users")
            .Where(w => w.EqualTo(x => x.Id, 1))
            .OutputDeleted();

        builder.Invoking(b => b.Build()).Should().Throw<InvalidOperationException>()
            .WithMessage("*SqlServerDialect*");
    }

    [Fact]
    public void Returning_SqlServer_ThrowsInvalidOperationException()
    {
        var builder = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .Where(w => w.EqualTo(x => x.Id, 1))
            .Returning();

        builder.Invoking(b => b.Build()).Should().Throw<InvalidOperationException>()
            .WithMessage("*SqlServer*");
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullDialect_ThrowsArgumentNullException()
    {
        var act = () => new SqlDeleteBuilder<TestUser>(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
