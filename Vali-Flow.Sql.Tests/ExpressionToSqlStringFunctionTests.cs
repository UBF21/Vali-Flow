using FluentAssertions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;
using Xunit;
using System.Linq.Expressions;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for string function translation in ExpressionToSqlVisitor.</summary>
public sealed class ExpressionToSqlStringFunctionTests
{
    private static readonly ISqlDialect SqlServer  = new SqlServerDialect();
    private static readonly ISqlDialect Postgres   = new PostgreSqlDialect();
    private static readonly ISqlDialect MySql      = new MySqlDialect();
    private static readonly ISqlDialect Sqlite     = new SqliteDialect();

    private static SqlResult Translate(Expression<Func<TestUser, bool>> expr, ISqlDialect dialect)
        => ExpressionToSqlVisitor.Translate(expr, dialect);

    // ── Substring ─────────────────────────────────────────────────────────────

    [Fact]
    public void Substring_SqlServer_EmitsSUBSTRING_1Based()
    {
        var result = Translate(x => x.Name.Substring(1, 3) == "lic", SqlServer);
        result.Sql.Should().Be("SUBSTRING([Name], 2, 3) = @p0");
        result.Parameters["p0"].Should().Be("lic");
    }

    [Fact]
    public void Substring_PostgreSQL_EmitsSUBSTRING()
    {
        var result = Translate(x => x.Name.Substring(0, 4) == "Alic", Postgres);
        result.Sql.Should().Be("SUBSTRING(\"Name\", 1, 4) = @p0");
        result.Parameters["p0"].Should().Be("Alic");
    }

    [Fact]
    public void Substring_MySQL_EmitsSUBSTRING()
    {
        var result = Translate(x => x.Name.Substring(0, 4) == "Alic", MySql);
        result.Sql.Should().Be("SUBSTRING(`Name`, 1, 4) = @p0");
        result.Parameters["p0"].Should().Be("Alic");
    }

    [Fact]
    public void Substring_SQLite_EmitsSUBSTR()
    {
        var result = Translate(x => x.Name.Substring(0, 4) == "Alic", Sqlite);
        result.Sql.Should().Be("SUBSTR(\"Name\", 1, 4) = @p0");
        result.Parameters["p0"].Should().Be("Alic");
    }

    // ── Trim ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Trim_SqlServer_EmitsLTRIM_RTRIM()
    {
        var result = Translate(x => x.Name.Trim() == "Alice", SqlServer);
        result.Sql.Should().Be("LTRIM(RTRIM([Name])) = @p0");
        result.Parameters["p0"].Should().Be("Alice");
    }

    [Fact]
    public void Trim_PostgreSQL_EmitsTRIM()
    {
        var result = Translate(x => x.Name.Trim() == "Alice", Postgres);
        result.Sql.Should().Be("TRIM(\"Name\") = @p0");
    }

    [Fact]
    public void Trim_MySQL_EmitsTRIM()
    {
        var result = Translate(x => x.Name.Trim() == "Alice", MySql);
        result.Sql.Should().Be("TRIM(`Name`) = @p0");
    }

    [Fact]
    public void Trim_SQLite_EmitsTRIM()
    {
        var result = Translate(x => x.Name.Trim() == "Alice", Sqlite);
        result.Sql.Should().Be("TRIM(\"Name\") = @p0");
    }

    // ── TrimStart ─────────────────────────────────────────────────────────────

    [Fact]
    public void TrimStart_AllDialects_EmitLTRIM()
    {
        var result = Translate(x => x.Name.TrimStart() == "Alice", SqlServer);
        result.Sql.Should().Be("LTRIM([Name]) = @p0");
    }

    [Fact]
    public void TrimStart_PostgreSQL_EmitsLTRIM()
    {
        var result = Translate(x => x.Name.TrimStart() == "Alice", Postgres);
        result.Sql.Should().Be("LTRIM(\"Name\") = @p0");
    }

    [Fact]
    public void TrimStart_MySQL_EmitsLTRIM()
    {
        var result = Translate(x => x.Name.TrimStart() == "Alice", MySql);
        result.Sql.Should().Be("LTRIM(`Name`) = @p0");
    }

    [Fact]
    public void TrimStart_SQLite_EmitsLTRIM()
    {
        var result = Translate(x => x.Name.TrimStart() == "Alice", Sqlite);
        result.Sql.Should().Be("LTRIM(\"Name\") = @p0");
    }

    // ── TrimEnd ───────────────────────────────────────────────────────────────

    [Fact]
    public void TrimEnd_AllDialects_EmitRTRIM()
    {
        var result = Translate(x => x.Name.TrimEnd() == "Alice", SqlServer);
        result.Sql.Should().Be("RTRIM([Name]) = @p0");
    }

    [Fact]
    public void TrimEnd_PostgreSQL_EmitsRTRIM()
    {
        var result = Translate(x => x.Name.TrimEnd() == "Alice", Postgres);
        result.Sql.Should().Be("RTRIM(\"Name\") = @p0");
    }

    // ── Length ────────────────────────────────────────────────────────────────

    [Fact]
    public void Length_SqlServer_EmitsLEN()
    {
        var result = Translate(x => x.Name.Length > 5, SqlServer);
        result.Sql.Should().Be("LEN([Name]) > @p0");
        result.Parameters["p0"].Should().Be(5);
    }

    [Fact]
    public void Length_PostgreSQL_EmitsLENGTH()
    {
        var result = Translate(x => x.Name.Length > 5, Postgres);
        result.Sql.Should().Be("LENGTH(\"Name\") > @p0");
    }

    [Fact]
    public void Length_MySQL_EmitsLENGTH()
    {
        var result = Translate(x => x.Name.Length > 5, MySql);
        result.Sql.Should().Be("LENGTH(`Name`) > @p0");
    }

    [Fact]
    public void Length_SQLite_EmitsLENGTH()
    {
        var result = Translate(x => x.Name.Length > 5, Sqlite);
        result.Sql.Should().Be("LENGTH(\"Name\") > @p0");
    }

    // ── Replace ───────────────────────────────────────────────────────────────

    [Fact]
    public void Replace_SqlServer_EmitsREPLACE_WithParams()
    {
        var result = Translate(x => x.Name.Replace("a", "e") == "Alice", SqlServer);
        result.Sql.Should().Be("REPLACE([Name], @p0, @p1) = @p2");
        result.Parameters["p0"].Should().Be("a");
        result.Parameters["p1"].Should().Be("e");
        result.Parameters["p2"].Should().Be("Alice");
    }

    [Fact]
    public void Replace_PostgreSQL_EmitsREPLACE()
    {
        var result = Translate(x => x.Name.Replace("a", "e") == "Alice", Postgres);
        result.Sql.Should().Be("REPLACE(\"Name\", @p0, @p1) = @p2");
    }

    [Fact]
    public void Replace_MySQL_EmitsREPLACE()
    {
        var result = Translate(x => x.Name.Replace("a", "e") == "Alice", MySql);
        result.Sql.Should().Be("REPLACE(`Name`, @p0, @p1) = @p2");
    }

    [Fact]
    public void Replace_SQLite_EmitsREPLACE()
    {
        var result = Translate(x => x.Name.Replace("a", "e") == "Alice", Sqlite);
        result.Sql.Should().Be("REPLACE(\"Name\", @p0, @p1) = @p2");
    }

    // ── IndexOf ───────────────────────────────────────────────────────────────

    [Fact]
    public void IndexOf_SqlServer_EmitsCHARINDEX_Minus1()
    {
        var result = Translate(x => x.Name.IndexOf("li") >= 0, SqlServer);
        result.Sql.Should().Be("CHARINDEX(@p0, [Name]) - 1 >= @p1");
        result.Parameters["p0"].Should().Be("li");
        result.Parameters["p1"].Should().Be(0);
    }

    [Fact]
    public void IndexOf_PostgreSQL_EmitsPOSITION_Minus1()
    {
        var result = Translate(x => x.Name.IndexOf("li") >= 0, Postgres);
        result.Sql.Should().Be("POSITION(@p0 IN \"Name\") - 1 >= @p1");
    }

    [Fact]
    public void IndexOf_MySQL_EmitsLOCATE_Minus1()
    {
        var result = Translate(x => x.Name.IndexOf("li") >= 0, MySql);
        result.Sql.Should().Be("LOCATE(@p0, `Name`) - 1 >= @p1");
    }

    [Fact]
    public void IndexOf_SQLite_EmitsINSTR_Minus1()
    {
        var result = Translate(x => x.Name.IndexOf("li") >= 0, Sqlite);
        result.Sql.Should().Be("INSTR(\"Name\", @p0) - 1 >= @p1");
    }
}
