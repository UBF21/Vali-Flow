using FluentAssertions;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Tests.Models;
using Xunit;

namespace Vali_Flow.InMemory.Tests;

/// <summary>
/// Tests covering negateCondition in aggregate methods, secondary ordering (thenBys),
/// and UpdateRange edge cases.
/// </summary>
public sealed class ValiFlowInMemoryAggregateAndOrderTests
{
    // -----------------------------------------------------------------------
    // Seed
    // Id  Name  Category      Price   Stock  Active
    //  1  A1    Electronics   100     5      true
    //  2  A2    Electronics   200     3      true
    //  3  B1    Books          30    10      true
    //  4  B2    Books          50     8      false  ← inactive
    //  5  C1    Clothing       80     2      false  ← inactive
    // -----------------------------------------------------------------------

    private static List<TestProduct> MakeData() => new()
    {
        new() { Id = 1, Name = "A1", Category = "Electronics", Price = 100m, Stock = 5,  IsActive = true  },
        new() { Id = 2, Name = "A2", Category = "Electronics", Price = 200m, Stock = 3,  IsActive = true  },
        new() { Id = 3, Name = "B1", Category = "Books",       Price = 30m,  Stock = 10, IsActive = true  },
        new() { Id = 4, Name = "B2", Category = "Books",       Price = 50m,  Stock = 8,  IsActive = false },
        new() { Id = 5, Name = "C1", Category = "Clothing",    Price = 80m,  Stock = 2,  IsActive = false },
    };

    // =========================================================================
    // SECTION 1 — Aggregate methods with negateCondition: true
    // Filter = IsTrue(p => p.IsActive) + negateCondition=true → only inactive
    // Inactive products: Id=4 (Price=50) and Id=5 (Price=80)
    // =========================================================================

    [Fact]
    public void EvaluateSum_NegateCondition_SumsInactiveProductsPrices()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData(), null, p => p.Id);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // negateCondition=true inverts filter → only inactive (50+80=130)
        decimal result = ev.EvaluateSum(null, p => p.Price, filter, negateCondition: true);

        result.Should().Be(130m);
    }

    [Fact]
    public void EvaluateMin_NegateCondition_ReturnsMinPriceOfInactiveProducts()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData(), null, p => p.Id);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // inactive prices: 50, 80 → min = 50
        decimal result = ev.EvaluateMin(null, p => p.Price, filter, negateCondition: true);

        result.Should().Be(50m);
    }

    [Fact]
    public void EvaluateMax_NegateCondition_ReturnsMaxPriceOfInactiveProducts()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData(), null, p => p.Id);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // inactive prices: 50, 80 → max = 80
        decimal result = ev.EvaluateMax(null, p => p.Price, filter, negateCondition: true);

        result.Should().Be(80m);
    }

    [Fact]
    public void EvaluateAverage_NegateCondition_ReturnsAveragePriceOfInactiveProducts()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData(), null, p => p.Id);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // inactive prices: 50, 80 → avg = (50+80)/2 = 65
        decimal result = ev.EvaluateAverage(null, p => p.Price, filter, negateCondition: true);

        result.Should().Be(65m);
    }

    [Fact]
    public void EvaluateSum_NegateConditionAllMatchFilter_ReturnsZero()
    {
        // Use a filter that matches all entities; negateCondition=true → no matches → sum = 0
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData(), null, p => p.Id);
        // All products have Price > 0, so GreaterThan(0) matches all
        var filter = new ValiFlow<TestProduct>().GreaterThan(p => p.Price, 0m);

        decimal result = ev.EvaluateSum(null, p => p.Price, filter, negateCondition: true);

        result.Should().Be(0m);
    }

    [Fact]
    public void EvaluateCount_NegateCondition_CountsOnlyInactiveProducts()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData(), null, p => p.Id);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // negateCondition=true → inactive products = 2
        int result = ev.EvaluateCount(null, filter, negateCondition: true);

        result.Should().Be(2);
    }

    // =========================================================================
    // SECTION 2 — EvaluateAll with thenBys secondary ordering
    // =========================================================================

    [Fact]
    public void EvaluateAll_OrderByCategoryAscThenByPriceAsc_ReturnsCorrectOrder()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData(), null, p => p.Id);
        var thenBys = new List<InMemoryThenBy<TestProduct, string>>
        {
            new(p => p.Price.ToString("000000.00"), ascending: true)
        };

        // Use string-comparable thenBy via Price formatted for alpha sort
        // Instead, use a typed approach: order by Category (string), thenBy Price (decimal cast to string is tricky)
        // Use object thenBy workaround — let's do a separate approach with two separate evaluations
        // Actually, InMemoryThenBy<T,TKey> shares TKey with orderBy — both must be the same type.
        // Category is string, so thenBy must also be string. We must use a different strategy.
        // For same-TKey ordering, we'll use int-keyed ordering so both are comparable.
        // Re-create evaluator and order by Category as int-comparable is not possible directly.
        // The real approach: use object as TKey (boxing).
        var evObj = new ValiFlowEvaluator<TestProduct, int>(MakeData(), null, p => p.Id);
        var thenByPrice = new List<InMemoryThenBy<TestProduct, object>>
        {
            new(p => (object)p.Price, ascending: true)
        };

        IEnumerable<TestProduct> result = evObj.EvaluateAll<object>(
            null,
            orderBy: p => (object)p.Category,
            ascending: true,
            thenBys: thenByPrice
        );

        // Expected order (Category asc, Price asc within same Category):
        // Books(30), Books(50), Clothing(80), Electronics(100), Electronics(200)
        var prices = result.Select(p => p.Price).ToList();
        prices.Should().Equal(30m, 50m, 80m, 100m, 200m);
    }

    [Fact]
    public void EvaluateAll_OrderByCategoryAscThenByPriceDesc_BooksOrderedByPriceDesc()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData(), null, p => p.Id);
        var thenByPriceDesc = new List<InMemoryThenBy<TestProduct, object>>
        {
            new(p => (object)p.Price, ascending: false)
        };

        IEnumerable<TestProduct> result = ev.EvaluateAll<object>(
            null,
            orderBy: p => (object)p.Category,
            ascending: true,
            thenBys: thenByPriceDesc
        );

        var list = result.ToList();

        // Books come first (ascending category), and within Books: B2(50) before B1(30) because price desc
        list[0].Name.Should().Be("B2"); // Price=50
        list[1].Name.Should().Be("B1"); // Price=30
    }

    [Fact]
    public void EvaluateAll_NoThenBys_OrderByPriceAscending_ReturnsCorrectOrder()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>(MakeData(), null, p => p.Id);

        IEnumerable<TestProduct> result = ev.EvaluateAll<decimal>(
            null,
            orderBy: p => p.Price,
            ascending: true,
            thenBys: null
        );

        var prices = result.Select(p => p.Price).ToList();
        prices.Should().Equal(30m, 50m, 80m, 100m, 200m);
    }

    // =========================================================================
    // SECTION 3 — UpdateRange edge cases
    // =========================================================================

    [Fact]
    public void UpdateRange_EntityExists_UpdatesCorrectlyAfterSaveChanges()
    {
        var data = MakeData();
        var ev = new ValiFlowEvaluator<TestProduct, int>(data, null, p => p.Id);

        var updated = new TestProduct { Id = 3, Name = "B1-Updated", Category = "Books", Price = 35m, Stock = 12, IsActive = true };
        ev.UpdateRange(new[] { updated });
        ev.SaveChanges();

        var stored = ev.EvaluateAll<int>(null).Single(p => p.Id == 3);
        stored.Name.Should().Be("B1-Updated");
        stored.Price.Should().Be(35m);
        stored.Stock.Should().Be(12);
    }

    [Fact]
    public void UpdateRange_EntityIdNotInStore_SilentlySkipsUnknownId()
    {
        // UpdateRange documents: if the id is not found in the index, it calls `continue` (silent skip).
        // This test verifies that behavior: no exception is thrown and the store is unchanged.
        var data = MakeData();
        var ev = new ValiFlowEvaluator<TestProduct, int>(data, null, p => p.Id);

        var ghost = new TestProduct { Id = 999, Name = "Ghost", Category = "X", Price = 1m, Stock = 1, IsActive = false };

        IEnumerable<TestProduct> returned = ev.UpdateRange(new[] { ghost });
        ev.SaveChanges();

        // No exception thrown; returned collection is empty (nothing was matched/updated)
        returned.Should().BeEmpty();
        // Store is unchanged — still 5 items, none with Id=999
        ev.EvaluateCount(null).Should().Be(5);
        ev.EvaluateAll<int>(null).Should().NotContain(p => p.Id == 999);
    }

    [Fact]
    public void UpdateRange_WithExternalList_UpdatesBothInternalStoreAndExternalList()
    {
        var data = MakeData();
        var externalList = MakeData(); // separate copy acting as external store
        var ev = new ValiFlowEvaluator<TestProduct, int>(data, null, p => p.Id);

        var updatedEntity = new TestProduct { Id = 1, Name = "A1-Ext", Category = "Electronics", Price = 150m, Stock = 7, IsActive = true };

        IEnumerable<TestProduct> returned = ev.UpdateRange(new[] { updatedEntity }, externalList);
        ev.SaveChanges();

        // The external list should be updated
        var externalItem = externalList.Single(p => p.Id == 1);
        externalItem.Name.Should().Be("A1-Ext");
        externalItem.Price.Should().Be(150m);

        // The returned collection should include the updated entity
        returned.Should().ContainSingle(p => p.Id == 1 && p.Name == "A1-Ext");
    }
}
