using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

public sealed class ValiFlowEfQueryWrapperTests
{
    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<TestDbContext> CreateSeededContextAsync()
    {
        var ctx = CreateContext();
        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 100m, IsShipped = true,  CreatedAt = new DateTime(2024,1,1) },
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 200m, IsShipped = false, CreatedAt = new DateTime(2024,2,1) },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 300m, IsShipped = true,  CreatedAt = new DateTime(2024,3,1) }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // EvaluateQueryAsync returns correct IQueryable
    [Fact]
    public async Task EvaluateQueryAsync_ReturnsAllOrders_WhenNoFilter()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>().WithOrderBy(o => o.Id);

        var query = await ev.EvaluateQueryAsync(spec);
        var results = await query.ToListAsync();

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task EvaluateQueryAsync_WithFilter_ReturnsFilteredOrders()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>(
                new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped))
            .WithOrderBy(o => o.Id);

        var query = await ev.EvaluateQueryAsync(spec);
        var results = await query.ToListAsync();

        results.Should().HaveCount(2);
        results.Should().OnlyContain(o => o.IsShipped);
    }

    [Fact]
    public async Task EvaluateQueryAsync_NullSpecification_ThrowsArgumentNullException()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = async () => await ev.EvaluateQueryAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // EvaluateQueryFailedAsync
    [Fact]
    public async Task EvaluateQueryFailedAsync_ShippedFilter_ReturnsUnshippedOrders()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>(
                new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped))
            .WithOrderBy(o => o.Id);

        var query = await ev.EvaluateQueryFailedAsync(spec);
        var results = await query.ToListAsync();

        results.Should().HaveCount(1);
        results.Should().OnlyContain(o => !o.IsShipped);
    }

    [Fact]
    public async Task EvaluateQueryFailedAsync_NullSpecification_ThrowsArgumentNullException()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = async () => await ev.EvaluateQueryFailedAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
