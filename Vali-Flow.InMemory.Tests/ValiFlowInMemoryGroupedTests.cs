using FluentAssertions;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;
using Xunit;

namespace Vali_Flow.InMemory.Tests;

/// <summary>Tests for all grouped evaluation methods in the InMemory evaluator.</summary>
public sealed class ValiFlowInMemoryGroupedTests
{
    private static List<TestProduct> MakeData() => new()
    {
        new() { Id = 1, Name = "A1", Category = "Electronics", Price = 100m, Stock = 5,  IsActive = true  },
        new() { Id = 2, Name = "A2", Category = "Electronics", Price = 200m, Stock = 3,  IsActive = true  },
        new() { Id = 3, Name = "B1", Category = "Books",       Price = 30m,  Stock = 10, IsActive = true  },
        new() { Id = 4, Name = "B2", Category = "Books",       Price = 50m,  Stock = 8,  IsActive = false },
        new() { Id = 5, Name = "C1", Category = "Clothing",    Price = 80m,  Stock = 2,  IsActive = true  },
    };

    // ── EvaluateGrouped ───────────────────────────────────────────────────────

    [Fact]
    public void EvaluateGrouped_ByCategory_ReturnsCorrectGroups()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData());

        var groups = ev.EvaluateGrouped(null, p => p.Category);

        groups.Should().HaveCount(3);
        groups["Electronics"].Should().HaveCount(2);
        groups["Books"].Should().HaveCount(2);
        groups["Clothing"].Should().HaveCount(1);
    }

    [Fact]
    public void EvaluateGrouped_WithFilter_OnlyIncludesMatchingEntities()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData());
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var groups = ev.EvaluateGrouped(null, p => p.Category, filter);

        groups.Should().HaveCount(3);
        groups["Books"].Should().HaveCount(1); // B2 (inactive) excluded
    }

    // ── EvaluateCountByGroup ──────────────────────────────────────────────────

    [Fact]
    public void EvaluateCountByGroup_ByCategory_ReturnsCorrectCounts()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData());

        var counts = ev.EvaluateCountByGroup(null, p => p.Category);

        counts["Electronics"].Should().Be(2);
        counts["Books"].Should().Be(2);
        counts["Clothing"].Should().Be(1);
    }

    // ── EvaluateSumByGroup ────────────────────────────────────────────────────

    [Fact]
    public void EvaluateSumByGroup_PriceByCategory_ReturnsSumsPerGroup()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData());

        var sums = ev.EvaluateSumByGroup(null, p => p.Category, p => p.Price);

        sums["Electronics"].Should().Be(300m); // 100 + 200
        sums["Books"].Should().Be(80m);        // 30 + 50
        sums["Clothing"].Should().Be(80m);     // 80
    }

    // ── EvaluateMinByGroup ────────────────────────────────────────────────────

    [Fact]
    public void EvaluateMinByGroup_PriceByCategory_ReturnsMinPerGroup()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData());

        var mins = ev.EvaluateMinByGroup(null, p => p.Category, p => p.Price);

        mins["Electronics"].Should().Be(100m);
        mins["Books"].Should().Be(30m);
    }

    // ── EvaluateMaxByGroup ────────────────────────────────────────────────────

    [Fact]
    public void EvaluateMaxByGroup_PriceByCategory_ReturnsMaxPerGroup()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData());

        var maxs = ev.EvaluateMaxByGroup(null, p => p.Category, p => p.Price);

        maxs["Electronics"].Should().Be(200m);
        maxs["Books"].Should().Be(50m);
    }

    // ── EvaluateAverageByGroup ────────────────────────────────────────────────

    [Fact]
    public void EvaluateAverageByGroup_PriceByCategory_ReturnsAveragesPerGroup()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData());

        var avgs = ev.EvaluateAverageByGroup(null, p => p.Category, p => p.Price);

        avgs["Electronics"].Should().Be(150m); // (100+200)/2
        avgs["Clothing"].Should().Be(80m);     // 80/1
    }

    // ── EvaluateTopByGroup ────────────────────────────────────────────────────

    [Fact]
    public void EvaluateTopByGroup_Top1ByPriceDescending_ReturnsHighestPerGroup()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData());

        var tops = ev.EvaluateTopByGroup<string, decimal>(
            null,
            p => p.Category,
            count: 1,
            orderBy: p => p.Price,
            ascending: false);

        tops["Electronics"].Should().HaveCount(1);
        tops["Electronics"].First().Price.Should().Be(200m);
        tops["Books"].First().Price.Should().Be(50m);
    }

    // ── EvaluateDuplicatesByGroup ─────────────────────────────────────────────

    [Fact]
    public void EvaluateDuplicatesByGroup_ByCategory_ReturnsOnlyMultiItemGroups()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData());

        var dupes = ev.EvaluateDuplicatesByGroup(null, p => p.Category);

        // Electronics(2) and Books(2) have duplicates; Clothing(1) does not
        dupes.Should().HaveCount(2);
        dupes.Should().ContainKey("Electronics");
        dupes.Should().ContainKey("Books");
        dupes.Should().NotContainKey("Clothing");
    }

    // ── EvaluateUniquesByGroup ────────────────────────────────────────────────

    [Fact]
    public void EvaluateUniquesByGroup_ByCategory_ReturnsOnlySingletonGroups()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData());

        var uniques = ev.EvaluateUniquesByGroup(null, p => p.Category);

        // Only Clothing has exactly one item
        uniques.Should().HaveCount(1);
        uniques.Should().ContainKey("Clothing");
    }

    // ── EvaluateAggregate ─────────────────────────────────────────────────────

    [Fact]
    public void EvaluateAggregate_CustomSum_ReturnsCorrectResult()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData());

        // Sum of all prices: 100+200+30+50+80 = 460
        decimal total = ev.EvaluateAggregate(null, p => p.Price, (acc, x) => acc + x);

        total.Should().Be(460m);
    }
}
