using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

public sealed class ValiFlowEfAggregateTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<TestDbContext> CreateSeededContextAsync()
    {
        var ctx = CreateContext();
        ctx.Orders.AddRange(
            new TestOrder { Id = 1,  CustomerName = "Alice",   Total = 150m,  IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 2,  CustomerName = "Bob",     Total = 320m,  IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) },
            new TestOrder { Id = 3,  CustomerName = "Carol",   Total = 80m,   IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1) },
            new TestOrder { Id = 4,  CustomerName = "Dave",    Total = 500m,  IsShipped = false, CreatedAt = new DateTime(2024, 4, 1) },
            new TestOrder { Id = 5,  CustomerName = "Eve",     Total = 210m,  IsShipped = true,  CreatedAt = new DateTime(2024, 5, 1) }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    private static BasicSpecification<TestOrder> AllOrders() =>
        new();

    private static BasicSpecification<TestOrder> ShippedOrders() =>
        new(new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped));

    private static BasicSpecification<TestOrder> UnshippedOrders() =>
        new(new ValiFlowQuery<TestOrder>().IsFalse(o => o.IsShipped));

    // ── EvaluateAverageAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAverageAsync_AllOrders_ReturnsCorrectAverage()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // (150+320+80+500+210)/5 = 1260/5 = 252
        decimal avg = await ev.EvaluateAverageAsync(AllOrders(), o => o.Total);

        avg.Should().Be(252m);
    }

    [Fact]
    public async Task EvaluateAverageAsync_FilteredShipped_ReturnsCorrectAverage()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Shipped: Alice(150), Carol(80), Eve(210) → (150+80+210)/3 = 440/3 ≈ 146.666...
        decimal avg = await ev.EvaluateAverageAsync(ShippedOrders(), o => o.Total);

        avg.Should().BeApproximately(146.666m, 0.001m);
    }

    [Fact]
    public async Task EvaluateAverageAsync_EmptyResult_ReturnsZero()
    {
        await using var ctx = CreateContext(); // empty DB
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        decimal avg = await ev.EvaluateAverageAsync(AllOrders(), o => o.Total);

        avg.Should().Be(0m);
    }

    // ── EvaluateMinAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateMinAsync_AllOrders_ReturnsSmallestTotal()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        decimal min = await ev.EvaluateMinAsync(AllOrders(), o => o.Total);

        min.Should().Be(80m); // Carol
    }

    [Fact]
    public async Task EvaluateMinAsync_FilteredUnshipped_ReturnsMinAmongUnshipped()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Unshipped: Bob(320), Dave(500)
        decimal min = await ev.EvaluateMinAsync(UnshippedOrders(), o => o.Total);

        min.Should().Be(320m);
    }

    [Fact]
    public async Task EvaluateMinAsync_EmptyResult_ReturnsZero()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        decimal min = await ev.EvaluateMinAsync(AllOrders(), o => o.Total);

        min.Should().Be(0m);
    }

    // ── EvaluateMaxAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateMaxAsync_AllOrders_ReturnsLargestTotal()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        decimal max = await ev.EvaluateMaxAsync(AllOrders(), o => o.Total);

        max.Should().Be(500m); // Dave
    }

    [Fact]
    public async Task EvaluateMaxAsync_FilteredShipped_ReturnsMaxAmongShipped()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Shipped: Alice(150), Carol(80), Eve(210)
        decimal max = await ev.EvaluateMaxAsync(ShippedOrders(), o => o.Total);

        max.Should().Be(210m);
    }

    [Fact]
    public async Task EvaluateMaxAsync_EmptyResult_ReturnsZero()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        decimal max = await ev.EvaluateMaxAsync(AllOrders(), o => o.Total);

        max.Should().Be(0m);
    }

    // ── EvaluateSumAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateSumAsync_AllOrders_ReturnsCorrectTotal()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // 150+320+80+500+210 = 1260
        decimal sum = await ev.EvaluateSumAsync(AllOrders(), o => o.Total);

        sum.Should().Be(1260m);
    }

    [Fact]
    public async Task EvaluateSumAsync_NoMatch_ReturnsZero()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var noMatch = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().GreaterThan(o => o.Total, 9999m));

        decimal sum = await ev.EvaluateSumAsync(noMatch, o => o.Total);

        sum.Should().Be(0m);
    }
}
