using FluentAssertions;
using System.Linq.Expressions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;
using Vali_Flow.Sql.Tests.Models;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for ConditionalExpression (CASE WHEN) and Enum.HasFlag translation.</summary>
public sealed class ExpressionToSqlCaseWhenTests
{
    private static readonly ISqlDialect SqlServer = new SqlServerDialect();
    private static readonly ISqlDialect Postgres  = new PostgreSqlDialect();
    private static readonly ISqlDialect MySql     = new MySqlDialect();
    private static readonly ISqlDialect Sqlite    = new SqliteDialect();

    private static SqlResult Translate<T>(Expression<Func<T, bool>> expr, ISqlDialect dialect)
        => ExpressionToSqlVisitor.Translate(expr, dialect);

    // ── CASE WHEN — bool test, numeric branches ──────────────────────────────

    [Fact]
    public void CaseWhen_BoolColumn_ThenNumericColumn_SqlServer()
    {
        var result = Translate<TestOrder>(
            x => (x.IsShipped ? x.Quantity : 0) > 0,
            SqlServer);

        result.Sql.Should().Be("CASE WHEN [IsShipped] = 1 THEN [Quantity] ELSE @p0 END > @p1");
        result.Parameters["p0"].Should().Be(0);
        result.Parameters["p1"].Should().Be(0);
    }

    [Fact]
    public void CaseWhen_BoolColumn_ThenNumericColumn_PostgreSQL()
    {
        var result = Translate<TestOrder>(
            x => (x.IsShipped ? x.Quantity : 0) > 0,
            Postgres);

        result.Sql.Should().Be("CASE WHEN \"IsShipped\" = TRUE THEN \"Quantity\" ELSE @p0 END > @p1");
    }

    [Fact]
    public void CaseWhen_BoolColumn_ThenNumericColumn_MySQL()
    {
        var result = Translate<TestOrder>(
            x => (x.IsShipped ? x.Quantity : 0) > 0,
            MySql);

        result.Sql.Should().Be("CASE WHEN `IsShipped` = TRUE THEN `Quantity` ELSE @p0 END > @p1");
    }

    [Fact]
    public void CaseWhen_BoolColumn_ThenNumericColumn_SQLite()
    {
        var result = Translate<TestOrder>(
            x => (x.IsShipped ? x.Quantity : 0) > 0,
            Sqlite);

        result.Sql.Should().Be("CASE WHEN \"IsShipped\" = 1 THEN \"Quantity\" ELSE @p0 END > @p1");
    }

    // ── CASE WHEN — comparison test, bool branches ───────────────────────────

    [Fact]
    public void CaseWhen_ComparisonTest_BoolResult_SqlServer()
    {
        // x.IsShipped ? x.Total > 100 : x.Total > 0
        var result = Translate<TestOrder>(
            x => x.IsShipped ? x.Total > 100m : x.Total > 0m,
            SqlServer);

        result.Sql.Should().Be("CASE WHEN [IsShipped] = 1 THEN [Total] > @p0 ELSE [Total] > @p1 END");
        result.Parameters["p0"].Should().Be(100m);
        result.Parameters["p1"].Should().Be(0m);
    }

    // ── CASE WHEN — numeric comparison test ──────────────────────────────────

    [Fact]
    public void CaseWhen_NumericTest_ThenElseConstants_SqlServer()
    {
        // (x.Quantity > 10 ? 1 : 0) > 0
        var result = Translate<TestOrder>(
            x => (x.Quantity > 10 ? 1 : 0) > 0,
            SqlServer);

        result.Sql.Should().Be("CASE WHEN [Quantity] > @p0 THEN @p1 ELSE @p2 END > @p3");
        result.Parameters["p0"].Should().Be(10);
        result.Parameters["p1"].Should().Be(1);
        result.Parameters["p2"].Should().Be(0);
        result.Parameters["p3"].Should().Be(0);
    }

    // ── CASE WHEN — negated ──────────────────────────────────────────────────

    [Fact]
    public void CaseWhen_Negated_WrapsWithNot_SqlServer()
    {
        var result = Translate<TestOrder>(
            x => !(x.IsShipped ? x.Total > 100m : x.Total > 0m),
            SqlServer);

        result.Sql.Should().StartWith("NOT (CASE WHEN");
        result.Sql.Should().EndWith("END)");
    }

    // ── Enum.HasFlag ─────────────────────────────────────────────────────────

    [Fact]
    public void HasFlag_SingleBit_SqlServer()
    {
        var result = Translate<TestOrder>(
            x => x.Permissions.HasFlag(OrderPermissions.View),
            SqlServer);

        result.Sql.Should().Be("([Permissions] & 1) = 1");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void HasFlag_SingleBit_PostgreSQL()
    {
        var result = Translate<TestOrder>(
            x => x.Permissions.HasFlag(OrderPermissions.View),
            Postgres);

        result.Sql.Should().Be("(\"Permissions\" & 1) = 1");
    }

    [Fact]
    public void HasFlag_SingleBit_MySQL()
    {
        var result = Translate<TestOrder>(
            x => x.Permissions.HasFlag(OrderPermissions.View),
            MySql);

        result.Sql.Should().Be("(`Permissions` & 1) = 1");
    }

    [Fact]
    public void HasFlag_SingleBit_SQLite()
    {
        var result = Translate<TestOrder>(
            x => x.Permissions.HasFlag(OrderPermissions.View),
            Sqlite);

        result.Sql.Should().Be("(\"Permissions\" & 1) = 1");
    }

    [Fact]
    public void HasFlag_MultiBitValue_SqlServer()
    {
        var result = Translate<TestOrder>(
            x => x.Permissions.HasFlag(OrderPermissions.Edit),
            SqlServer);

        result.Sql.Should().Be("([Permissions] & 2) = 2");
    }

    [Fact]
    public void HasFlag_LargerBit_SqlServer()
    {
        var result = Translate<TestOrder>(
            x => x.Permissions.HasFlag(OrderPermissions.Admin),
            SqlServer);

        result.Sql.Should().Be("([Permissions] & 8) = 8");
    }

    [Fact]
    public void HasFlag_CombinedWithAnd_SqlServer()
    {
        var result = Translate<TestOrder>(
            x => x.IsShipped && x.Permissions.HasFlag(OrderPermissions.View),
            SqlServer);

        result.Sql.Should().Be("([IsShipped] = 1 AND ([Permissions] & 1) = 1)");
    }

    [Fact]
    public void HasFlag_Negated_SqlServer()
    {
        var result = Translate<TestOrder>(
            x => !x.Permissions.HasFlag(OrderPermissions.Delete),
            SqlServer);

        result.Sql.Should().Be("NOT (([Permissions] & 4) = 4)");
    }
}
