using FluentAssertions;
using System.Linq.Expressions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for string.IsNullOrEmpty and string.IsNullOrWhiteSpace translation in ExpressionToSqlVisitor.</summary>
public sealed class ExpressionToSqlIsNullOrEmptyTests
{
    private static readonly ISqlDialect SqlServer = new SqlServerDialect();
    private static readonly ISqlDialect Postgres  = new PostgreSqlDialect();
    private static readonly ISqlDialect MySql     = new MySqlDialect();
    private static readonly ISqlDialect Sqlite    = new SqliteDialect();

    private static SqlResult Translate(Expression<Func<TestUser, bool>> expr, ISqlDialect dialect)
        => ExpressionToSqlVisitor.Translate(expr, dialect);

    // ── string.IsNullOrEmpty ──────────────────────────────────────────────────

    [Fact]
    public void IsNullOrEmpty_SqlServer_EmitsNullOrEmptyCheck()
    {
        var result = Translate(x => string.IsNullOrEmpty(x.Name), SqlServer);
        result.Sql.Should().Be("([Name] IS NULL OR [Name] = '')");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void IsNullOrEmpty_PostgreSQL_EmitsNullOrEmptyCheck()
    {
        var result = Translate(x => string.IsNullOrEmpty(x.Name), Postgres);
        result.Sql.Should().Be("(\"Name\" IS NULL OR \"Name\" = '')");
    }

    [Fact]
    public void IsNullOrEmpty_MySQL_EmitsNullOrEmptyCheck()
    {
        var result = Translate(x => string.IsNullOrEmpty(x.Name), MySql);
        result.Sql.Should().Be("(`Name` IS NULL OR `Name` = '')");
    }

    [Fact]
    public void IsNullOrEmpty_SQLite_EmitsNullOrEmptyCheck()
    {
        var result = Translate(x => string.IsNullOrEmpty(x.Name), Sqlite);
        result.Sql.Should().Be("(\"Name\" IS NULL OR \"Name\" = '')");
    }

    // ── string.IsNullOrEmpty negated ──────────────────────────────────────────

    [Fact]
    public void IsNullOrEmpty_Negated_SqlServer_EmitsNotWrapped()
    {
        var result = Translate(x => !string.IsNullOrEmpty(x.Name), SqlServer);
        result.Sql.Should().Be("NOT (([Name] IS NULL OR [Name] = ''))");
    }

    // ── string.IsNullOrWhiteSpace ─────────────────────────────────────────────

    [Fact]
    public void IsNullOrWhiteSpace_SqlServer_EmitsLtrimRtrimCheck()
    {
        var result = Translate(x => string.IsNullOrWhiteSpace(x.Name), SqlServer);
        result.Sql.Should().Be("([Name] IS NULL OR LTRIM(RTRIM([Name])) = '')");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void IsNullOrWhiteSpace_PostgreSQL_EmitsTrimCheck()
    {
        var result = Translate(x => string.IsNullOrWhiteSpace(x.Name), Postgres);
        result.Sql.Should().Be("(\"Name\" IS NULL OR TRIM(\"Name\") = '')");
    }

    [Fact]
    public void IsNullOrWhiteSpace_MySQL_EmitsTrimCheck()
    {
        var result = Translate(x => string.IsNullOrWhiteSpace(x.Name), MySql);
        result.Sql.Should().Be("(`Name` IS NULL OR TRIM(`Name`) = '')");
    }

    [Fact]
    public void IsNullOrWhiteSpace_SQLite_EmitsLtrimRtrimCheck()
    {
        var result = Translate(x => string.IsNullOrWhiteSpace(x.Name), Sqlite);
        result.Sql.Should().Be("(\"Name\" IS NULL OR LTRIM(RTRIM(\"Name\")) = '')");
    }
}
