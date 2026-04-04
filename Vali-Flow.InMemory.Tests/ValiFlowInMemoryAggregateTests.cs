using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;

namespace Vali_Flow.InMemory.Tests;

public sealed class ValiFlowInMemoryAggregateTests
{
    // -----------------------------------------------------------------------
    // Seed
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
    // EvaluateMin
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateMin_Stock_ReturnsLowestStock()
    {
        var evaluator = CreateEvaluator();

        int result = evaluator.EvaluateMin(null, p => p.Stock);

        result.Should().Be(20); // Honey
    }

    [Fact]
    public void EvaluateMin_Price_ReturnsCheapestPrice()
    {
        var evaluator = CreateEvaluator();

        decimal result = evaluator.EvaluateMin(null, p => p.Price);

        result.Should().Be(0.5m); // Carrot
    }

    [Fact]
    public void EvaluateMin_ActiveOnly_ReturnsMinAmongActiveProducts()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        int result = evaluator.EvaluateMin(null, p => p.Stock, filter);

        result.Should().Be(20); // Honey (active, Stock = 20)
    }

    // -----------------------------------------------------------------------
    // EvaluateMax
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateMax_Stock_ReturnsHighestStock()
    {
        var evaluator = CreateEvaluator();

        int result = evaluator.EvaluateMax(null, p => p.Stock);

        result.Should().Be(200); // Carrot
    }

    [Fact]
    public void EvaluateMax_Price_ReturnsMostExpensivePrice()
    {
        var evaluator = CreateEvaluator();

        decimal result = evaluator.EvaluateMax(null, p => p.Price);

        result.Should().Be(7.0m); // Honey
    }

    [Fact]
    public void EvaluateMax_InactiveOnly_ReturnsMaxAmongInactiveProducts()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsFalse(p => p.IsActive);

        int result = evaluator.EvaluateMax(null, p => p.Stock, filter);

        result.Should().Be(60); // Grape (inactive, Stock = 60)
    }

    // -----------------------------------------------------------------------
    // EvaluateSum
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateSum_Stock_ReturnsTotalStock()
    {
        var evaluator = CreateEvaluator();

        // 100+150+200+50+80+30+60+20+90+45 = 825
        int result = evaluator.EvaluateSum(null, p => p.Stock);

        result.Should().Be(825);
    }

    [Fact]
    public void EvaluateSum_ActiveStocks_ReturnsSumOfActiveOnly()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Active stocks: 100+150+200+80+30+20+90 = 670
        int result = evaluator.EvaluateSum(null, p => p.Stock, filter);

        result.Should().Be(670);
    }

    [Fact]
    public void EvaluateSum_Price_ReturnsTotalPrice()
    {
        var evaluator = CreateEvaluator();

        // 1.5+0.8+0.5+2.5+3.0+4.5+3.2+7.0+1.2+2.8 = 27.0
        decimal result = evaluator.EvaluateSum(null, p => p.Price);

        result.Should().Be(27.0m);
    }

    // -----------------------------------------------------------------------
    // EvaluateAverage
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateAverage_Stock_ReturnsCorrectAverage()
    {
        var evaluator = CreateEvaluator();

        // 825 / 10 = 82.5
        decimal result = evaluator.EvaluateAverage(null, p => p.Stock);

        result.Should().Be(82.5m);
    }

    [Fact]
    public void EvaluateAverage_Price_ReturnsCorrectAverage()
    {
        var evaluator = CreateEvaluator();

        // 27.0 / 10 = 2.7
        decimal result = evaluator.EvaluateAverage(null, p => p.Price);

        result.Should().Be(2.7m);
    }

    [Fact]
    public void EvaluateAverage_FruitPrices_ReturnsAverageForCategory()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().EqualTo(p => p.Category, "Fruit");

        // Fruit prices: 1.5+0.8+4.5+3.2 = 10.0 / 4 = 2.5
        decimal result = evaluator.EvaluateAverage(null, p => p.Price, filter);

        result.Should().Be(2.5m);
    }

    // -----------------------------------------------------------------------
    // EvaluateGrouped
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateGrouped_ByCategory_ReturnsFourGroups()
    {
        var evaluator = CreateEvaluator();

        Dictionary<string, List<TestProduct>> result = evaluator.EvaluateGrouped(null, p => p.Category);

        result.Should().HaveCount(4);
        result.Keys.Should().BeEquivalentTo(new[] { "Fruit", "Veggie", "Sweet", "Dairy" });
    }

    [Fact]
    public void EvaluateGrouped_ByCategory_FruitGroupHasFourItems()
    {
        var evaluator = CreateEvaluator();

        Dictionary<string, List<TestProduct>> result = evaluator.EvaluateGrouped(null, p => p.Category);

        result["Fruit"].Should().HaveCount(4);
    }

    [Fact]
    public void EvaluateGrouped_ByIsActive_ReturnsTwoGroups()
    {
        var evaluator = CreateEvaluator();

        Dictionary<bool, List<TestProduct>> result = evaluator.EvaluateGrouped(null, p => p.IsActive);

        result.Should().HaveCount(2);
        result[true].Should().HaveCount(7);
        result[false].Should().HaveCount(3);
    }

    // -----------------------------------------------------------------------
    // EvaluateCountByGroup
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateCountByGroup_ByCategory_ReturnCorrectCounts()
    {
        var evaluator = CreateEvaluator();

        Dictionary<string, int> result = evaluator.EvaluateCountByGroup(null, p => p.Category);

        result["Fruit"].Should().Be(4);
        result["Sweet"].Should().Be(3);
        result["Dairy"].Should().Be(2);
        result["Veggie"].Should().Be(1);
    }

    [Fact]
    public void EvaluateCountByGroup_ActiveOnly_ByCategory_ReturnsCorrectCounts()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Active: Fruit=3(Apple,Banana,Fig), Veggie=1(Carrot), Dairy=2(Egg,Ice), Sweet=1(Honey)
        Dictionary<string, int> result = evaluator.EvaluateCountByGroup(null, p => p.Category, filter);

        result["Fruit"].Should().Be(3);
        result["Veggie"].Should().Be(1);
        result["Dairy"].Should().Be(2);
        result["Sweet"].Should().Be(1);
    }

    // -----------------------------------------------------------------------
    // EvaluateSumByGroup
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateSumByGroup_StockByCategory_ReturnsCorrectSums()
    {
        var evaluator = CreateEvaluator();

        // Fruit: 100+150+30+60=340, Veggie: 200, Sweet: 50+20+45=115, Dairy: 80+90=170
        Dictionary<string, int> result = evaluator.EvaluateSumByGroup(null, p => p.Category, p => p.Stock);

        result["Fruit"].Should().Be(340);
        result["Veggie"].Should().Be(200);
        result["Sweet"].Should().Be(115);
        result["Dairy"].Should().Be(170);
    }

    [Fact]
    public void EvaluateSumByGroup_PriceByCategory_ReturnsCorrectSums()
    {
        var evaluator = CreateEvaluator();

        // Fruit: 1.5+0.8+4.5+3.2=10.0
        Dictionary<string, decimal> result = evaluator.EvaluateSumByGroup(null, p => p.Category, p => p.Price);

        result["Fruit"].Should().Be(10.0m);
        result["Dairy"].Should().Be(4.2m);  // 3.0+1.2
        result["Veggie"].Should().Be(0.5m);
    }

    // -----------------------------------------------------------------------
    // EvaluateMinByGroup
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateMinByGroup_StockByCategory_ReturnsMinPerGroup()
    {
        var evaluator = CreateEvaluator();

        Dictionary<string, int> result = evaluator.EvaluateMinByGroup(null, p => p.Category, p => p.Stock);

        result["Fruit"].Should().Be(30);   // Fig
        result["Veggie"].Should().Be(200); // Carrot (only one)
        result["Sweet"].Should().Be(20);   // Honey
        result["Dairy"].Should().Be(80);   // Egg
    }

    [Fact]
    public void EvaluateMinByGroup_PriceByCategory_ReturnsMinPricePerGroup()
    {
        var evaluator = CreateEvaluator();

        Dictionary<string, decimal> result = evaluator.EvaluateMinByGroup(null, p => p.Category, p => p.Price);

        result["Fruit"].Should().Be(0.8m);  // Banana
        result["Sweet"].Should().Be(2.5m);  // Donut
        result["Dairy"].Should().Be(1.2m);  // Ice
        result["Veggie"].Should().Be(0.5m); // Carrot
    }

    // -----------------------------------------------------------------------
    // EvaluateMaxByGroup
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateMaxByGroup_StockByCategory_ReturnsMaxPerGroup()
    {
        var evaluator = CreateEvaluator();

        Dictionary<string, int> result = evaluator.EvaluateMaxByGroup(null, p => p.Category, p => p.Stock);

        result["Fruit"].Should().Be(150);  // Banana
        result["Veggie"].Should().Be(200); // Carrot
        result["Sweet"].Should().Be(50);   // Donut
        result["Dairy"].Should().Be(90);   // Ice
    }

    [Fact]
    public void EvaluateMaxByGroup_PriceByCategory_ReturnsMaxPricePerGroup()
    {
        var evaluator = CreateEvaluator();

        Dictionary<string, decimal> result = evaluator.EvaluateMaxByGroup(null, p => p.Category, p => p.Price);

        result["Fruit"].Should().Be(4.5m);  // Fig
        result["Sweet"].Should().Be(7.0m);  // Honey
        result["Dairy"].Should().Be(3.0m);  // Egg
        result["Veggie"].Should().Be(0.5m); // Carrot
    }

    // -----------------------------------------------------------------------
    // EvaluateAverageByGroup
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateAverageByGroup_StockByCategory_ReturnsAveragePerGroup()
    {
        var evaluator = CreateEvaluator();

        // Fruit: (100+150+30+60)/4 = 340/4 = 85
        // Veggie: 200/1 = 200
        // Sweet: (50+20+45)/3 = 115/3 ≈ 38.333...
        // Dairy: (80+90)/2 = 85
        Dictionary<string, decimal> result = evaluator.EvaluateAverageByGroup(null, p => p.Category, p => p.Stock);

        result["Fruit"].Should().Be(85m);
        result["Veggie"].Should().Be(200m);
        result["Dairy"].Should().Be(85m);
        result["Sweet"].Should().BeApproximately(38.333m, 0.001m);
    }

    [Fact]
    public void EvaluateAverageByGroup_PriceByCategory_ReturnsAveragePricePerGroup()
    {
        var evaluator = CreateEvaluator();

        // Dairy: (3.0+1.2)/2 = 2.1
        Dictionary<string, decimal> result = evaluator.EvaluateAverageByGroup(null, p => p.Category, p => p.Price);

        result["Dairy"].Should().Be(2.1m);
        result["Veggie"].Should().Be(0.5m);
    }

    // -----------------------------------------------------------------------
    // EvaluateTopByGroup
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateTopByGroup_Top1ByCategory_ReturnsOneItemPerGroup()
    {
        var evaluator = CreateEvaluator();

        Dictionary<string, List<TestProduct>> result = evaluator.EvaluateTopByGroup<string, int>(
            null, p => p.Category, count: 1);

        result.Should().HaveCount(4);
        result.Values.Should().AllSatisfy(list => list.Should().HaveCount(1));
    }

    [Fact]
    public void EvaluateTopByGroup_Top2FruitOrderedByPriceAscending_ReturnsTwoCheapestFruits()
    {
        var evaluator = CreateEvaluator();

        // Order all by price ascending first, then group and take 2
        // Fruit sorted by price asc: Banana(0.8), Apple(1.5), Grape(3.2), Fig(4.5)
        Dictionary<string, List<TestProduct>> result = evaluator.EvaluateTopByGroup(
            null, p => p.Category, count: 2,
            orderBy: p => (object)p.Price, ascending: true);

        result["Fruit"].Should().HaveCount(2);
        result["Fruit"].Select(p => p.Id).Should().BeEquivalentTo(new[] { 2, 1 }); // Banana, Apple
    }

    [Fact]
    public void EvaluateTopByGroup_Top3WithActiveFilter_LimitsSourceBeforeGrouping()
    {
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Active Fruit: Apple, Banana, Fig (3 items) → top 3 = all 3
        Dictionary<string, List<TestProduct>> result = evaluator.EvaluateTopByGroup<string, int>(
            null, p => p.Category, count: 3, valiFlow: filter);

        result["Fruit"].Should().HaveCount(3);
        // Active Sweet has only Honey → top 3 = 1
        result["Sweet"].Should().HaveCount(1);
    }

    [Fact]
    public void EvaluateTopByGroup_Top2OrderedByStockDescending_ReturnsHighestStockPerGroup()
    {
        var evaluator = CreateEvaluator();

        // Fruit by stock desc: Banana(150), Apple(100), Grape(60), Fig(30) → top 2 = Banana, Apple
        Dictionary<string, List<TestProduct>> result = evaluator.EvaluateTopByGroup(
            null, p => p.Category, count: 2,
            orderBy: p => (object)p.Stock, ascending: false);

        result["Fruit"].Should().HaveCount(2);
        result["Fruit"].Select(p => p.Id).Should().Contain(2); // Banana
        result["Fruit"].Select(p => p.Id).Should().Contain(1); // Apple
    }
}
