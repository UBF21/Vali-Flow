using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

/// <summary>
/// Covers: EvaluateGetLastAsync, EvaluateGetLastFailedAsync, EvaluateDistinctAsync,
/// EvaluateDuplicatesAsync, EvaluateTopAsync, EvaluateAllFailedAsync,
/// and QuerySpecification with descending order, ThenBy, and Top.
/// </summary>
public sealed class ValiFlowEfQueryTests
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
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,  IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1) },
            new TestOrder { Id = 4, CustomerName = "Dave",  Total = 500m, IsShipped = false, CreatedAt = new DateTime(2024, 4, 1) },
            new TestOrder { Id = 5, CustomerName = "Eve",   Total = 210m, IsShipped = true,  CreatedAt = new DateTime(2024, 5, 1) }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ── EvaluateGetLastAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateGetLastAsync_OrderedById_ReturnsHighestId()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id);

        var last = await ev.EvaluateGetLastAsync(spec);

        last.Should().NotBeNull();
        last!.Id.Should().Be(5);
    }

    [Fact]
    public async Task EvaluateGetLastAsync_FilteredShipped_ReturnsLastShippedByDate()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>(
                new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped))
            .WithOrderBy(o => o.CreatedAt);

        var last = await ev.EvaluateGetLastAsync(spec);

        last.Should().NotBeNull();
        last!.CustomerName.Should().Be("Eve"); // latest shipped date
    }

    [Fact]
    public async Task EvaluateGetLastAsync_NoOrderBy_ThrowsInvalidOperationException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>();

        Func<Task> act = async () => await ev.EvaluateGetLastAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── EvaluateGetLastFailedAsync ─────────────────────────────────────────────

    [Fact]
    public async Task EvaluateGetLastFailedAsync_ShippedFilter_ReturnsLastUnshippedByDate()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Filter = IsShipped==true; Failed = NOT shipped
        var spec = new QuerySpecification<TestOrder>(
                new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped))
            .WithOrderBy(o => o.CreatedAt);

        var last = await ev.EvaluateGetLastFailedAsync(spec);

        last.Should().NotBeNull();
        last!.IsShipped.Should().BeFalse();
        last.CustomerName.Should().Be("Dave"); // latest unshipped
    }

    [Fact]
    public async Task EvaluateGetLastFailedAsync_NoOrderBy_ThrowsInvalidOperationException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped));

        Func<Task> act = async () => await ev.EvaluateGetLastFailedAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── EvaluateDistinctAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateDistinctAsync_ByIsShipped_ReturnsTwoDistinctRecords()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id);

        var distinct = await ev.EvaluateDistinctAsync(spec, o => o.IsShipped);
        var list = distinct.ToList();

        // Two distinct IsShipped values → one representative per group
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task EvaluateDistinctAsync_ByCustomerName_ReturnsAllFive()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id);

        // All customer names are unique → 5 distinct
        var distinct = await ev.EvaluateDistinctAsync(spec, o => o.CustomerName);

        distinct.Should().HaveCount(5);
    }

    // ── EvaluateDuplicatesAsync ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateDuplicatesAsync_ByIsShipped_ReturnsAllFive()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id);

        // true: 3 orders, false: 2 orders — both groups have >1 member
        var duplicates = await ev.EvaluateDuplicatesAsync(spec, o => o.IsShipped);

        duplicates.Should().HaveCount(5);
    }

    [Fact]
    public async Task EvaluateDuplicatesAsync_ByUniqueKey_ReturnsEmpty()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id);

        // CustomerName is unique for each order → no duplicates
        var duplicates = await ev.EvaluateDuplicatesAsync(spec, o => o.CustomerName);

        duplicates.Should().BeEmpty();
    }

    // ── EvaluateTopAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateTopAsync_Top3_ReturnsThreeOrders()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id);

        var query = await ev.EvaluateTopAsync(spec, 3);
        var results = await query.ToListAsync();

        results.Should().HaveCount(3);
        results[0].Id.Should().Be(1);
        results[2].Id.Should().Be(3);
    }

    [Fact]
    public async Task EvaluateTopAsync_CountZero_ThrowsArgumentOutOfRangeException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>().WithOrderBy(o => o.Id);

        Func<Task> act = async () => await ev.EvaluateTopAsync(spec, 0);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ── EvaluateAllFailedAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAllFailedAsync_ShippedFilter_ReturnsUnshippedOrders()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>(
                new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped))
            .WithOrderBy(o => o.Id);

        var query = await ev.EvaluateAllFailedAsync(spec);
        var results = await query.ToListAsync();

        results.Should().HaveCount(2);
        results.Should().OnlyContain(o => !o.IsShipped);
    }

    // ── QuerySpecification — descending order ─────────────────────────────────

    [Fact]
    public async Task EvaluateQueryAsync_DescendingOrder_ReturnsSortedByTotalDesc()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Total, ascending: false);

        var query = await ev.EvaluateQueryAsync(spec);
        var results = await query.ToListAsync();

        results.Should().HaveCount(5);
        results[0].Total.Should().Be(500m); // Dave
        results[4].Total.Should().Be(80m);  // Carol
    }

    // ── QuerySpecification — ThenBy ───────────────────────────────────────────

    [Fact]
    public async Task EvaluateQueryAsync_OrderByIsShippedThenByTotal_ReturnsCorrectOrder()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.IsShipped)
            .AddThenBy(o => o.Total);

        var query = await ev.EvaluateQueryAsync(spec);
        var results = await query.ToListAsync();

        // First group: IsShipped=false (Bob 320, Dave 500) ordered by Total asc
        results[0].CustomerName.Should().Be("Bob");
        results[1].CustomerName.Should().Be("Dave");
        // Second group: IsShipped=true (Carol 80, Alice 150, Eve 210) ordered by Total asc
        results[2].CustomerName.Should().Be("Carol");
        results[3].CustomerName.Should().Be("Alice");
        results[4].CustomerName.Should().Be("Eve");
    }

    // ── QuerySpecification — WithTop ──────────────────────────────────────────

    [Fact]
    public async Task EvaluateQueryAsync_WithTop2_ReturnsTwoHighestTotals()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Total, ascending: false)
            .WithTop(2);

        var query = await ev.EvaluateQueryAsync(spec);
        var results = await query.Take(spec.Top!.Value).ToListAsync();

        results.Should().HaveCount(2);
        results[0].Total.Should().Be(500m);
        results[1].Total.Should().Be(320m);
    }

    // ── QuerySpecification — WithPagination + filter ──────────────────────────

    [Fact]
    public async Task EvaluatePagedAsync_FilteredShipped_PaginationIsCorrect()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>(
                new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped))
            .WithOrderBy(o => o.Total)
            .WithPagination(1, 2);

        var paged = await ev.EvaluatePagedAsync(spec);

        // Shipped: Carol(80), Alice(150), Eve(210) → page 1 size 2 = Carol, Alice
        paged.TotalCount.Should().Be(3);
        paged.TotalPages.Should().Be(2);
        paged.Items.Should().HaveCount(2);
        paged.Items[0].CustomerName.Should().Be("Carol");
        paged.Items[1].CustomerName.Should().Be("Alice");
    }
}
