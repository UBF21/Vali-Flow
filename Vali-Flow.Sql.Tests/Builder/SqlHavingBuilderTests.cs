using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests.Builder;

/// <summary>Tests for <see cref="SqlHavingBuilder{T}"/>.</summary>
public sealed class SqlHavingBuilderTests
{
    private static SqlHavingBuilder<TestUser> Having() => new();
    private static readonly ISqlDialect Sql = new SqlServerDialect();

    private static (string Sql, IReadOnlyDictionary<string, object> Params) Build(SqlHavingBuilder<TestUser> b)
        => b.Build(Sql);

    // ── COUNT(*) ──────────────────────────────────────────────────────────────

    [Fact]
    public void CountEquals_ProducesEqualsCondition()
    {
        var (sql, p) = Build(Having().CountEquals(5));
        sql.Should().Be("COUNT(*) = @ph0");
        p["ph0"].Should().Be(5);
    }

    [Fact]
    public void CountNotEquals_ProducesNotEqualsCondition()
    {
        var (sql, p) = Build(Having().CountNotEquals(5));
        sql.Should().Be("COUNT(*) <> @ph0");
        p["ph0"].Should().Be(5);
    }

    [Fact]
    public void CountGreaterThan_ProducesGtCondition()
    {
        var (sql, p) = Build(Having().CountGreaterThan(3));
        sql.Should().Be("COUNT(*) > @ph0");
        p["ph0"].Should().Be(3);
    }

    [Fact]
    public void CountGreaterThanOrEqualTo_ProducesGteCondition()
    {
        var (sql, p) = Build(Having().CountGreaterThanOrEqualTo(3));
        sql.Should().Be("COUNT(*) >= @ph0");
        p["ph0"].Should().Be(3);
    }

    [Fact]
    public void CountLessThan_ProducesLtCondition()
    {
        var (sql, p) = Build(Having().CountLessThan(10));
        sql.Should().Be("COUNT(*) < @ph0");
        p["ph0"].Should().Be(10);
    }

    [Fact]
    public void CountLessThanOrEqualTo_ProducesLteCondition()
    {
        var (sql, p) = Build(Having().CountLessThanOrEqualTo(10));
        sql.Should().Be("COUNT(*) <= @ph0");
        p["ph0"].Should().Be(10);
    }

    [Fact]
    public void CountBetween_ProducesBetweenCondition()
    {
        var (sql, p) = Build(Having().CountBetween(2, 10));
        sql.Should().Be("COUNT(*) BETWEEN @ph0 AND @ph1");
        p["ph0"].Should().Be(2);
        p["ph1"].Should().Be(10);
    }

    // ── SUM ───────────────────────────────────────────────────────────────────

    [Fact]
    public void SumGreaterThan_ProducesGtCondition()
    {
        var (sql, p) = Build(Having().SumGreaterThan(x => x.Salary, 10000m));
        sql.Should().Be("SUM([Salary]) > @ph0");
        p["ph0"].Should().Be(10000m);
    }

    [Fact]
    public void SumGreaterThanOrEqualTo_ProducesGteCondition()
    {
        var (sql, p) = Build(Having().SumGreaterThanOrEqualTo(x => x.Salary, 10000m));
        sql.Should().Be("SUM([Salary]) >= @ph0");
        p["ph0"].Should().Be(10000m);
    }

    [Fact]
    public void SumLessThan_ProducesLtCondition()
    {
        var (sql, p) = Build(Having().SumLessThan(x => x.Salary, 10000m));
        sql.Should().Be("SUM([Salary]) < @ph0");
        p["ph0"].Should().Be(10000m);
    }

    [Fact]
    public void SumLessThanOrEqualTo_ProducesLteCondition()
    {
        var (sql, p) = Build(Having().SumLessThanOrEqualTo(x => x.Salary, 10000m));
        sql.Should().Be("SUM([Salary]) <= @ph0");
        p["ph0"].Should().Be(10000m);
    }

    [Fact]
    public void SumBetween_ProducesBetweenCondition()
    {
        var (sql, p) = Build(Having().SumBetween(x => x.Salary, 1000m, 50000m));
        sql.Should().Be("SUM([Salary]) BETWEEN @ph0 AND @ph1");
        p["ph0"].Should().Be(1000m);
        p["ph1"].Should().Be(50000m);
    }

    // ── AVG ───────────────────────────────────────────────────────────────────

    [Fact]
    public void AverageGreaterThan_ProducesGtCondition()
    {
        var (sql, p) = Build(Having().AverageGreaterThan(x => x.Salary, 3000m));
        sql.Should().Be("AVG([Salary]) > @ph0");
        p["ph0"].Should().Be(3000m);
    }

    [Fact]
    public void AverageLessThan_ProducesLtCondition()
    {
        var (sql, p) = Build(Having().AverageLessThan(x => x.Salary, 3000m));
        sql.Should().Be("AVG([Salary]) < @ph0");
        p["ph0"].Should().Be(3000m);
    }

    [Fact]
    public void AverageBetween_ProducesBetweenCondition()
    {
        var (sql, p) = Build(Having().AverageBetween(x => x.Salary, 500m, 5000m));
        sql.Should().Be("AVG([Salary]) BETWEEN @ph0 AND @ph1");
        p["ph0"].Should().Be(500m);
        p["ph1"].Should().Be(5000m);
    }

    // ── MIN ───────────────────────────────────────────────────────────────────

    [Fact]
    public void MinGreaterThan_ProducesGtCondition()
    {
        var (sql, p) = Build(Having().MinGreaterThan(x => x.Salary, 1000m));
        sql.Should().Be("MIN([Salary]) > @ph0");
        p["ph0"].Should().Be(1000m);
    }

    [Fact]
    public void MinLessThan_ProducesLtCondition()
    {
        var (sql, p) = Build(Having().MinLessThan(x => x.Salary, 1000m));
        sql.Should().Be("MIN([Salary]) < @ph0");
        p["ph0"].Should().Be(1000m);
    }

    // ── MAX ───────────────────────────────────────────────────────────────────

    [Fact]
    public void MaxGreaterThan_ProducesGtCondition()
    {
        var (sql, p) = Build(Having().MaxGreaterThan(x => x.Salary, 9000m));
        sql.Should().Be("MAX([Salary]) > @ph0");
        p["ph0"].Should().Be(9000m);
    }

    [Fact]
    public void MaxLessThan_ProducesLtCondition()
    {
        var (sql, p) = Build(Having().MaxLessThan(x => x.Salary, 9000m));
        sql.Should().Be("MAX([Salary]) < @ph0");
        p["ph0"].Should().Be(9000m);
    }

    // ── AND/OR grouping ───────────────────────────────────────────────────────

    [Fact]
    public void AndIsDefault_SingleGroup_NoParens()
    {
        var (sql, _) = Build(Having()
            .CountGreaterThan(1)
            .And()
            .SumGreaterThan(x => x.Salary, 1000m));
        sql.Should().Be("COUNT(*) > @ph0 AND SUM([Salary]) > @ph1");
    }

    [Fact]
    public void Or_CreatesNewGroup_WithOuterParens()
    {
        var (sql, _) = Build(Having()
            .CountGreaterThan(5)
            .Or()
            .SumGreaterThan(x => x.Salary, 10000m));
        sql.Should().Be("(COUNT(*) > @ph0 OR SUM([Salary]) > @ph1)");
    }

    [Fact]
    public void MixedAndOr_CorrectParenthesization()
    {
        // (COUNT(*) > @ph0 AND SUM([Salary]) > @ph1) OR MIN([Salary]) < @ph2
        var (sql, _) = Build(Having()
            .CountGreaterThan(2)
            .SumGreaterThan(x => x.Salary, 5000m)
            .Or()
            .MinLessThan(x => x.Salary, 500m));
        sql.Should().Be("((COUNT(*) > @ph0 AND SUM([Salary]) > @ph1) OR MIN([Salary]) < @ph2)");
    }

    // ── Empty builder ─────────────────────────────────────────────────────────

    [Fact]
    public void EmptyBuilder_ReturnsEmptyString()
    {
        var (sql, _) = Build(Having());
        sql.Should().Be(string.Empty);
    }

    // ── Parameter prefix ──────────────────────────────────────────────────────

    [Fact]
    public void Parameters_UsePhPrefix_NoAtSign()
    {
        var (_, p) = Build(Having().CountGreaterThan(1).CountLessThan(100));
        p.Keys.Should().Contain("ph0");
        p.Keys.Should().Contain("ph1");
        p.Keys.Should().NotContain("@ph0");
    }

    // ── CountNotBetween ───────────────────────────────────────────────────────

    [Fact]
    public void CountNotBetween_GeneratesNotBetweenCondition()
    {
        var (sql, p) = Build(Having().CountNotBetween(5, 10));
        sql.Should().Be("COUNT(*) NOT BETWEEN @ph0 AND @ph1");
        p["ph0"].Should().Be(5);
        p["ph1"].Should().Be(10);
    }

    // ── SUM equals / not-equals / not-between ─────────────────────────────────

    [Fact]
    public void SumEquals_GeneratesEqualCondition()
    {
        var (sql, p) = Build(Having().SumEquals(x => x.Salary, 1000m));
        sql.Should().Be("SUM([Salary]) = @ph0");
        p["ph0"].Should().Be(1000m);
    }

    [Fact]
    public void SumNotEquals_GeneratesNotEqualCondition()
    {
        var (sql, _) = Build(Having().SumNotEquals(x => x.Salary, 0m));
        sql.Should().Be("SUM([Salary]) <> @ph0");
    }

    [Fact]
    public void SumNotBetween_GeneratesNotBetweenCondition()
    {
        var (sql, p) = Build(Having().SumNotBetween(x => x.Salary, 100m, 500m));
        sql.Should().Be("SUM([Salary]) NOT BETWEEN @ph0 AND @ph1");
        p["ph0"].Should().Be(100m);
        p["ph1"].Should().Be(500m);
    }

    // ── AVG equals / not-equals / not-between ─────────────────────────────────

    [Fact]
    public void AverageEquals_GeneratesEqualCondition()
    {
        var (sql, _) = Build(Having().AverageEquals(x => x.Salary, 2500m));
        sql.Should().Be("AVG([Salary]) = @ph0");
    }

    [Fact]
    public void AverageNotEquals_GeneratesNotEqualCondition()
    {
        var (sql, _) = Build(Having().AverageNotEquals(x => x.Salary, 0m));
        sql.Should().Be("AVG([Salary]) <> @ph0");
    }

    [Fact]
    public void AverageNotBetween_GeneratesNotBetweenCondition()
    {
        var (sql, _) = Build(Having().AverageNotBetween(x => x.Salary, 1000m, 5000m));
        sql.Should().Be("AVG([Salary]) NOT BETWEEN @ph0 AND @ph1");
    }

    // ── MIN full set ──────────────────────────────────────────────────────────

    [Fact]
    public void MinEquals_GeneratesEqualCondition()
    {
        var (sql, _) = Build(Having().MinEquals(x => x.Age, 18));
        sql.Should().Be("MIN([Age]) = @ph0");
    }

    [Fact]
    public void MinNotEquals_GeneratesNotEqualCondition()
    {
        var (sql, _) = Build(Having().MinNotEquals(x => x.Age, 0));
        sql.Should().Be("MIN([Age]) <> @ph0");
    }

    [Fact]
    public void MinBetween_GeneratesBetweenCondition()
    {
        var (sql, p) = Build(Having().MinBetween(x => x.Age, 18, 65));
        sql.Should().Be("MIN([Age]) BETWEEN @ph0 AND @ph1");
        p["ph0"].Should().Be(18);
        p["ph1"].Should().Be(65);
    }

    [Fact]
    public void MinNotBetween_GeneratesNotBetweenCondition()
    {
        var (sql, _) = Build(Having().MinNotBetween(x => x.Age, 18, 65));
        sql.Should().Be("MIN([Age]) NOT BETWEEN @ph0 AND @ph1");
    }

    // ── MAX full set ──────────────────────────────────────────────────────────

    [Fact]
    public void MaxEquals_GeneratesEqualCondition()
    {
        var (sql, _) = Build(Having().MaxEquals(x => x.Age, 65));
        sql.Should().Be("MAX([Age]) = @ph0");
    }

    [Fact]
    public void MaxNotEquals_GeneratesNotEqualCondition()
    {
        var (sql, _) = Build(Having().MaxNotEquals(x => x.Age, 0));
        sql.Should().Be("MAX([Age]) <> @ph0");
    }

    [Fact]
    public void MaxBetween_GeneratesBetweenCondition()
    {
        var (sql, p) = Build(Having().MaxBetween(x => x.Age, 20, 80));
        sql.Should().Be("MAX([Age]) BETWEEN @ph0 AND @ph1");
        p["ph0"].Should().Be(20);
        p["ph1"].Should().Be(80);
    }

    [Fact]
    public void MaxNotBetween_GeneratesNotBetweenCondition()
    {
        var (sql, _) = Build(Having().MaxNotBetween(x => x.Age, 20, 80));
        sql.Should().Be("MAX([Age]) NOT BETWEEN @ph0 AND @ph1");
    }

    // ── COUNT(DISTINCT) ───────────────────────────────────────────────────────

    [Fact]
    public void CountDistinctGreaterThan_GeneratesCountDistinctCondition()
    {
        var (sql, p) = Build(Having().CountDistinctGreaterThan(x => x.Department, 3));
        sql.Should().Be("COUNT(DISTINCT [Department]) > @ph0");
        p["ph0"].Should().Be(3);
    }

    [Fact]
    public void CountDistinctEquals_GeneratesCountDistinctEquals()
    {
        var (sql, p) = Build(Having().CountDistinctEquals(x => x.Status, 1));
        sql.Should().Be("COUNT(DISTINCT [Status]) = @ph0");
        p["ph0"].Should().Be(1);
    }

    [Fact]
    public void CountDistinctGreaterThanOrEqualTo_GeneratesCountDistinctGte()
    {
        var (sql, p) = Build(Having().CountDistinctGreaterThanOrEqualTo(x => x.Department, 2));
        sql.Should().Be("COUNT(DISTINCT [Department]) >= @ph0");
        p["ph0"].Should().Be(2);
    }

    [Fact]
    public void CountDistinctLessThan_GeneratesCountDistinctLt()
    {
        var (sql, _) = Build(Having().CountDistinctLessThan(x => x.Status, 5));
        sql.Should().Be("COUNT(DISTINCT [Status]) < @ph0");
    }

    [Fact]
    public void CountDistinctLessThanOrEqualTo_GeneratesCountDistinctLte()
    {
        var (sql, _) = Build(Having().CountDistinctLessThanOrEqualTo(x => x.Status, 10));
        sql.Should().Be("COUNT(DISTINCT [Status]) <= @ph0");
    }

    // ── SUM(DISTINCT) ─────────────────────────────────────────────────────────

    [Fact]
    public void SumDistinctGreaterThan_GeneratesSumDistinctCondition()
    {
        var (sql, p) = Build(Having().SumDistinctGreaterThan(x => x.Salary, 5000m));
        sql.Should().Be("SUM(DISTINCT [Salary]) > @ph0");
        p["ph0"].Should().Be(5000m);
    }

    [Fact]
    public void SumDistinctEquals_GeneratesSumDistinctEquals()
    {
        var (sql, p) = Build(Having().SumDistinctEquals(x => x.Salary, 10000m));
        sql.Should().Be("SUM(DISTINCT [Salary]) = @ph0");
        p["ph0"].Should().Be(10000m);
    }

    [Fact]
    public void SumDistinctGreaterThanOrEqualTo_GeneratesSumDistinctGte()
    {
        var (sql, _) = Build(Having().SumDistinctGreaterThanOrEqualTo(x => x.Salary, 3000m));
        sql.Should().Be("SUM(DISTINCT [Salary]) >= @ph0");
    }

    [Fact]
    public void SumDistinctLessThan_GeneratesSumDistinctLt()
    {
        var (sql, _) = Build(Having().SumDistinctLessThan(x => x.Salary, 8000m));
        sql.Should().Be("SUM(DISTINCT [Salary]) < @ph0");
    }

    [Fact]
    public void SumDistinctLessThanOrEqualTo_GeneratesSumDistinctLte()
    {
        var (sql, _) = Build(Having().SumDistinctLessThanOrEqualTo(x => x.Salary, 7000m));
        sql.Should().Be("SUM(DISTINCT [Salary]) <= @ph0");
    }
}
