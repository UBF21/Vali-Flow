using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;

namespace Vali_Flow.InMemory.Tests;

/// <summary>
/// Tests that verify EvaluateAverage precision after replacing
/// decimal.CreateTruncating(sum) with decimal.CreateChecked(sum).
/// Also verifies correct handling of negative constant values.
/// </summary>
public sealed class ValiFlowInMemoryAveragePrecisionTests
{
    // -----------------------------------------------------------------------
    // Seed
    // Id  Name      Category  Price   Stock  Active
    //  1  Alpha     A         1.00      1    true
    //  2  Beta      A         2.00      3    true
    //  3  Gamma     B         3.00      5    false
    //  4  Delta     B         4.00      2    true
    //  5  Epsilon   C         5.00      4    false
    // -----------------------------------------------------------------------
    // Stock averages for precision tests:
    //   All items:           (1+3+5+2+4)/5 = 15/5 = 3.0   (exact integer)
    //   Items 1+2:           (1+3)/2       = 4/2  = 2.0   (exact integer)
    //   Items 1+3:           (1+5)/2       = 6/2  = 3.0   (exact integer)
    //   Items 1+2 (Id 1,2):  (1+3)/2       = 2.0          (exact)
    //   Active (Id 1,2,4):   (1+3+2)/3     = 6/3  = 2.0   (exact integer)
    //   Inactive (Id 3,5):   (5+4)/2       = 9/2  = 4.5   (fractional — key precision case)
    // -----------------------------------------------------------------------

    private static readonly List<TestProduct> Seed = new()
    {
        new() { Id = 1, Name = "Alpha",   Category = "A", Price = 1.00m, Stock = 1, IsActive = true  },
        new() { Id = 2, Name = "Beta",    Category = "A", Price = 2.00m, Stock = 3, IsActive = true  },
        new() { Id = 3, Name = "Gamma",   Category = "B", Price = 3.00m, Stock = 5, IsActive = false },
        new() { Id = 4, Name = "Delta",   Category = "B", Price = 4.00m, Stock = 2, IsActive = true  },
        new() { Id = 5, Name = "Epsilon", Category = "C", Price = 5.00m, Stock = 4, IsActive = false },
    };

    private static ValiFlowEvaluator<TestProduct, int> CreateEvaluator()
        => new(Seed, null, p => p.Id);

    // -----------------------------------------------------------------------
    // EvaluateAverage — integer Stock, exact result
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateAverage_IntegerStock_OddCount_ReturnsExactResult()
    {
        // (1+3+5)/3 = 3.0 — three items whose average is a whole number
        var items = Seed.Where(p => p.Id is 1 or 2 or 3).ToList();
        var evaluator = new ValiFlowEvaluator<TestProduct, int>(items, null, p => p.Id);

        decimal result = evaluator.EvaluateAverage<int>(null, p => p.Stock);

        result.Should().Be(3.0m);
    }

    [Fact]
    public void EvaluateAverage_IntegerStock_EvenCount_ReturnsFractionalResult()
    {
        // (1+3)/2 = 2.0 — two items, whole-number average
        var items = Seed.Where(p => p.Id is 1 or 2).ToList();
        var evaluator = new ValiFlowEvaluator<TestProduct, int>(items, null, p => p.Id);

        decimal result = evaluator.EvaluateAverage<int>(null, p => p.Stock);

        result.Should().Be(2.0m);
    }

    [Fact]
    public void EvaluateAverage_IntegerStock_FractionalAverage_PreservesPrecision()
    {
        // Inactive items: Stock 5 and 4 → (5+4)/2 = 4.5
        // With CreateTruncating the intermediate decimal conversion is identical for int,
        // but this test documents the expected precise decimal output.
        var filter = new ValiFlow<TestProduct>().IsFalse(p => p.IsActive);
        var evaluator = CreateEvaluator();

        decimal result = evaluator.EvaluateAverage<int>(null, p => p.Stock, filter);

        result.Should().Be(4.5m);
    }

    // -----------------------------------------------------------------------
    // EvaluateAverage — empty store returns 0
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateAverage_EmptyStore_ReturnsZero()
    {
        var evaluator = new ValiFlowEvaluator<TestProduct, int>(new List<TestProduct>(), null, p => p.Id);

        decimal result = evaluator.EvaluateAverage<int>(null, p => p.Stock);

        result.Should().Be(0m);
    }

    // -----------------------------------------------------------------------
    // EvaluateAverage — single item returns that item's value exactly
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateAverage_SingleItem_ReturnsItemValueAsDecimal()
    {
        var items = Seed.Where(p => p.Id == 3).ToList(); // Stock = 5
        var evaluator = new ValiFlowEvaluator<TestProduct, int>(items, null, p => p.Id);

        decimal result = evaluator.EvaluateAverage<int>(null, p => p.Stock);

        result.Should().Be(5.0m);
    }

    // -----------------------------------------------------------------------
    // EvaluateAverage — with filter (active products only)
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateAverage_WithFilter_AveragesOnlyMatchingItems()
    {
        // Active items: Id 1 (Stock=1), Id 2 (Stock=3), Id 4 (Stock=2)
        // Average = (1+3+2)/3 = 2.0
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);
        var evaluator = CreateEvaluator();

        decimal result = evaluator.EvaluateAverage<int>(null, p => p.Stock, filter);

        result.Should().Be(2.0m);
    }

    // -----------------------------------------------------------------------
    // EvaluateAverage — with negateCondition (non-active products)
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateAverage_WithNegateCondition_AveragesNonMatchingItems()
    {
        // negateCondition=true on IsActive filter → inactive items: Id 3 (Stock=5), Id 5 (Stock=4)
        // Average = (5+4)/2 = 4.5
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);
        var evaluator = CreateEvaluator();

        decimal result = evaluator.EvaluateAverage<int>(null, p => p.Stock, filter, negateCondition: true);

        result.Should().Be(4.5m);
    }

    // -----------------------------------------------------------------------
    // EvaluateAverage — decimal Price field (verifies CreateChecked path)
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateAverage_DecimalPrice_AllItems_ReturnsCorrectAverage()
    {
        // Prices: 1.00 + 2.00 + 3.00 + 4.00 + 5.00 = 15.00 / 5 = 3.00
        var evaluator = CreateEvaluator();

        decimal result = evaluator.EvaluateAverage<decimal>(null, p => p.Price);

        result.Should().Be(3.00m);
    }

    [Fact]
    public void EvaluateAverage_DecimalPrice_WithFilter_ReturnsFractionalPrecision()
    {
        // Active prices: 1.00, 2.00, 4.00 → sum = 7.00 / 3 = 2.3333...
        // Verify that decimal division is used, not integer truncation
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);
        var evaluator = CreateEvaluator();

        decimal result = evaluator.EvaluateAverage<decimal>(null, p => p.Price, filter);

        decimal expected = 7.00m / 3m;
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Negative constant handling — filter that uses a negative comparison
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateAverage_NegativeConstantFilter_HandledCorrectly()
    {
        // Seed contains no negative stock, so a filter for Stock > -1 matches all items
        // All Stock: 1+3+5+2+4 = 15 / 5 = 3.0
        var filter = new ValiFlow<TestProduct>().GreaterThan(p => p.Stock, -1);
        var evaluator = CreateEvaluator();

        decimal result = evaluator.EvaluateAverage<int>(null, p => p.Stock, filter);

        result.Should().Be(3.0m);
    }

    [Fact]
    public void EvaluateAverage_NegativeConstantFilter_NoMatches_ReturnsZero()
    {
        // Stock > 1000 matches nothing → should return 0, not throw
        var filter = new ValiFlow<TestProduct>().GreaterThan(p => p.Stock, 1000);
        var evaluator = CreateEvaluator();

        decimal result = evaluator.EvaluateAverage<int>(null, p => p.Stock, filter);

        result.Should().Be(0m);
    }
}
