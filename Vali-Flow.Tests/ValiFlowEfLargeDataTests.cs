using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

public sealed class ValiFlowEfLargeDataTests
{
    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<TestDbContext> CreateLargeSeededContextAsync(int count = 1000)
    {
        var ctx = CreateContext();
        var orders = Enumerable.Range(1, count).Select(i => new TestOrder
        {
            Id = i,
            CustomerName = i % 2 == 0 ? "Even" : "Odd",
            Total = i * 10m,
            IsShipped = i % 3 == 0,
            CreatedAt = new DateTime(2024, 1, 1).AddDays(i)
        }).ToList();
        ctx.Orders.AddRange(orders);
        await ctx.SaveChangesAsync();
        return ctx;
    }

    [Fact]
    public async Task EvaluateQueryAsync_1000Entities_ReturnsAll()
    {
        await using var ctx = await CreateLargeSeededContextAsync(1000);
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>(new ValiFlowQuery<TestOrder>());
        var results = await evaluator.EvaluateQueryAsync(spec);

        results.Should().HaveCount(1000);
    }

    [Fact]
    public async Task EvaluateCountAsync_1000FilteredEntities_ReturnsCorrect()
    {
        await using var ctx = await CreateLargeSeededContextAsync(1000);
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped); // i%3==0 → 333 items
        var spec = new BasicSpecification<TestOrder>(filter);
        var count = await evaluator.EvaluateCountAsync(spec);

        count.Should().Be(333);
    }

    [Fact]
    public async Task EvaluatePagedAsync_1000Entities_Page10_ReturnsCorrectSlice()
    {
        await using var ctx = await CreateLargeSeededContextAsync(1000);
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>();
        var spec = new QuerySpecification<TestOrder>(filter)
            .WithOrderBy(o => o.Id, ascending: true)
            .WithPagination(10, 50);

        var results = (await evaluator.EvaluatePagedAsync(spec)).Items.ToList();

        results.Should().HaveCount(50);
        results.First().Id.Should().Be(451); // page 10 of 50: (10-1)*50+1 = 451
    }

    [Fact]
    public async Task EvaluateGroupedAsync_1000Entities_GroupsByCategory()
    {
        await using var ctx = await CreateLargeSeededContextAsync(1000);
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>();
        var spec = new BasicSpecification<TestOrder>(filter);

        var grouped = await evaluator.EvaluateGroupedAsync(spec, o => o.CustomerName);

        grouped.Should().HaveCount(2); // "Even" and "Odd"
        grouped["Even"].Should().HaveCount(500);
        grouped["Odd"].Should().HaveCount(500);
    }

    [Fact]
    public async Task EvaluateMinMaxAsync_1000Entities_ReturnsCorrectValues()
    {
        await using var ctx = await CreateLargeSeededContextAsync(1000);
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>();
        var spec = new BasicSpecification<TestOrder>(filter);

        var min = await evaluator.EvaluateMinAsync<decimal>(spec, o => o.Total);
        var max = await evaluator.EvaluateMaxAsync<decimal>(spec, o => o.Total);

        min.Should().Be(10m);    // Id=1 → 1*10
        max.Should().Be(10000m); // Id=1000 → 1000*10
    }

    [Fact]
    public async Task EvaluateAllAsync_WithOrdering_1000Entities_IsSorted()
    {
        await using var ctx = await CreateLargeSeededContextAsync(1000);
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>();
        var spec = new QuerySpecification<TestOrder>(filter)
            .WithOrderBy(o => o.Total, ascending: false);

        var results = (await evaluator.EvaluateQueryAsync(spec)).ToList();

        results.Should().HaveCount(1000);
        results.First().Total.Should().Be(10000m);
        results.Last().Total.Should().Be(10m);
    }
}
