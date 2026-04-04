using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Extensions;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for <see cref="ValiFlowSqlExtensions"/>.</summary>
public sealed class ValiFlowSqlExtensionsTests
{
    private static readonly ISqlDialect SqlServer = new SqlServerDialect();
    private static readonly ISqlDialect Postgres = new PostgreSqlDialect();

    // ── ToSql (WHERE clause only) ─────────────────────────────────────────────

    [Fact]
    public void ToSql_SimpleCondition_ReturnsSqlResult()
    {
        var flow = new ValiFlow<TestUser>().Add(x => x.Age > 18);
        var result = flow.ToSql(SqlServer);

        result.Sql.Should().Be("[Age] > @p0");
        result.Parameters["p0"].Should().Be(18);
    }

    [Fact]
    public void ToSql_NullFlow_ThrowsArgumentNull()
    {
        ValiFlow<TestUser>? flow = null;
        var act = () => flow!.ToSql(SqlServer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("flow");
    }

    [Fact]
    public void ToSql_NullDialect_ThrowsArgumentNull()
    {
        var flow = new ValiFlow<TestUser>().Add(x => x.Age > 18);
        var act = () => flow.ToSql(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dialect");
    }

    [Fact]
    public void ToSql_Expression_Overload()
    {
        System.Linq.Expressions.Expression<Func<TestUser, bool>> expr = x => x.Id == 42;
        var result = expr.ToSql(SqlServer);
        result.Sql.Should().Be("[Id] = @p0");
        result.Parameters["p0"].Should().Be(42);
    }

    // ── ToSqlCount ────────────────────────────────────────────────────────────

    [Fact]
    public void ToSqlCount_WithFilter_GeneratesCountQueryWithWhere()
    {
        // Boolean member access (BUG-08 fix) generates the literal directly: [IsActive] = 1
        var flow = new ValiFlow<TestUser>().Add(x => x.IsActive);
        var result = flow.ToSqlCount(SqlServer, "Users");

        result.Sql.Should().Be("SELECT COUNT(*) FROM [Users] WHERE [IsActive] = 1");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void ToSqlCount_NullFlow_CountsAllRows()
    {
        ValiFlow<TestUser>? flow = null;
        var result = flow.ToSqlCount(SqlServer, "Users");

        result.Sql.Should().Be("SELECT COUNT(*) FROM [Users]");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void ToSqlCount_NoTableName_UsesTypeName()
    {
        var result = ((ValiFlow<TestUser>?)null).ToSqlCount(SqlServer);
        result.Sql.Should().Be("SELECT COUNT(*) FROM [TestUser]");
    }

    [Fact]
    public void ToSqlCount_PostgreSql_UsesDoubleQuotes()
    {
        var flow = new ValiFlow<TestUser>().Add(x => x.Age > 18);
        var result = flow.ToSqlCount(Postgres, "users");
        result.Sql.Should().Be("SELECT COUNT(*) FROM \"users\" WHERE \"Age\" > @p0");
    }

    [Fact]
    public void ToSqlCount_NullDialect_ThrowsArgumentNull()
    {
        var act = () => ((ValiFlow<TestUser>?)null).ToSqlCount(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dialect");
    }

    // ── ToSql (ValiFlowQuery<T> overload) ─────────────────────────────────────

    [Fact]
    public void ToSql_ValiFlowQuery_SimpleCondition_ReturnsSqlResult()
    {
        var flow = new ValiFlowQuery<TestUser>().Add(x => x.Age > 18);
        var result = flow.ToSql(SqlServer);

        result.Sql.Should().Be("[Age] > @p0");
        result.Parameters["p0"].Should().Be(18);
    }

    [Fact]
    public void ToSql_ValiFlowQuery_NullFlow_ThrowsArgumentNull()
    {
        ValiFlowQuery<TestUser>? flow = null;
        var act = () => flow!.ToSql(SqlServer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("flow");
    }

    [Fact]
    public void ToSql_ValiFlowQuery_NullDialect_ThrowsArgumentNull()
    {
        var flow = new ValiFlowQuery<TestUser>().Add(x => x.Age > 18);
        var act = () => flow.ToSql(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dialect");
    }

    [Fact]
    public void ToSql_ValiFlowQuery_PostgreSQL_UsesDoubleQuotes()
    {
        var flow = new ValiFlowQuery<TestUser>().Add(x => x.IsActive);
        var result = flow.ToSql(Postgres);
        result.Sql.Should().Be("\"IsActive\" = TRUE");
    }

    // ── ToSqlCount (ValiFlowQuery<T> overload) ────────────────────────────────

    [Fact]
    public void ToSqlCount_ValiFlowQuery_WithFilter_GeneratesCountQueryWithWhere()
    {
        var flow = new ValiFlowQuery<TestUser>().Add(x => x.IsActive);
        var result = flow.ToSqlCount(SqlServer, "Users");

        result.Sql.Should().Be("SELECT COUNT(*) FROM [Users] WHERE [IsActive] = 1");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void ToSqlCount_ValiFlowQuery_NullFlow_CountsAllRows()
    {
        ValiFlowQuery<TestUser>? flow = null;
        var result = flow.ToSqlCount(SqlServer, "Users");

        result.Sql.Should().Be("SELECT COUNT(*) FROM [Users]");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void ToSqlCount_ValiFlowQuery_NoTableName_UsesTypeName()
    {
        var result = ((ValiFlowQuery<TestUser>?)null).ToSqlCount(SqlServer);
        result.Sql.Should().Be("SELECT COUNT(*) FROM [TestUser]");
    }

    [Fact]
    public void ToSqlCount_ValiFlowQuery_NullDialect_ThrowsArgumentNull()
    {
        var act = () => ((ValiFlowQuery<TestUser>?)null).ToSqlCount(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dialect");
    }

    [Fact]
    public void ToSqlCount_ValiFlowQuery_PostgreSQL_UsesDoubleQuotes()
    {
        var flow = new ValiFlowQuery<TestUser>().Add(x => x.Age > 21);
        var result = flow.ToSqlCount(Postgres, "users");
        result.Sql.Should().Be("SELECT COUNT(*) FROM \"users\" WHERE \"Age\" > @p0");
    }
}
