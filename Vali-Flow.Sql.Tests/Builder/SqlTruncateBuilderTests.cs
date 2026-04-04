using FluentAssertions;
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
}
