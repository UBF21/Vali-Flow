using FluentAssertions;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Tests.Models;
using Xunit;

namespace Vali_Flow.InMemory.Tests;

/// <summary>
/// Covers remaining gaps:
///   Section 1 — EvaluateAverageByGroup fractional precision (4 tests)
///   Section 2 — EvaluateDistinct with ordering (4 tests)
///   Section 3 — EvaluateDuplicates with ordering (2 tests)
///   Section 4 — EvaluatePaged with negateCondition (3 tests)
/// </summary>
public sealed class ValiFlowInMemoryLastGapsTests
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

    // ── Section 1: EvaluateAverageByGroup precision ──────────────────────────

    [Fact]
    public void EvaluateAverageByGroup_Electronics_ReturnsExact150()
    {
        var data = MakeData();
        var ev = Ev(data);

        var result = ev.EvaluateAverageByGroup(
            null,
            p => p.Category,
            p => p.Price);

        result["Electronics"].Should().Be(150.0m);
    }

    [Fact]
    public void EvaluateAverageByGroup_Books_ReturnsExact40()
    {
        var data = MakeData();
        var ev = Ev(data);

        var result = ev.EvaluateAverageByGroup(
            null,
            p => p.Category,
            p => p.Price);

        result["Books"].Should().Be(40.0m);
    }

    [Fact]
    public void EvaluateAverageByGroup_SingleItemGroup_ReturnsItemPrice()
    {
        var data = MakeData();
        var ev = Ev(data);

        var result = ev.EvaluateAverageByGroup(
            null,
            p => p.Category,
            p => p.Price);

        // Clothing has only one item: C1 at 80
        result["Clothing"].Should().Be(80.0m);
    }

    [Fact]
    public void EvaluateAverageByGroup_WithActiveFilter_BooksHasOnlyB1()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = ev.EvaluateAverageByGroup(
            null,
            p => p.Category,
            p => p.Price,
            valiFlow: filter);

        // Active Books: only B1 (30) — B2 is inactive
        result["Books"].Should().Be(30.0m);
        // Clothing is inactive so it should not appear
        result.ContainsKey("Clothing").Should().BeFalse();
    }

    // ── Section 2: EvaluateDistinct with ordering ─────────────────────────────

    [Fact]
    public void EvaluateDistinct_ByCategory_OrderedByCategoryAscending_ReturnsSortedCategories()
    {
        var data = MakeData();
        var ev = Ev(data);

        // TKey = string: selector and orderBy both use Category
        var result = ev.EvaluateDistinct(
            null,
            selector: (Func<TestProduct, string>)(p => p.Category),
            orderBy: (Func<TestProduct, string>)(p => p.Category),
            ascending: true).ToList();

        // Three distinct categories, sorted alphabetically
        result.Should().HaveCount(3);
        result.Select(p => p.Category).Should().BeInAscendingOrder();
        result[0].Category.Should().Be("Books");
        result[1].Category.Should().Be("Clothing");
        result[2].Category.Should().Be("Electronics");
    }

    [Fact]
    public void EvaluateDistinct_ByCategory_OrderedByCategoryDescending_ReturnsReverseSortedCategories()
    {
        var data = MakeData();
        var ev = Ev(data);

        // TKey = string: selector and orderBy both use Category, descending
        var result = ev.EvaluateDistinct(
            null,
            selector: (Func<TestProduct, string>)(p => p.Category),
            orderBy: (Func<TestProduct, string>)(p => p.Category),
            ascending: false).ToList();

        // Three distinct categories, sorted reverse alphabetically
        result.Should().HaveCount(3);
        result[0].Category.Should().Be("Electronics");
        result[1].Category.Should().Be("Clothing");
        result[2].Category.Should().Be("Books");
    }

    [Fact]
    public void EvaluateDistinct_ByCategory_WithActiveFilter_ExcludesInactiveOnlyCategories()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Active products: A1(Electronics), A2(Electronics), B1(Books) — Clothing only has inactive items
        var result = ev.EvaluateDistinct(
            null,
            selector: (Func<TestProduct, string>)(p => p.Category),
            orderBy: (Func<TestProduct, string>)(p => p.Category),
            ascending: true,
            valiFlow: filter).ToList();

        result.Should().HaveCount(2);
        result.Select(p => p.Category).Should().Contain("Electronics");
        result.Select(p => p.Category).Should().Contain("Books");
        result.Select(p => p.Category).Should().NotContain("Clothing");
    }

    [Fact]
    public void EvaluateDistinct_ByCategoryWithNoOrderBy_ReturnsOneItemPerCategory()
    {
        var data = MakeData();
        var ev = Ev(data);

        var result = ev.EvaluateDistinct(
            null,
            selector: (Func<TestProduct, string>)(p => p.Category)).ToList();

        result.Should().HaveCount(3);
        result.Select(p => p.Category).Should().OnlyHaveUniqueItems();
    }

    // ── Section 3: EvaluateDuplicates with ordering ───────────────────────────

    [Fact]
    public void EvaluateDuplicates_ByCategory_OrderedByCategoryAscending_ReturnsBooksBeforeElectronics()
    {
        var data = MakeData();
        var ev = Ev(data);

        // TKey = string for both selector and orderBy
        var result = ev.EvaluateDuplicates(
            null,
            selector: (Func<TestProduct, string>)(p => p.Category),
            orderBy: (Func<TestProduct, string>)(p => p.Category),
            ascending: true).ToList();

        // Clothing has only 1 item — no duplicate
        // Duplicates: Books (B1, B2) and Electronics (A1, A2)
        // Ordered by Category asc: Books items first, then Electronics items
        result.Should().HaveCount(4);
        result[0].Category.Should().Be("Books");
        result[1].Category.Should().Be("Books");
        result[2].Category.Should().Be("Electronics");
        result[3].Category.Should().Be("Electronics");
    }

    [Fact]
    public void EvaluateDuplicates_ByCategory_OrderedByCategoryDescending_ReturnsElectronicsFirst()
    {
        var data = MakeData();
        var ev = Ev(data);

        // TKey = string for both selector and orderBy, descending
        var result = ev.EvaluateDuplicates(
            null,
            selector: (Func<TestProduct, string>)(p => p.Category),
            orderBy: (Func<TestProduct, string>)(p => p.Category),
            ascending: false).ToList();

        // Electronics alphabetically after Books, so descending: Electronics first
        result.Should().HaveCount(4);
        result[0].Category.Should().Be("Electronics");
        result[1].Category.Should().Be("Electronics");
        result[2].Category.Should().Be("Books");
        result[3].Category.Should().Be("Books");
    }

    // ── Section 4: EvaluatePaged with negateCondition ─────────────────────────

    [Fact]
    public void EvaluatePaged_WithNegateCondition_Page1Size1_ReturnsFirstInactiveProduct()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // negateCondition:true inverts IsActive → returns inactive products (B2, C1)
        var result = ev.EvaluatePaged(
            null,
            page: 1,
            pageSize: 1,
            orderBy: (Func<TestProduct, decimal>)(p => p.Price),
            ascending: true,
            valiFlow: filter,
            negateCondition: true).ToList();

        // Inactive by Price asc: C1(80), B2(50) → ordered: B2(50) first
        result.Should().HaveCount(1);
        result[0].IsActive.Should().BeFalse();
        result[0].Price.Should().Be(50m);
    }

    [Fact]
    public void EvaluatePaged_WithNegateCondition_Page2Size1_ReturnsSecondInactiveProduct()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = ev.EvaluatePaged(
            null,
            page: 2,
            pageSize: 1,
            orderBy: (Func<TestProduct, decimal>)(p => p.Price),
            ascending: true,
            valiFlow: filter,
            negateCondition: true).ToList();

        // Page 2: the second inactive product by Price asc → C1(80)
        result.Should().HaveCount(1);
        result[0].IsActive.Should().BeFalse();
        result[0].Price.Should().Be(80m);
    }

    [Fact]
    public void EvaluatePaged_WithNegateCondition_Page1Size10_ReturnsBothInactiveProducts()
    {
        var data = MakeData();
        var ev = Ev(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = ev.EvaluatePaged(
            null,
            page: 1,
            pageSize: 10,
            orderBy: (Func<TestProduct, decimal>)(p => p.Price),
            ascending: true,
            valiFlow: filter,
            negateCondition: true).ToList();

        // All inactive: B2(50) and C1(80)
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => !p.IsActive);
    }
}
