using System.Diagnostics;
using FluentAssertions;
using Vali_Flow.Abstractions.Diagnostics;
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

    [Fact]
    public void From_UnsafeTableName_ThrowsArgumentException()
    {
        var act = () => new SqlDeleteBuilder<TestUser>(Sql).From("Users]; DROP TABLE Users;--");

        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
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

    [Fact]
    public void Where_LambdaExpression_CalledTwice_ThrowsInvalidOperationException()
    {
        // Previously this silently overwrote the first predicate with the second one
        // with no error — inconsistent with the SqlWhereBuilder overload (Where(SqlWhereBuilder<T>)),
        // which already guards against being set twice.
        var builder = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .Where(x => x.Age < 18);

        builder.Invoking(b => b.Where(x => x.Age > 65))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Where_ValiFlow_CalledTwice_ThrowsInvalidOperationException()
    {
        var filter1 = new ValiFlow<TestUser>().EqualTo(x => x.Department, "Sales");
        var filter2 = new ValiFlow<TestUser>().EqualTo(x => x.Department, "IT");
        var builder = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .Where(filter1);

        builder.Invoking(b => b.Where(filter2))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Where_SqlWhereBuilder_CalledTwice_ThrowsInvalidOperationException()
    {
        var builder = new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .Where(w => w.EqualTo(x => x.Id, 1));

        builder.Invoking(b => b.Where(w => w.EqualTo(x => x.Id, 2)))
            .Should().Throw<InvalidOperationException>();
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

    // ── Observability ─────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithTag_RecordsActivityWithTagAndEntityType()
    {
        Activity? seen = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ValiFlowDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => seen = activity
        };
        ActivitySource.AddActivityListener(listener);

        new SqlDeleteBuilder<TestUser>(Sql)
            .From("Users")
            .AllowDeleteAll()
            .Tag("Delete all users")
            .Build();

        seen.Should().NotBeNull();
        seen!.GetTagItem("vali_flow.tag").Should().Be("Delete all users");
        seen.GetTagItem("vali_flow.entity_type").Should().Be("TestUser");
        seen.GetTagItem("vali_flow.has_where").Should().Be(false);
        seen.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Fact]
    public void Build_WithoutWhereOrAllowDeleteAll_RecordsErrorStatus()
    {
        Activity? seen = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ValiFlowDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => seen = activity
        };
        ActivitySource.AddActivityListener(listener);

        // No Where() and no AllowDeleteAll() triggers the existing safety guard in Build().
        var act = () => new SqlDeleteBuilder<TestUser>(Sql).From("Users").Build();

        act.Should().Throw<InvalidOperationException>();
        seen.Should().NotBeNull();
        seen!.Status.Should().Be(ActivityStatusCode.Error);
    }
}
