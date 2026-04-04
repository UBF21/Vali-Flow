using FluentAssertions;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;
using Xunit;

namespace Vali_Flow.InMemory.Tests;

/// <summary>
/// Covers EvaluateSumByGroup, EvaluateAverageByGroup, EvaluateAggregate,
/// EvaluateDuplicatesByGroup, UpdateRange (O(N+M)), DeleteRange (O(N+M)),
/// SaveChanges (HashSet), and EvaluateTopByGroup (typed TOrderKey).
/// </summary>
public sealed class ValiFlowInMemoryByGroupTests
{
    // ── Seed ─────────────────────────────────────────────────────────────────
    // Id  Name    Category  Price   Stock  Active
    //  1  Apple   Fruit     1.50    100    true
    //  2  Banana  Fruit     0.80    150    true
    //  3  Carrot  Veggie    0.50    200    true
    //  4  Donut   Sweet     2.50     50    false
    //  5  Egg     Dairy     3.00     80    true
    //  6  Fig     Fruit     4.50     30    true
    //  7  Grape   Fruit     3.20     60    false
    //  8  Honey   Sweet     7.00     20    true
    //  9  Ice     Dairy     1.20     90    true
    // 10  Jam     Sweet     2.80     45    false

    private static List<TestProduct> Seed() => new()
    {
        new() { Id = 1,  Name = "Apple",  Category = "Fruit",  Price = 1.50m, Stock = 100, IsActive = true  },
        new() { Id = 2,  Name = "Banana", Category = "Fruit",  Price = 0.80m, Stock = 150, IsActive = true  },
        new() { Id = 3,  Name = "Carrot", Category = "Veggie", Price = 0.50m, Stock = 200, IsActive = true  },
        new() { Id = 4,  Name = "Donut",  Category = "Sweet",  Price = 2.50m, Stock = 50,  IsActive = false },
        new() { Id = 5,  Name = "Egg",    Category = "Dairy",  Price = 3.00m, Stock = 80,  IsActive = true  },
        new() { Id = 6,  Name = "Fig",    Category = "Fruit",  Price = 4.50m, Stock = 30,  IsActive = true  },
        new() { Id = 7,  Name = "Grape",  Category = "Fruit",  Price = 3.20m, Stock = 60,  IsActive = false },
        new() { Id = 8,  Name = "Honey",  Category = "Sweet",  Price = 7.00m, Stock = 20,  IsActive = true  },
        new() { Id = 9,  Name = "Ice",    Category = "Dairy",  Price = 1.20m, Stock = 90,  IsActive = true  },
        new() { Id = 10, Name = "Jam",    Category = "Sweet",  Price = 2.80m, Stock = 45,  IsActive = false },
    };

    private static ValiFlowEvaluator<TestProduct, int> Ev(List<TestProduct> data) =>
        new(data, null, p => p.Id);

    // ── EvaluateSumByGroup ────────────────────────────────────────────────────

    [Fact]
    public void EvaluateSumByGroup_AllCategories_CorrectSums()
    {
        var data = Seed();
        var ev = Ev(data);

        // Fruit: 1.50+0.80+4.50+3.20 = 10.00
        var result = ev.EvaluateSumByGroup(data, p => p.Category, p => p.Price);

        result["Fruit"].Should().Be(10.00m);
        result["Veggie"].Should().Be(0.50m);
        result["Sweet"].Should().Be(2.50m + 7.00m + 2.80m);
        result["Dairy"].Should().Be(3.00m + 1.20m);
    }

    [Fact]
    public void EvaluateSumByGroup_WithFilter_OnlyActiveItems()
    {
        var data = Seed();
        var ev = Ev(data);
        var active = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Active Fruit: Apple(1.50) + Banana(0.80) + Fig(4.50) = 6.80
        var result = ev.EvaluateSumByGroup(data, p => p.Category, p => p.Price, valiFlow: active);

        result["Fruit"].Should().Be(6.80m);
        result["Sweet"].Should().Be(7.00m);   // only Honey(active)
        result["Dairy"].Should().Be(4.20m);   // Egg(3.00) + Ice(1.20)
        result["Veggie"].Should().Be(0.50m);  // Carrot is active
    }

    [Fact]
    public void EvaluateSumByGroup_IntSelector_CorrectStockSums()
    {
        var data = Seed();
        var ev = Ev(data);

        var result = ev.EvaluateSumByGroup(data, p => p.Category, p => p.Stock);

        result["Fruit"].Should().Be(100 + 150 + 30 + 60); // 340
        result["Veggie"].Should().Be(200);
    }

    // ── EvaluateAverageByGroup ────────────────────────────────────────────────

    [Fact]
    public void EvaluateAverageByGroup_FruitCategory_CorrectAverage()
    {
        var data = Seed();
        var ev = Ev(data);

        // Fruit avg price: (1.50+0.80+4.50+3.20)/4 = 10.00/4 = 2.50
        var result = ev.EvaluateAverageByGroup(data, p => p.Category, p => p.Price);

        result["Fruit"].Should().Be(2.50m);
    }

    [Fact]
    public void EvaluateAverageByGroup_DecimalPrecision_NoBoxingArtifacts()
    {
        var data = Seed();
        var ev = Ev(data);

        // Dairy avg: (3.00+1.20)/2 = 4.20/2 = 2.10
        var result = ev.EvaluateAverageByGroup(data, p => p.Category, p => p.Price);

        result["Dairy"].Should().Be(2.10m);
    }

    [Fact]
    public void EvaluateAverageByGroup_SingleItemGroup_ReturnsItemValue()
    {
        var data = Seed();
        var ev = Ev(data);

        // Veggie has only Carrot at 0.50
        var result = ev.EvaluateAverageByGroup(data, p => p.Category, p => p.Price);

        result["Veggie"].Should().Be(0.50m);
    }

    // ── EvaluateAggregate ─────────────────────────────────────────────────────

    [Fact]
    public void EvaluateAggregate_SumWithCustomAggregator_CorrectResult()
    {
        var data = Seed();
        var ev = Ev(data);

        decimal result = ev.EvaluateAggregate(data, p => p.Price, (acc, x) => acc + x);

        // Total of all prices
        result.Should().Be(data.Sum(p => p.Price));
    }

    [Fact]
    public void EvaluateAggregate_MaxWithCustomAggregator_ReturnsMax()
    {
        var data = Seed();
        var ev = Ev(data);

        decimal result = ev.EvaluateAggregate(data, p => p.Price, (acc, x) => acc > x ? acc : x);

        result.Should().Be(7.00m); // Honey
    }

    [Fact]
    public void EvaluateAggregate_WithFilter_OnlyActiveProducts()
    {
        var data = Seed();
        var ev = Ev(data);
        var active = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Active: Apple+Banana+Carrot+Egg+Fig+Honey+Ice = 1.50+0.80+0.50+3.00+4.50+7.00+1.20 = 18.50
        decimal result = ev.EvaluateAggregate(data, p => p.Price, (acc, x) => acc + x, valiFlow: active);

        result.Should().Be(18.50m);
    }

    // ── EvaluateDuplicatesByGroup ─────────────────────────────────────────────

    [Fact]
    public void EvaluateDuplicatesByGroup_ByCategory_ExcludesSingleGroups()
    {
        var data = Seed();
        var ev = Ev(data);

        var result = ev.EvaluateDuplicatesByGroup(data, p => p.Category);

        // Veggie has only 1 item → excluded
        result.Should().NotContainKey("Veggie");
        result.Should().ContainKey("Fruit");
        result.Should().ContainKey("Sweet");
        result.Should().ContainKey("Dairy");
    }

    [Fact]
    public void EvaluateDuplicatesByGroup_FruitGroup_ContainsAllFourItems()
    {
        var data = Seed();
        var ev = Ev(data);

        var result = ev.EvaluateDuplicatesByGroup(data, p => p.Category);

        result["Fruit"].Should().HaveCount(4);
    }

    [Fact]
    public void EvaluateDuplicatesByGroup_NoDuplicates_ReturnsEmpty()
    {
        var data = Seed();
        var ev = Ev(data);

        // All unique Ids — no duplicates by Id
        var result = ev.EvaluateDuplicatesByGroup(data, p => p.Id);

        result.Should().BeEmpty();
    }

    // ── UpdateRange O(N+M) ───────────────────────────────────────────────────

    [Fact]
    public void UpdateRange_1000Entities_UpdatesAllCorrectly()
    {
        var data = Enumerable.Range(1, 1000).Select(i => new TestProduct
        {
            Id = i, Name = $"P{i}", Category = "Cat", Price = i * 1m, Stock = i, IsActive = true
        }).ToList();
        var ev = Ev(data);

        var updates = data.Select(p => new TestProduct
        {
            Id = p.Id, Name = $"Updated{p.Id}", Category = p.Category,
            Price = p.Price * 2, Stock = p.Stock, IsActive = p.IsActive
        }).ToList();

        var result = ev.UpdateRange(updates, data);

        result.Should().HaveCount(1000);
        data.Should().AllSatisfy(p => p.Name.Should().StartWith("Updated"));
    }

    [Fact]
    public void UpdateRange_PartialMatch_UpdatesOnlyExisting()
    {
        var data = Seed();
        var ev = Ev(data);

        var updates = new List<TestProduct>
        {
            new() { Id = 1, Name = "Apple v2", Category = "Fruit", Price = 9.99m, Stock = 100, IsActive = true },
            new() { Id = 99, Name = "Ghost",   Category = "Fruit", Price = 1.00m, Stock = 10,  IsActive = true }, // not exist
        };

        var result = ev.UpdateRange(updates, data);

        result.Should().HaveCount(1); // only Id=1 found
        data.First(p => p.Id == 1).Name.Should().Be("Apple v2");
        data.Should().NotContain(p => p.Id == 99);
    }

    // ── DeleteRange O(N+M) ───────────────────────────────────────────────────

    [Fact]
    public void DeleteRange_500Entities_RemovesAll()
    {
        var data = Enumerable.Range(1, 1000).Select(i => new TestProduct
        {
            Id = i, Name = $"P{i}", Category = "Cat", Price = i * 1m, Stock = i, IsActive = true
        }).ToList();
        var ev = Ev(data);

        var toDelete = data.Take(500).ToList();
        int deleted = ev.DeleteRange(toDelete, data);

        deleted.Should().Be(500);
        data.Should().HaveCount(500);
    }

    [Fact]
    public void DeleteRange_NonExistentEntities_ReturnsZero()
    {
        var data = Seed();
        var ev = Ev(data);

        var ghosts = new List<TestProduct>
        {
            new() { Id = 99, Name = "Ghost", Category = "X", Price = 0m, Stock = 0, IsActive = false }
        };

        int deleted = ev.DeleteRange(ghosts, data);

        deleted.Should().Be(0);
        data.Should().HaveCount(10);
    }

    // ── SaveChanges HashSet ───────────────────────────────────────────────────

    [Fact]
    public void SaveChanges_AddAndDelete_StoreReflectsChanges()
    {
        var data = Seed();
        var ev = Ev(data);

        ev.Add(new TestProduct { Id = 50, Name = "Kiwi", Category = "Fruit", Price = 2m, Stock = 40, IsActive = true });
        ev.Delete(data.First(p => p.Id == 1)); // remove Apple

        ev.SaveChanges();

        ev.EvaluateCount(null).Should().Be(10); // 10 - 1 + 1 = 10
        ev.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 50)).Should().NotBeNull();
        ev.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 1)).Should().BeNull();
    }

    [Fact]
    public void SaveChanges_BatchDelete_AllRemovedInOnePass()
    {
        var data = Seed();
        var ev = Ev(data);

        // Stage deletion of all Sweet items (Ids 4, 8, 10)
        foreach (var item in data.Where(p => p.Category == "Sweet").ToList())
            ev.Delete(item);

        ev.SaveChanges();

        ev.EvaluateCount(null).Should().Be(7); // 10 - 3
        ev.EvaluateAny(null, new ValiFlow<TestProduct>().EqualTo(p => p.Category, "Sweet")).Should().BeFalse();
    }

    // ── EvaluateTopByGroup (TOrderKey genérico) ──────────────────────────────

    [Fact]
    public void EvaluateTopByGroup_WithDecimalOrderBy_TypeSafeNoBoxing()
    {
        var data = Seed();
        var ev = Ev(data);

        // Top 1 per category ordered by Price ascending (cheapest first)
        var result = ev.EvaluateTopByGroup<string, decimal>(
            data, p => p.Category, count: 1, orderBy: p => p.Price, ascending: true);

        result["Fruit"].Single().Name.Should().Be("Banana");   // 0.80 cheapest
        result["Sweet"].Single().Name.Should().Be("Donut");    // 2.50 cheapest
        result["Dairy"].Single().Name.Should().Be("Ice");      // 1.20 cheapest
        result["Veggie"].Single().Name.Should().Be("Carrot");  // only one
    }

    [Fact]
    public void EvaluateTopByGroup_NoOrderBy_ReturnsNaturalOrderTopN()
    {
        var data = Seed();
        var ev = Ev(data);

        var result = ev.EvaluateTopByGroup<string, int>(data, p => p.Category, count: 2);

        result["Fruit"].Should().HaveCount(2);
        result["Veggie"].Should().HaveCount(1); // only 1 item in Veggie
        result["Sweet"].Should().HaveCount(2);
        result["Dairy"].Should().HaveCount(2);
    }

    [Fact]
    public void EvaluateTopByGroup_WithFilter_OnlyActiveInGroups()
    {
        var data = Seed();
        var ev = Ev(data);
        var active = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Active Fruit: Apple, Banana, Fig (3). Top 2 by price desc → Fig(4.50), Apple(1.50)
        var result = ev.EvaluateTopByGroup<string, decimal>(
            data, p => p.Category, count: 2,
            orderBy: p => p.Price, ascending: false, valiFlow: active);

        result["Fruit"].Should().HaveCount(2);
        result["Fruit"][0].Name.Should().Be("Fig");
        result["Fruit"][1].Name.Should().Be("Apple");
    }
}
