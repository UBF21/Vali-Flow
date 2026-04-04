using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;

namespace Vali_Flow.InMemory.Tests;

/// <summary>
/// Final coverage gaps:
///   Section 1 — SetValiFlow (4 tests)
///   Section 2 — negateCondition in remaining grouped methods (7 tests)
///   Section 3 — Null guards for keySelector (3 tests)
/// </summary>
public sealed class ValiFlowInMemoryFinalCoverageTests
{
    // ── Seed ─────────────────────────────────────────────────────────────────
    // Id  Name  Category     Price  Stock  Active
    //  1  A1    Electronics  100    5      true
    //  2  A2    Electronics  200    3      true
    //  3  B1    Books         30   10      true
    //  4  B2    Books         50    8      false
    //  5  C1    Clothing      80    2      false

    private static List<TestProduct> MakeData() => new()
    {
        new() { Id = 1, Name = "A1", Category = "Electronics", Price = 100m, Stock = 5,  IsActive = true  },
        new() { Id = 2, Name = "A2", Category = "Electronics", Price = 200m, Stock = 3,  IsActive = true  },
        new() { Id = 3, Name = "B1", Category = "Books",       Price = 30m,  Stock = 10, IsActive = true  },
        new() { Id = 4, Name = "B2", Category = "Books",       Price = 50m,  Stock = 8,  IsActive = false },
        new() { Id = 5, Name = "C1", Category = "Clothing",    Price = 80m,  Stock = 2,  IsActive = false },
    };

    private static ValiFlowEvaluator<TestProduct, int> Ev(List<TestProduct> data) =>
        new(data, null, p => p.Id);

    // ── Section 1: SetValiFlow ────────────────────────────────────────────────

    [Fact]
    public void SetValiFlow_WithActiveFilter_EvaluateCountReturnsOnlyActiveCount()
    {
        var data = MakeData();
        var ev = Ev(data);

        ev.SetValiFlow(new ValiFlow<TestProduct>().IsTrue(p => p.IsActive));

        // 3 active products (Id 1, 2, 3)
        ev.EvaluateCount(null).Should().Be(3);
    }

    [Fact]
    public void SetValiFlow_WithActiveFilter_GetFirstReturnsFirstActiveProduct()
    {
        var data = MakeData();
        var ev = Ev(data);

        ev.SetValiFlow(new ValiFlow<TestProduct>().IsTrue(p => p.IsActive));

        var first = ev.GetFirst(null);
        first.Should().NotBeNull();
        first!.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SetValiFlow_ChangedTwice_SecondFilterTakesEffect()
    {
        var data = MakeData();
        var ev = Ev(data);

        // First filter: active only → 3 results
        ev.SetValiFlow(new ValiFlow<TestProduct>().IsTrue(p => p.IsActive));
        ev.EvaluateCount(null).Should().Be(3);

        // Second filter: inactive only → 2 results
        ev.SetValiFlow(new ValiFlow<TestProduct>().IsFalse(p => p.IsActive));
        ev.EvaluateCount(null).Should().Be(2);
    }

    [Fact]
    public void SetValiFlow_NullArgument_ThrowsArgumentNullException()
    {
        var data = MakeData();
        var ev = Ev(data);

        var act = () => ev.SetValiFlow(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── Section 2: negateCondition in remaining grouped methods ───────────────
    // Filter: IsTrue(p => p.IsActive) + negateCondition: true → INACTIVE products (Id 4, 5)
    // Id=4 Books/50, Id=5 Clothing/80

    [Fact]
    public void EvaluateSumByGroup_NegateCondition_SumsOnlyInactiveProducts()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = ev.EvaluateSumByGroup(data, p => p.Category, p => p.Price,
            valiFlow: filter, negateCondition: true);

        result.Should().HaveCount(2);
        result["Books"].Should().Be(50m);
        result["Clothing"].Should().Be(80m);
        result.Should().NotContainKey("Electronics");
    }

    [Fact]
    public void EvaluateMinByGroup_NegateCondition_MinOnlyInactiveProducts()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = ev.EvaluateMinByGroup(data, p => p.Category, p => p.Price,
            valiFlow: filter, negateCondition: true);

        result.Should().HaveCount(2);
        result["Books"].Should().Be(50m);
        result["Clothing"].Should().Be(80m);
    }

    [Fact]
    public void EvaluateMaxByGroup_NegateCondition_MaxOnlyInactiveProducts()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = ev.EvaluateMaxByGroup(data, p => p.Category, p => p.Price,
            valiFlow: filter, negateCondition: true);

        result.Should().HaveCount(2);
        result["Books"].Should().Be(50m);
        result["Clothing"].Should().Be(80m);
    }

    [Fact]
    public void EvaluateAverageByGroup_NegateCondition_AverageOnlyInactiveProducts()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = ev.EvaluateAverageByGroup(data, p => p.Category, p => p.Price,
            valiFlow: filter, negateCondition: true);

        result.Should().HaveCount(2);
        result["Books"].Should().Be(50m);    // single item → avg = item value
        result["Clothing"].Should().Be(80m); // single item → avg = item value
    }

    [Fact]
    public void EvaluateTopByGroup_NegateCondition_TopPerGroupOfInactiveProducts()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = ev.EvaluateTopByGroup<string, decimal>(
            data, p => p.Category, count: 1,
            orderBy: p => p.Price, ascending: true,
            valiFlow: filter, negateCondition: true);

        result.Should().HaveCount(2);
        result["Books"].Should().HaveCount(1);
        result["Books"].Single().Id.Should().Be(4);
        result["Clothing"].Should().HaveCount(1);
        result["Clothing"].Single().Id.Should().Be(5);
    }

    [Fact]
    public void EvaluateDuplicatesByGroup_NegateCondition_NoDuplicatesAmongInactive()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Each inactive category (Books, Clothing) has exactly 1 inactive item → no duplicates
        var result = ev.EvaluateDuplicatesByGroup(data, p => p.Category,
            valiFlow: filter, negateCondition: true);

        result.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateUniquesByGroup_NegateCondition_EachInactiveCategoryIsUnique()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Each inactive category has exactly 1 item → both are unique
        var result = ev.EvaluateUniquesByGroup(data, p => p.Category,
            valiFlow: filter, negateCondition: true);

        result.Should().HaveCount(2);
        result.Should().ContainKey("Books");
        result.Should().ContainKey("Clothing");
        result["Books"].Id.Should().Be(4);
        result["Clothing"].Id.Should().Be(5);
    }

    // ── Section 3: Null guards for keySelector ────────────────────────────────

    [Fact]
    public void EvaluateSumByGroup_NullKeySelector_ThrowsArgumentNullException()
    {
        var data = MakeData();
        var ev = Ev(data);

        var act = () => ev.EvaluateSumByGroup<string, decimal>(data, null!, p => p.Price);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EvaluateMinByGroup_NullKeySelector_ThrowsArgumentNullException()
    {
        var data = MakeData();
        var ev = Ev(data);

        var act = () => ev.EvaluateMinByGroup<string, decimal>(data, null!, p => p.Price);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EvaluateMaxByGroup_NullKeySelector_ThrowsArgumentNullException()
    {
        var data = MakeData();
        var ev = Ev(data);

        var act = () => ev.EvaluateMaxByGroup<string, decimal>(data, null!, p => p.Price);

        act.Should().Throw<ArgumentNullException>();
    }
}
