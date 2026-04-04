using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

public sealed class ValiFlowEfReadTests
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
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m,  IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m,  IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,   IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1) },
            new TestOrder { Id = 4, CustomerName = "Dave",  Total = 500m,  IsShipped = false, CreatedAt = new DateTime(2024, 4, 1) },
            new TestOrder { Id = 5, CustomerName = "Eve",   Total = 210m,  IsShipped = true,  CreatedAt = new DateTime(2024, 5, 1) }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ── EvaluateAnyAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAnyAsync_WithMatchingFilter_ReturnsTrue()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new BasicSpecification<TestOrder>(filter);

        var result = await evaluator.EvaluateAnyAsync(spec);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAnyAsync_WithNoMatchingFilter_ReturnsFalse()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        // Total > 1000 matches nothing in the seed data
        var filter = new ValiFlowQuery<TestOrder>().GreaterThan(o => o.Total, 1000m);
        var spec = new BasicSpecification<TestOrder>(filter);

        var result = await evaluator.EvaluateAnyAsync(spec);

        result.Should().BeFalse();
    }

    // ── EvaluateCountAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateCountAsync_ShippedOrders_ReturnsThree()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new BasicSpecification<TestOrder>(filter);

        var count = await evaluator.EvaluateCountAsync(spec);

        count.Should().Be(3);
    }

    [Fact]
    public async Task EvaluateCountAsync_AllOrders_ReturnsFive()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        // Empty filter — matches all
        var spec = new BasicSpecification<TestOrder>();

        var count = await evaluator.EvaluateCountAsync(spec);

        count.Should().Be(5);
    }

    // ── EvaluateGetFirstAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateGetFirstAsync_ShippedOrders_ReturnsAnOrder()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new BasicSpecification<TestOrder>(filter);

        var order = await evaluator.EvaluateGetFirstAsync(spec);

        order.Should().NotBeNull();
        order!.IsShipped.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateGetFirstAsync_NoMatchingOrders_ReturnsNull()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().GreaterThan(o => o.Total, 9999m);
        var spec = new BasicSpecification<TestOrder>(filter);

        var order = await evaluator.EvaluateGetFirstAsync(spec);

        order.Should().BeNull();
    }

    // ── EvaluateGetFirstFailedAsync ───────────────────────────────────────────

    [Fact]
    public async Task EvaluateGetFirstFailedAsync_ShippedFilter_ReturnsNonShippedOrder()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        // Filter is "IsShipped == true"; FirstFailed returns first entity that does NOT match
        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new BasicSpecification<TestOrder>(filter);

        var order = await evaluator.EvaluateGetFirstFailedAsync(spec);

        order.Should().NotBeNull();
        order!.IsShipped.Should().BeFalse();
    }

    // ── EvaluateQueryAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateQueryAsync_ShippedFilter_ReturnsCorrectOrders()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new QuerySpecification<TestOrder>(filter)
            .WithOrderBy(o => o.Id);

        var query = await evaluator.EvaluateQueryAsync(spec);
        var results = await query.ToListAsync();

        results.Should().HaveCount(3);
        results.Should().OnlyContain(o => o.IsShipped);
    }

    [Fact]
    public async Task EvaluateQueryAsync_AllOrders_ReturnsFiveOrders()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id);

        var query = await evaluator.EvaluateQueryAsync(spec);
        var results = await query.ToListAsync();

        results.Should().HaveCount(5);
    }

    // ── EvaluateMinAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateMinAsync_Total_ReturnsSmallestTotal()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new BasicSpecification<TestOrder>();

        var min = await evaluator.EvaluateMinAsync(spec, o => o.Total);

        min.Should().Be(80m);
    }

    // ── EvaluateMaxAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateMaxAsync_Total_ReturnsLargestTotal()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new BasicSpecification<TestOrder>();

        var max = await evaluator.EvaluateMaxAsync(spec, o => o.Total);

        max.Should().Be(500m);
    }

    // ── EvaluateSumAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateSumAsync_AllTotals_ReturnsSumOfAllTotals()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new BasicSpecification<TestOrder>();

        // EvaluateSumAsync has an overload for decimal
        var sum = await evaluator.EvaluateSumAsync(spec, o => o.Total);

        sum.Should().Be(1260m); // 150 + 320 + 80 + 500 + 210
    }

    [Fact]
    public async Task EvaluateSumAsync_ShippedTotals_ReturnsSumOfShippedTotals()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new BasicSpecification<TestOrder>(filter);

        var sum = await evaluator.EvaluateSumAsync(spec, o => o.Total);

        sum.Should().Be(440m); // 150 + 80 + 210
    }

    // ── EvaluateAverageAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAverageAsync_AllTotals_ReturnsCorrectAverage()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new BasicSpecification<TestOrder>();

        var avg = await evaluator.EvaluateAverageAsync(spec, o => o.Total);

        avg.Should().Be(252m); // 1260 / 5
    }

    // ── EvaluateCountByGroupAsync ─────────────────────────────────────────────

    [Fact]
    public async Task EvaluateCountByGroupAsync_GroupByIsShipped_ReturnsCorrectCounts()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new BasicSpecification<TestOrder>();

        var grouped = await evaluator.EvaluateCountByGroupAsync(spec, o => o.IsShipped);

        grouped.Should().HaveCount(2);
        grouped[true].Should().Be(3);   // Alice, Carol, Eve
        grouped[false].Should().Be(2);  // Bob, Dave
    }

    // ── EvaluatePagedAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluatePagedAsync_Page1Size2_ReturnsFirstTwoOrdersOrderedById()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id)
            .WithPagination(1, 2);

        var paged = await evaluator.EvaluatePagedAsync(spec);

        paged.Items.Should().HaveCount(2);
        paged.TotalCount.Should().Be(5);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(2);
        paged.TotalPages.Should().Be(3);
        paged.Items[0].Id.Should().Be(1);
        paged.Items[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task EvaluatePagedAsync_Page2Size2_ReturnsSecondPage()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id)
            .WithPagination(2, 2);

        var paged = await evaluator.EvaluatePagedAsync(spec);

        paged.Items.Should().HaveCount(2);
        paged.Items[0].Id.Should().Be(3);
        paged.Items[1].Id.Should().Be(4);
        paged.HasNextPage.Should().BeTrue();
        paged.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluatePagedAsync_MissingOrderBy_ThrowsInvalidOperationException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithPagination(1, 2);

        var act = async () => await evaluator.EvaluatePagedAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
