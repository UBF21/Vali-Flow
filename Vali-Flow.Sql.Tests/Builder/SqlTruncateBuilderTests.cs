using System.Diagnostics;
using FluentAssertions;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests.Builder;

/// <summary>Tests for <see cref="SqlTruncateBuilder{T}"/>.</summary>
public sealed class SqlTruncateBuilderTests
{
    private static readonly ISqlDialect Sql = new SqlServerDialect();
    private static readonly ISqlDialect Pg = new PostgreSqlDialect();
    private static readonly ISqlDialect My = new MySqlDialect();
    private static readonly ISqlDialect Sqlite = new SqliteDialect();

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

    // ── Basic TRUNCATE ────────────────────────────────────────────────────────

    [Fact]
    public void Table_SqlServer_GeneratesTruncate()
    {
        var result = new SqlTruncateBuilder<TestUser>(Sql)
            .Table("Users")
            .Build();

        result.Sql.Should().Be("TRUNCATE TABLE [Users]");
    }

    [Fact]
    public void Table_UnsafeTableName_ThrowsArgumentException()
    {
        var act = () => new SqlTruncateBuilder<TestUser>(Sql).Table("Users]; DROP TABLE Users;--");

        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    [Fact]
    public void Table_WithSchema_GeneratesSchemaQualified()
    {
        var result = new SqlTruncateBuilder<TestUser>(Sql)
            .Table("Users", "dbo")
            .Build();

        result.Sql.Should().Be("TRUNCATE TABLE [dbo].[Users]");
    }

    [Fact]
    public void NoTable_UsesTypeName()
    {
        var result = new SqlTruncateBuilder<TestUser>(Sql)
            .Build();

        result.Sql.Should().Be("TRUNCATE TABLE [TestUser]");
    }

    // ── Dialects ──────────────────────────────────────────────────────────────

    [Fact]
    public void PostgreSQL_UsesDoubleQuotes()
    {
        var result = new SqlTruncateBuilder<TestUser>(Pg)
            .Table("Users")
            .Build();

        result.Sql.Should().Be("TRUNCATE TABLE \"Users\"");
    }

    [Fact]
    public void MySQL_UsesBackticks()
    {
        var result = new SqlTruncateBuilder<TestUser>(My)
            .Table("Users")
            .Build();

        result.Sql.Should().Be("TRUNCATE TABLE `Users`");
    }

    [Fact]
    public void SQLite_ThrowsOnTruncate()
    {
        var act = () => new SqlTruncateBuilder<TestUser>(Sqlite)
            .Table("Users")
            .Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SQLite*does not support TRUNCATE TABLE*");
    }

    // ── Tag ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Tag_PrependsSqlComment()
    {
        var result = new SqlTruncateBuilder<TestUser>(Sql)
            .Table("Users")
            .Tag("Clear all users")
            .Build();

        result.Sql.Should().StartWith("-- Clear all users\n");
        result.Sql.Should().Contain("TRUNCATE TABLE [Users]");
    }

    [Fact]
    public void Tag_NullOrWhitespace_ThrowsArgumentException()
    {
        var builder = new SqlTruncateBuilder<TestUser>(Sql).Table("Users");
        builder.Invoking(b => b.Tag("  ")).Should().Throw<ArgumentException>();
        builder.Invoking(b => b.Tag("")).Should().Throw<ArgumentException>();
    }

    // ── Parameters ────────────────────────────────────────────────────────────

    [Fact]
    public void Build_ReturnsEmptyParameters()
    {
        var result = new SqlTruncateBuilder<TestUser>(Sql)
            .Table("Users")
            .Build();

        result.Parameters.Should().BeEmpty();
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullDialect_ThrowsArgumentNullException()
    {
        var act = () => new SqlTruncateBuilder<TestUser>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── Observability ─────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithTag_RecordsActivityWithTagAndEntityType()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;

        new SqlTruncateBuilder<TestUser>(Sql)
            .Table("Users")
            .Tag("Truncate users")
            .Build();

        var seen = GetOwn(captured, root);
        seen.GetTagItem("vali_flow.tag").Should().Be("Truncate users");
        seen.GetTagItem("vali_flow.entity_type").Should().Be("TestUser");
        seen.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Fact]
    public void Build_UnsupportedDialect_RecordsErrorStatus()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;

        // SQLite does not support TRUNCATE TABLE, triggering the existing guard in Build().
        var act = () => new SqlTruncateBuilder<TestUser>(Sqlite).Table("Users").Build();

        act.Should().Throw<InvalidOperationException>();
        GetOwn(captured, root).Status.Should().Be(ActivityStatusCode.Error);
    }
}
