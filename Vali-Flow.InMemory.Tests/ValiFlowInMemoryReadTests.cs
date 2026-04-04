using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;

namespace Vali_Flow.InMemory.Tests;

public sealed class ValiFlowInMemoryReadTests
{
    // -----------------------------------------------------------------------
    // Seed data — 10 products: 4 Fruit, 3 Sweet, 2 Dairy, 1 Veggie
    // Active: 1,2,3,5,6,8,9  (7)   Inactive: 4,7,10  (3)
    // -----------------------------------------------------------------------
    private static readonly List<TestProduct> Seed = new()
    {
        new() { Id = 1,  Name = "Apple",  Category = "Fruit",  Price = 1.5m,  Stock = 100, IsActive = true  },
        new() { Id = 2,  Name = "Banana", Category = "Fruit",  Price = 0.8m,  Stock = 150, IsActive = true  },
        new() { Id = 3,  Name = "Carrot", Category = "Veggie", Price = 0.5m,  Stock = 200, IsActive = true  },
        new() { Id = 4,  Name = "Donut",  Category = "Sweet",  Price = 2.5m,  Stock = 50,  IsActive = false },
        new() { Id = 5,  Name = "Egg",    Category = "Dairy",  Price = 3.0m,  Stock = 80,  IsActive = true  },
        new() { Id = 6,  Name = "Fig",    Category = "Fruit",  Price = 4.5m,  Stock = 30,  IsActive = true  },
        new() { Id = 7,  Name = "Grape",  Category = "Fruit",  Price = 3.2m,  Stock = 60,  IsActive = false },
        new() { Id = 8,  Name = "Honey",  Category = "Sweet",  Price = 7.0m,  Stock = 20,  IsActive = true  },
        new() { Id = 9,  Name = "Ice",    Category = "Dairy",  Price = 1.2m,  Stock = 90,  IsActive = true  },
        new() { Id = 10, Name = "Jam",    Category = "Sweet",  Price = 2.8m,  Stock = 45,  IsActive = false },
    };

    private static ValiFlowEvaluator<TestProduct, int> CreateEvaluator()
        => new(Seed, null, p => p.Id);

    // -----------------------------------------------------------------------
    // Evaluate(entity, filter) — single-entity predicate check
    // -----------------------------------------------------------------------

    [Fact]
    public void Evaluate_ActiveProduct_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);
        var product = Seed.First(p => p.Id == 1); // Apple, IsActive = true

        bool result = evaluator.Evaluate(product, filter);

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_InactiveProductWithActiveFilter_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);
        var product = Seed.First(p => p.Id == 4); // Donut, IsActive = false

        bool result = evaluator.Evaluate(product, filter);

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithNegateCondition_InvertsResult()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);
        var activeProduct = Seed.First(p => p.Id == 1);

        bool result = evaluator.Evaluate(activeProduct, filter, negateCondition: true);

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_ProductMatchingPriceFilter_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().GreaterThan(p => p.Price, 5m);
        var product = Seed.First(p => p.Id == 8); // Honey, Price = 7.0m

        bool result = evaluator.Evaluate(product, filter);

        result.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // EvaluateAny(entities, filter)
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateAny_WithMatchingFilter_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().GreaterThan(p => p.Price, 6m);

        bool result = evaluator.EvaluateAny(null, filter);

        result.Should().BeTrue(); // Honey = 7.0m
    }

    [Fact]
    public void EvaluateAny_WithNoMatchingFilter_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().GreaterThan(p => p.Price, 100m);

        bool result = evaluator.EvaluateAny(null, filter);

        result.Should().BeFalse();
    }

    [Fact]
    public void EvaluateAny_WithNullFilter_ReturnsTrueWhenStoreNonEmpty()
    {
        var evaluator = CreateEvaluator();

        bool result = evaluator.EvaluateAny(null, null);

        result.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // EvaluateCount(entities, filter)
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateCount_ActiveProducts_ReturnsSeven()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        int count = evaluator.EvaluateCount(null, filter);

        count.Should().Be(7);
    }

    [Fact]
    public void EvaluateCount_FruitCategory_ReturnsFour()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().EqualTo(p => p.Category, "Fruit");

        int count = evaluator.EvaluateCount(null, filter);

        count.Should().Be(4);
    }

    [Fact]
    public void EvaluateCount_WithNegateCondition_ReturnsInactiveCount()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // negateCondition inverts → counts inactive products
        int count = evaluator.EvaluateCount(null, filter, negateCondition: true);

        count.Should().Be(3);
    }

    [Fact]
    public void EvaluateCount_NullFilter_ReturnsAllTen()
    {
        var evaluator = CreateEvaluator();

        int count = evaluator.EvaluateCount(null, null);

        count.Should().Be(10);
    }

    // -----------------------------------------------------------------------
    // GetFirst / GetFirstFailed
    // -----------------------------------------------------------------------

    [Fact]
    public void GetFirst_ActiveProductsFilteredByPrice_ReturnsFirstActiveOverTwoAndHalf()
    {
        var evaluator = CreateEvaluator();
        // Filter: active AND price > 2.5 → Egg(3.0), Fig(4.5), Honey(7.0) — first in seed order is Egg (id=5)
        var filter = new ValiFlow<TestProduct>()
            .IsTrue(p => p.IsActive)
            .GreaterThan(p => p.Price, 2.5m);

        TestProduct? result = evaluator.GetFirst(null, filter);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5); // Egg
    }

    [Fact]
    public void GetFirst_NoFilterNoOrderBy_ReturnsFirstInStore()
    {
        var evaluator = CreateEvaluator();

        TestProduct? result = evaluator.GetFirst(null);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1); // Apple
    }

    [Fact]
    public void GetFirstFailed_ActiveFilter_ReturnsFirstInactive()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // GetFirstFailed returns the first item that fails the condition (i.e., IsActive == false)
        TestProduct? result = evaluator.GetFirstFailed(null, filter);

        result.Should().NotBeNull();
        result!.Id.Should().Be(4); // Donut, first inactive in seed order
    }

    [Fact]
    public void GetFirst_HighPriceFilter_ReturnsHoney()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().GreaterThan(p => p.Price, 6m);

        TestProduct? result = evaluator.GetFirst(null, filter);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Honey");
    }

    // -----------------------------------------------------------------------
    // GetLast / GetLastFailed
    // -----------------------------------------------------------------------

    [Fact]
    public void GetLast_ActiveProducts_ReturnsLastActiveInStore()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // GetLast<TKey>(entities, orderBy, ascending, thenBys, valiFlow, negateCondition)
        // No ordering → keeps natural seed order; last active in seed is Ice (id=9)
        TestProduct? result = evaluator.GetLast<int>(null, null, true, null, filter);

        result.Should().NotBeNull();
        result!.Id.Should().Be(9); // Ice, last active in seed order
    }

    [Fact]
    public void GetLast_AllProductsOrderedByPriceDescending_ReturnsLowestPrice()
    {
        var evaluator = CreateEvaluator();

        // descending order: Honey(7.0),...,Carrot(0.5) → last = Carrot
        TestProduct? result = evaluator.GetLast(null, (TestProduct p) => p.Price, ascending: false);

        result.Should().NotBeNull();
        result!.Id.Should().Be(3); // Carrot, 0.5m
    }

    [Fact]
    public void GetLastFailed_ActiveFilter_ReturnsLastInactive()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // GetLastFailed<TKey>(entities, orderBy, ascending, thenBys, valiFlow, negateCondition)
        TestProduct? result = evaluator.GetLastFailed<int>(null, null, true, null, filter);

        result.Should().NotBeNull();
        result!.Id.Should().Be(10); // Jam, last inactive
    }

    // -----------------------------------------------------------------------
    // EvaluateAll / EvaluateAllFailed
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateAll_ActiveProducts_ReturnsSevenItems()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        IEnumerable<TestProduct> result = evaluator.EvaluateAll<int>(null, valiFlow: filter);

        result.Should().HaveCount(7);
    }

    [Fact]
    public void EvaluateAll_OrderedByStockDescending_FirstHasHighestStock()
    {
        var evaluator = CreateEvaluator();

        IEnumerable<TestProduct> result = evaluator.EvaluateAll(null, orderBy: (TestProduct p) => p.Stock, ascending: false);

        result.First().Id.Should().Be(3); // Carrot, Stock = 200
    }

    [Fact]
    public void EvaluateAllFailed_ActiveFilter_ReturnsThreeInactiveItems()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        IEnumerable<TestProduct> result = evaluator.EvaluateAllFailed<int>(null, valiFlow: filter);

        result.Should().HaveCount(3);
        result.Select(p => p.Id).Should().BeEquivalentTo(new[] { 4, 7, 10 });
    }

    [Fact]
    public void EvaluateAll_FruitCategory_ReturnsFourItems()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().EqualTo(p => p.Category, "Fruit");

        IEnumerable<TestProduct> result = evaluator.EvaluateAll<int>(null, valiFlow: filter);

        result.Should().HaveCount(4);
    }

    // -----------------------------------------------------------------------
    // EvaluatePaged
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluatePaged_Page1Size3_ReturnsFirstThreeProducts()
    {
        var evaluator = CreateEvaluator();

        IEnumerable<TestProduct> result = evaluator.EvaluatePaged<int>(
            null, page: 1, pageSize: 3, orderBy: p => p.Id, ascending: true);

        result.Should().HaveCount(3);
        result.Select(p => p.Id).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void EvaluatePaged_Page2Size3_ReturnsSecondPage()
    {
        var evaluator = CreateEvaluator();

        IEnumerable<TestProduct> result = evaluator.EvaluatePaged<int>(
            null, page: 2, pageSize: 3, orderBy: p => p.Id, ascending: true);

        result.Should().HaveCount(3);
        result.Select(p => p.Id).Should().Equal(4, 5, 6);
    }

    [Fact]
    public void EvaluatePaged_LastPage_ReturnsRemainder()
    {
        var evaluator = CreateEvaluator();

        // 10 products, pageSize=3 → page 4 has 1 item
        IEnumerable<TestProduct> result = evaluator.EvaluatePaged<int>(
            null, page: 4, pageSize: 3, orderBy: p => p.Id, ascending: true);

        result.Should().HaveCount(1);
        result.Single().Id.Should().Be(10);
    }

    [Fact]
    public void EvaluatePaged_WithActiveFilter_ReturnsPagedSubset()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // 7 active, ordered by id: 1,2,3,5,6,8,9 → page 2, size 3 → 5,6,8
        IEnumerable<TestProduct> result = evaluator.EvaluatePaged(
            null, page: 2, pageSize: 3,
            orderBy: (TestProduct p) => p.Id, ascending: true,
            valiFlow: filter);

        result.Should().HaveCount(3);
        result.Select(p => p.Id).Should().Equal(5, 6, 8);
    }

    // -----------------------------------------------------------------------
    // EvaluateDistinct
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateDistinct_ByCategory_ReturnsFourDistinctGroups()
    {
        var evaluator = CreateEvaluator();

        IEnumerable<TestProduct> result = evaluator.EvaluateDistinct<string>(
            null, selector: p => p.Category);

        result.Should().HaveCount(4);
    }

    [Fact]
    public void EvaluateDistinct_ByCategory_ContainsOneRepresentativePerCategory()
    {
        var evaluator = CreateEvaluator();

        IEnumerable<TestProduct> result = evaluator.EvaluateDistinct<string>(
            null, selector: p => p.Category);

        var categories = result.Select(p => p.Category).Distinct().ToList();
        categories.Should().BeEquivalentTo(new[] { "Fruit", "Veggie", "Sweet", "Dairy" });
    }

    [Fact]
    public void EvaluateDistinct_ActiveOnly_ReturnsDistinctFromActiveSubset()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Active products span all 4 categories (Fruit, Veggie, Dairy, Sweet all have at least 1 active)
        IEnumerable<TestProduct> result = evaluator.EvaluateDistinct(
            null, selector: (TestProduct p) => p.Category, valiFlow: filter);

        result.Should().HaveCount(4);
    }

    // -----------------------------------------------------------------------
    // EvaluateTop
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateTop_TopThreeByPriceDescending_ReturnsMostExpensive()
    {
        var evaluator = CreateEvaluator();

        IEnumerable<TestProduct> result = evaluator.EvaluateTop(
            null, count: 3,
            orderBy: (TestProduct p) => p.Price, ascending: false);

        // Honey(7.0), Fig(4.5), Grape(3.2)
        result.Should().HaveCount(3);
        result.First().Id.Should().Be(8); // Honey
    }

    [Fact]
    public void EvaluateTop_TopOneByPriceAscending_ReturnsCheapest()
    {
        var evaluator = CreateEvaluator();

        IEnumerable<TestProduct> result = evaluator.EvaluateTop(
            null, count: 1,
            orderBy: (TestProduct p) => p.Price, ascending: true);

        result.Should().HaveCount(1);
        result.Single().Id.Should().Be(3); // Carrot, 0.5m
    }

    [Fact]
    public void EvaluateTop_WithActiveFilter_ReturnsTopFromActiveSubset()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        IEnumerable<TestProduct> result = evaluator.EvaluateTop(
            null, count: 2,
            orderBy: (TestProduct p) => p.Stock, ascending: false,
            valiFlow: filter);

        // Active by stock desc: Carrot(200), Banana(150), Apple(100), Ice(90), Egg(80), Fig(30), Honey(20)
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(3);  // Carrot
        result.Last().Id.Should().Be(2);   // Banana
    }

    [Fact]
    public void EvaluateTop_CountGreaterThanSource_ReturnsAllMatching()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().EqualTo(p => p.Category, "Dairy");

        IEnumerable<TestProduct> result = evaluator.EvaluateTop<int>(
            null, count: 100, valiFlow: filter);

        result.Should().HaveCount(2); // Only 2 Dairy products exist
    }
}
