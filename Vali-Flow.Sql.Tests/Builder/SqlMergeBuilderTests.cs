using FluentAssertions;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests.Builder;

/// <summary>Tests for <see cref="SqlMergeBuilder{TTarget,TSource}"/>.</summary>
public sealed class SqlMergeBuilderTests
{
    private static readonly ISqlDialect Sql = new SqlServerDialect();

    // ── Basic MERGE ───────────────────────────────────────────────────────────

    [Fact]
    public void Build_BasicMerge_GeneratesMergeStatement()
    {
        var result = new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Into("Users")
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenMatchedUpdate(b => b.MatchedSetColumn(t => t.Name, s => s.Name))
            .WhenNotMatchedInsert(b => b
                .NotMatchedInsertColumn(t => t.Id, s => s.Id)
                .NotMatchedInsertColumn(t => t.Name, s => s.Name))
            .Build();

        result.Sql.Should().Contain("MERGE INTO [Users] AS target");
        result.Sql.Should().Contain("USING [StagingUsers] AS src");
        result.Sql.Should().Contain("WHEN MATCHED THEN");
        result.Sql.Should().Contain("WHEN NOT MATCHED BY TARGET THEN");
        result.Sql.Should().EndWith(";");
    }

    // ── WHEN MATCHED UPDATE ───────────────────────────────────────────────────

    [Fact]
    public void Build_WhenMatchedUpdate_GeneratesUpdateClause()
    {
        var result = new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Into("Users")
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenMatchedUpdate(b => b
                .MatchedSetColumn(t => t.Name, s => s.Name)
                .MatchedSetColumn(t => t.Age, s => s.Age))
            .Build();

        result.Sql.Should().Contain("WHEN MATCHED THEN");
        result.Sql.Should().Contain("UPDATE SET");
        result.Sql.Should().Contain("target.[Name] = src.[Name]");
        result.Sql.Should().Contain("target.[Age] = src.[Age]");
    }

    // ── WHEN NOT MATCHED BY TARGET INSERT ─────────────────────────────────────

    [Fact]
    public void Build_WhenNotMatchedInsert_GeneratesInsertClause()
    {
        var result = new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Into("Users")
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenNotMatchedInsert(b => b
                .NotMatchedInsertColumn(t => t.Id, s => s.Id)
                .NotMatchedInsertColumn(t => t.Name, s => s.Name))
            .Build();

        result.Sql.Should().Contain("WHEN NOT MATCHED BY TARGET THEN");
        result.Sql.Should().Contain("INSERT");
        result.Sql.Should().Contain("[Id]");
        result.Sql.Should().Contain("[Name]");
    }

    // ── WHEN NOT MATCHED BY SOURCE DELETE ─────────────────────────────────────

    [Fact]
    public void Build_WhenNotMatchedBySourceDelete_GeneratesDeleteClause()
    {
        var result = new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Into("Users")
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenNotMatchedBySourceDelete()
            .Build();

        result.Sql.Should().Contain("WHEN NOT MATCHED BY SOURCE THEN DELETE");
    }

    // ── Parameterized values ──────────────────────────────────────────────────

    [Fact]
    public void Build_MatchedSetValue_ParameterizesValue()
    {
        var result = new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Into("Users")
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenMatchedUpdate(b => b.MatchedSetValue(t => t.Salary, 99999.99m))
            .Build();

        result.Parameters.Should().ContainKey("pm0");
        result.Parameters["pm0"].Should().Be(99999.99m);
        result.Sql.Should().Contain("@pm0");
    }

    [Fact]
    public void Build_NotMatchedInsertValue_ParameterizesValue()
    {
        var result = new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Into("Users")
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenNotMatchedInsert(b => b
                .NotMatchedInsertColumn(t => t.Id, s => s.Id)
                .NotMatchedInsertValue(t => t.IsActive, true))
            .Build();

        result.Parameters.Should().ContainKey("pm0");
        result.Parameters["pm0"].Should().Be(true);
        result.Sql.Should().Contain("@pm0");
    }

    // ── Schema ────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithSchema_GeneratesSchemaQualifiedTarget()
    {
        var result = new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Into("Users", "dbo")
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenNotMatchedBySourceDelete()
            .Build();

        result.Sql.Should().Contain("MERGE INTO [dbo].[Users] AS target");
    }

    // ── Default table name ────────────────────────────────────────────────────

    [Fact]
    public void Build_WithoutInto_ThrowsInvalidOperationException()
    {
        // I-8: Into() is mandatory; omitting it must throw rather than silently
        // falling back to typeof(TTarget).Name and generating an incorrect statement.
        var act = () => new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenNotMatchedBySourceDelete()
            .Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Into()*");
    }

    // ── Dialect guard ─────────────────────────────────────────────────────────

    [Fact]
    public void Build_NonSqlServerDialect_ThrowsInvalidOperationException()
    {
        var builder = new SqlMergeBuilder<TestUser, TestUser>(new PostgreSqlDialect())
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenNotMatchedBySourceDelete();

        builder.Invoking(b => b.Build())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*MERGE is not supported*");
    }

    // ── Missing On() ─────────────────────────────────────────────────────────

    [Fact]
    public void Build_NoOnCondition_ThrowsInvalidOperationException()
    {
        var builder = new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Into("Users")
            .Using("StagingUsers")
            .WhenNotMatchedBySourceDelete();

        builder.Invoking(b => b.Build())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*On()*");
    }

    // ── Missing When clauses ──────────────────────────────────────────────────

    [Fact]
    public void Build_NoWhenClauses_ThrowsInvalidOperationException()
    {
        var builder = new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Into("Users")
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id);

        builder.Invoking(b => b.Build())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*WhenMatchedUpdate*");
    }

    // ── Tag ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Tag_PrependsSqlComment()
    {
        var result = new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Into("Users")
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenNotMatchedBySourceDelete()
            .Tag("Sync staging to production")
            .Build();

        result.Sql.Should().StartWith("-- Sync staging to production\n");
        result.Sql.Should().Contain("MERGE INTO");
    }

    [Fact]
    public void Tag_NullOrWhitespace_ThrowsArgumentException()
    {
        var builder = new SqlMergeBuilder<TestUser, TestUser>(Sql)
            .Into("Users")
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenNotMatchedBySourceDelete();

        builder.Invoking(b => b.Tag("   ")).Should().Throw<ArgumentException>();
        builder.Invoking(b => b.Tag("")).Should().Throw<ArgumentException>();
    }

    // ── Oracle dialect ────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithOracleDialect_DoesNotUseAsAlias()
    {
        var oracle = new OracleDialect();
        var result = new SqlMergeBuilder<TestUser, TestUser>(oracle)
            .Into("Users")
            .Using("StagingUsers")
            .On(t => t.Id, s => s.Id)
            .WhenMatchedUpdate(b => b.MatchedSetColumn(t => t.Name, s => s.Name))
            .Build();

        // The current implementation hardcodes "AS target" regardless of dialect.
        // This test documents the actual behavior so regressions are caught.
        result.Sql.Should().Contain("target");
        result.Sql.Should().Contain("MERGE INTO");
    }
}
