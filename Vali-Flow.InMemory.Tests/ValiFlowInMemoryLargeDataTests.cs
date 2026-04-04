using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;

namespace Vali_Flow.InMemory.Tests;

public sealed class ValiFlowInMemoryLargeDataTests
{
    private static List<TestProduct> CreateLargeDataset(int count = 10_000) =>
        Enumerable.Range(1, count).Select(i => new TestProduct
        {
            Id = i,
            Name = $"Product{i}",
            Category = (i % 4) switch { 0 => "Fruit", 1 => "Veggie", 2 => "Sweet", _ => "Dairy" },
            Price = (i % 100) * 0.5m + 0.5m,
            Stock = i % 200,
            IsActive = i % 3 != 0
        }).ToList();

    private static ValiFlowEvaluator<TestProduct, int> CreateEvaluator(List<TestProduct> data)
        => new(data, null, p => p.Id);

    [Fact]
    public void EvaluateCount_10kEntities_WithFilter_ReturnsCorrect()
    {
        var data = CreateLargeDataset();
        var evaluator = CreateEvaluator(data);
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive); // i%3!=0 → 6667 items

        var count = evaluator.EvaluateCount(data, filter);

        count.Should().Be(data.Count(p => p.IsActive));
    }

    [Fact]
    public void EvaluateAll_10kEntities_ReturnsOrdered()
    {
        var data = CreateLargeDataset();
        var evaluator = CreateEvaluator(data);

        var results = evaluator.EvaluateAll<decimal>(data, orderBy: p => p.Price, ascending: true).ToList();

        results.Should().HaveCount(10_000);
        for (int i = 0; i < results.Count - 1; i++)
            results[i].Price.Should().BeLessOrEqualTo(results[i + 1].Price);
    }

    [Fact]
    public void EvaluateTop_10kEntities_Returns100()
    {
        var data = CreateLargeDataset();
        var evaluator = CreateEvaluator(data);

        var results = evaluator.EvaluateTop<int>(data, count: 100, orderBy: p => p.Stock, ascending: false).ToList();

        results.Should().HaveCount(100);
        results[0].Stock.Should().BeGreaterOrEqualTo(results[99].Stock);
    }

    [Fact]
    public void EvaluatePaged_10kEntities_LastPage_CorrectCount()
    {
        var data = CreateLargeDataset();
        var evaluator = CreateEvaluator(data);

        // 10000 items, pageSize=100 → 100 pages, last page=100
        var results = evaluator.EvaluatePaged<int>(data, page: 100, pageSize: 100, orderBy: p => p.Id, ascending: true).ToList();

        results.Should().HaveCount(100);
        results.Last().Id.Should().Be(10_000);
    }

    [Fact]
    public void EvaluateSum_10kEntities_ReturnsCorrectTotal()
    {
        var data = CreateLargeDataset();
        var evaluator = CreateEvaluator(data);

        var sum = evaluator.EvaluateSum(data, p => p.Price);
        var expected = data.Sum(p => p.Price);

        sum.Should().Be(expected);
    }

    [Fact]
    public void EvaluateGrouped_10kEntities_GroupsCorrectly()
    {
        var data = CreateLargeDataset();
        var evaluator = CreateEvaluator(data);

        var grouped = evaluator.EvaluateGrouped(data, p => p.Category);

        grouped.Should().HaveCount(4);
        grouped.Values.Sum(g => g.Count).Should().Be(10_000);
    }

    [Fact]
    public void EvaluateDistinct_10kEntities_RemovesDuplicates()
    {
        var data = CreateLargeDataset();
        var evaluator = CreateEvaluator(data);

        // Distinct by Category → only 4 unique categories
        var result = evaluator.EvaluateDistinct<string>(data, p => p.Category).ToList();

        result.Should().HaveCount(4);
    }

    [Fact]
    public void PagedResult_10kEntities_TotalPagesCorrect()
    {
        var data = CreateLargeDataset();
        var evaluator = CreateEvaluator(data);

        var pagedResult = evaluator.EvaluatePagedResult<int>(data, page: 1, pageSize: 100);

        pagedResult.TotalCount.Should().Be(10_000);
        pagedResult.TotalPages.Should().Be(100);
        pagedResult.Items.Should().HaveCount(100);
        pagedResult.HasNextPage.Should().BeTrue();
        pagedResult.HasPreviousPage.Should().BeFalse();
    }
}
