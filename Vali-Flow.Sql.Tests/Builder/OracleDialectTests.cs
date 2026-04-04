using FluentAssertions;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests.Builder;

/// <summary>Tests for <see cref="OracleDialect"/>.</summary>
public sealed class OracleDialectTests
{
    private static readonly ISqlDialect Oracle = new OracleDialect();

    // ── Properties ────────────────────────────────────────────────────────────

    [Fact]
    public void ParameterPrefix_IsColon()
    {
        Oracle.ParameterPrefix.Should().Be(":");
    }

    [Fact]
    public void QuoteIdentifier_UsesDoubleQuotes()
    {
        Oracle.QuoteIdentifier("Name").Should().Be("\"Name\"");
    }

    [Fact]
    public void TrueValue_Is1()
    {
        Oracle.TrueValue.Should().Be("1");
    }

    [Fact]
    public void FalseValue_Is0()
    {
        Oracle.FalseValue.Should().Be("0");
    }

    [Fact]
    public void DialectName_IsOracle()
    {
        Oracle.DialectName.Should().Be("Oracle");
    }

    [Fact]
    public void SupportsMerge_IsTrue()
    {
        Oracle.SupportsMerge.Should().BeTrue();
    }

    [Fact]
    public void SupportsNullsOrdering_IsTrue()
    {
        Oracle.SupportsNullsOrdering.Should().BeTrue();
    }

    [Fact]
    public void SupportsReturning_IsTrue()
    {
        Oracle.SupportsReturning.Should().BeTrue();
    }

    [Fact]
    public void SupportsInlineOutput_IsFalse()
    {
        Oracle.SupportsInlineOutput.Should().BeFalse();
    }

    [Fact]
    public void SupportsOnConflict_IsFalse()
    {
        Oracle.SupportsOnConflict.Should().BeFalse();
    }

    [Fact]
    public void SupportsOnDuplicateKey_IsFalse()
    {
        Oracle.SupportsOnDuplicateKey.Should().BeFalse();
    }

    [Fact]
    public void CurrentTimestamp_IsSysdate()
    {
        Oracle.CurrentTimestamp.Should().Be("SYSDATE");
    }

    [Fact]
    public void ForUpdateClause_IsForUpdate()
    {
        Oracle.ForUpdateClause.Should().Be("FOR UPDATE");
    }

    [Fact]
    public void ForShareClause_IsEmpty()
    {
        Oracle.ForShareClause.Should().BeEmpty();
    }

    [Fact]
    public void RecursiveCteKeyword_IsEmpty()
    {
        Oracle.RecursiveCteKeyword.Should().BeEmpty();
    }

    [Fact]
    public void SupportsIsDistinctFrom_IsFalse()
    {
        Oracle.SupportsIsDistinctFrom.Should().BeFalse();
    }

    // ── SelectTop ─────────────────────────────────────────────────────────────

    [Fact]
    public void SelectTop_ReturnsEmpty()
    {
        Oracle.SelectTop(10).Should().Be(string.Empty);
    }

    [Fact]
    public void SelectTop_NullTake_ReturnsEmpty()
    {
        Oracle.SelectTop(null).Should().Be(string.Empty);
    }

    // ── LimitOffset ───────────────────────────────────────────────────────────

    [Fact]
    public void LimitOffset_TakeOnly_UsesFetchFirst()
    {
        Oracle.LimitOffset(10, null).Should().Be("OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    public void LimitOffset_TakeAndSkip_UsesOffsetFetch()
    {
        Oracle.LimitOffset(10, 20).Should().Be("OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    public void LimitOffset_NullTake_ReturnsEmpty()
    {
        Oracle.LimitOffset(null, null).Should().Be(string.Empty);
    }

    [Fact]
    public void LimitOffset_NullTakeWithSkip_ReturnsOffsetOnly()
    {
        // I-7: skip-only (no take) must emit OFFSET N ROWS so that Oracle
        // can skip rows without fetching an arbitrary upper bound.
        Oracle.LimitOffset(null, 5).Should().Be("OFFSET 5 ROWS");
    }

    // ── DatePartExpression ────────────────────────────────────────────────────

    [Fact]
    public void DatePartExpression_Year_UsesExtract()
    {
        Oracle.DatePartExpression("\"CreatedAt\"", "YEAR").Should().Be("EXTRACT(YEAR FROM \"CreatedAt\")");
    }

    [Fact]
    public void DatePartExpression_Month_UsesExtract()
    {
        Oracle.DatePartExpression("\"CreatedAt\"", "MONTH").Should().Be("EXTRACT(MONTH FROM \"CreatedAt\")");
    }

    [Fact]
    public void DatePartExpression_Day_UsesExtract()
    {
        Oracle.DatePartExpression("\"CreatedAt\"", "DAY").Should().Be("EXTRACT(DAY FROM \"CreatedAt\")");
    }

    [Fact]
    public void DatePartExpression_Hour_UsesExtract()
    {
        Oracle.DatePartExpression("\"CreatedAt\"", "HOUR").Should().Be("EXTRACT(HOUR FROM \"CreatedAt\")");
    }

    [Fact]
    public void DatePartExpression_Minute_UsesExtract()
    {
        Oracle.DatePartExpression("\"CreatedAt\"", "MINUTE").Should().Be("EXTRACT(MINUTE FROM \"CreatedAt\")");
    }

    [Fact]
    public void DatePartExpression_Second_UsesExtract()
    {
        Oracle.DatePartExpression("\"CreatedAt\"", "SECOND").Should().Be("EXTRACT(SECOND FROM \"CreatedAt\")");
    }

    [Fact]
    public void DatePartExpression_LowercasePart_IsNormalized()
    {
        Oracle.DatePartExpression("\"CreatedAt\"", "year").Should().Be("EXTRACT(YEAR FROM \"CreatedAt\")");
    }

    // ── ILikeExpression ───────────────────────────────────────────────────────

    [Fact]
    public void ILikeExpression_UsesUpperFunction()
    {
        Oracle.ILikeExpression("\"Name\"", ":pw0").Should().Be("UPPER(\"Name\") LIKE UPPER(:pw0)");
    }

    // ── NullsOrderClause ──────────────────────────────────────────────────────

    [Fact]
    public void NullsOrderClause_First_ReturnsNullsFirst()
    {
        Oracle.NullsOrderClause(NullsOrder.First).Should().Be("NULLS FIRST");
    }

    [Fact]
    public void NullsOrderClause_Last_ReturnsNullsLast()
    {
        Oracle.NullsOrderClause(NullsOrder.Last).Should().Be("NULLS LAST");
    }

    [Fact]
    public void NullsOrderClause_Default_ReturnsEmpty()
    {
        Oracle.NullsOrderClause(NullsOrder.Default).Should().BeEmpty();
    }

    // ── NullCheck / NotNullCheck ──────────────────────────────────────────────

    [Fact]
    public void NullCheck_ReturnsIsNull()
    {
        Oracle.NullCheck("\"Name\"").Should().Be("\"Name\" IS NULL");
    }

    [Fact]
    public void NotNullCheck_ReturnsIsNotNull()
    {
        Oracle.NotNullCheck("\"Name\"").Should().Be("\"Name\" IS NOT NULL");
    }

    // ── ConcatExpression ──────────────────────────────────────────────────────

    [Fact]
    public void ConcatExpression_UsesPipe()
    {
        Oracle.ConcatExpression("\"a\"", "\"b\"").Should().Be("\"a\" || \"b\"");
    }

    [Fact]
    public void ConcatExpression_ThreeParts_UsesPipe()
    {
        Oracle.ConcatExpression("\"a\"", "\"b\"", "\"c\"").Should().Be("\"a\" || \"b\" || \"c\"");
    }

    // ── Integration: SqlQueryBuilder ──────────────────────────────────────────

    [Fact]
    public void SqlQueryBuilder_Oracle_UsesColonParameters()
    {
        var result = new SqlQueryBuilder<TestUser>(Oracle)
            .From("Users")
            .Where(w => w.EqualTo(x => x.Id, 1))
            .Build();

        result.Sql.Should().Contain(":pw0");
        result.Parameters["pw0"].Should().Be(1);
    }

    [Fact]
    public void SqlQueryBuilder_Oracle_PaginatesWithFetchFirst()
    {
        var result = new SqlQueryBuilder<TestUser>(Oracle)
            .From("Users")
            .OrderBy(x => x.Id)
            .Take(10)
            .Skip(20)
            .Build();

        result.Sql.Should().Contain("FETCH NEXT 10 ROWS ONLY");
        result.Sql.Should().Contain("OFFSET 20 ROWS");
    }

    [Fact]
    public void SqlQueryBuilder_Oracle_QuotesIdentifiersWithDoubleQuotes()
    {
        var result = new SqlQueryBuilder<TestUser>(Oracle)
            .From("Users")
            .Build();

        result.Sql.Should().Contain("\"Users\"");
    }

    // ── Integration: SqlDeleteBuilder ─────────────────────────────────────────

    [Fact]
    public void SqlDeleteBuilder_Oracle_BasicDelete()
    {
        var result = new SqlDeleteBuilder<TestUser>(Oracle)
            .From("Users")
            .AllowDeleteAll()
            .Build();

        result.Sql.Should().Be("DELETE FROM \"Users\"");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void SqlDeleteBuilder_Oracle_WithWhere_UsesColonParam()
    {
        var result = new SqlDeleteBuilder<TestUser>(Oracle)
            .From("Users")
            .Where(w => w.EqualTo(x => x.Id, 42))
            .Build();

        result.Sql.Should().Contain("WHERE");
        result.Sql.Should().Contain(":pw0");
        result.Parameters["pw0"].Should().Be(42);
    }
}
