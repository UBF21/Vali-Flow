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
/// Covers: EvaluateDistinctAsync pagination (post-grouping), EvaluateDuplicatesAsync pagination,
/// EvaluateAverageAsync precision, EvaluateMinAsync/EvaluateMaxAsync DB-side, EvaluateTopAsync.
/// </summary>
public sealed class ValiFlowEfDistinctPaginationTests
{
    // Seed: 6 orders — two customers share IsShipped=true (3), two share IsShipped=false (2), one extra true
    // CustomerNames: "A1", "A2", "B1", "B2", "C1", "C2"  — all unique
    // IsShipped groups: true={A1,B1,C1}, false={A2,B2}
    // Totals: 10, 20, 30, 40, 50, 60

    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<TestDbContext> CreateSeededContextAsync()
    {
        var ctx = CreateContext();
        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "A1", Total = 10m, IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 2, CustomerName = "A2", Total = 20m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) },
            new TestOrder { Id = 3, CustomerName = "B1", Total = 30m, IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1) },
            new TestOrder { Id = 4, CustomerName = "B2", Total = 40m, IsShipped = false, CreatedAt = new DateTime(2024, 4, 1) },
            new TestOrder { Id = 5, CustomerName = "C1", Total = 50m, IsShipped = true,  CreatedAt = new DateTime(2024, 5, 1) },
            new TestOrder { Id = 6, CustomerName = "C2", Total = 60m, IsShipped = true,  CreatedAt = new DateTime(2024, 6, 1) }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ── EvaluateDistinctAsync — pagination after grouping ────────────────────

    [Fact]
    public async Task EvaluateDistinctAsync_WithPagination_PaginatesOnGroupedResult()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // 6 rows, 6 unique CustomerNames → 6 distinct groups. Page 1, size 2 should return first 2 distinct.
        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id)
            .WithPagination(1, 2);

        var distinct = await ev.EvaluateDistinctAsync(spec, o => o.CustomerName);

        // Pagination operates on the 6 distinct groups → 2 results
        distinct.Should().HaveCount(2);
    }

    [Fact]
    public async Task EvaluateDistinctAsync_ByIsShipped_WithPagination_PagesDistinctGroups()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // IsShipped has 2 distinct values (true, false). Page 1 size 1 → 1 distinct value.
        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id)
            .WithPagination(1, 1);

        var distinct = await ev.EvaluateDistinctAsync(spec, o => o.IsShipped);

        distinct.Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateDistinctAsync_WithTop_LimitsDistinctGroups()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id)
            .WithTop(3);

        var distinct = await ev.EvaluateDistinctAsync(spec, o => o.CustomerName);

        // 6 distinct names, Top(3) on grouped result → 3
        distinct.Should().HaveCount(3);
    }

    // ── EvaluateDuplicatesAsync — pagination after grouping ───────────────────

    [Fact]
    public async Task EvaluateDuplicatesAsync_WithPagination_PaginatesOnDuplicateSet()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // IsShipped: true×4, false×2 → all 6 are duplicates. Page 1 size 2 → 2 results.
        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id)
            .WithPagination(1, 2);

        var dups = await ev.EvaluateDuplicatesAsync(spec, o => o.IsShipped);

        dups.Should().HaveCount(2);
    }

    [Fact]
    public async Task EvaluateDuplicatesAsync_WithTop_LimitsDuplicateSet()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id)
            .WithTop(3);

        var dups = await ev.EvaluateDuplicatesAsync(spec, o => o.IsShipped);

        dups.Should().HaveCount(3);
    }

    // ── EvaluateAverageAsync — decimal precision ─────────────────────────────

    [Fact]
    public async Task EvaluateAverageAsync_DecimalColumn_CorrectPrecision()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        // Totals: 10+20+30+40+50+60 = 210 / 6 = 35
        var avg = await ev.EvaluateAverageAsync(spec, o => o.Total);

        avg.Should().Be(35m);
    }

    [Fact]
    public async Task EvaluateAverageAsync_FilteredOrders_CorrectValue()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped));

        // Shipped totals: 10+30+50+60 = 150 / 4 = 37.5
        var avg = await ev.EvaluateAverageAsync(spec, o => o.Total);

        avg.Should().Be(37.5m);
    }

    [Fact]
    public async Task EvaluateAverageAsync_EmptyResult_ReturnsZero()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().GreaterThan(o => o.Total, 9999m));

        var avg = await ev.EvaluateAverageAsync(spec, o => o.Total);

        avg.Should().Be(0m);
    }

    // ── EvaluateMinAsync / EvaluateMaxAsync — DB-side ────────────────────────

    [Fact]
    public async Task EvaluateMinAsync_DecimalColumn_ReturnsMin()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var min = await ev.EvaluateMinAsync(spec, o => o.Total);

        min.Should().Be(10m);
    }

    [Fact]
    public async Task EvaluateMinAsync_EmptyResult_ReturnsZero()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().GreaterThan(o => o.Total, 9999m));

        var min = await ev.EvaluateMinAsync(spec, o => o.Total);

        min.Should().Be(0m);
    }

    [Fact]
    public async Task EvaluateMinAsync_IntColumn_ReturnsMin()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var min = await ev.EvaluateMinAsync(spec, o => o.Id);

        min.Should().Be(1);
    }

    [Fact]
    public async Task EvaluateMaxAsync_DecimalColumn_ReturnsMax()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var max = await ev.EvaluateMaxAsync(spec, o => o.Total);

        max.Should().Be(60m);
    }

    [Fact]
    public async Task EvaluateMaxAsync_EmptyResult_ReturnsZero()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().GreaterThan(o => o.Total, 9999m));

        var max = await ev.EvaluateMaxAsync(spec, o => o.Total);

        max.Should().Be(0m);
    }

    [Fact]
    public async Task EvaluateMaxAsync_IntColumn_ReturnsMax()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var max = await ev.EvaluateMaxAsync(spec, o => o.Id);

        max.Should().Be(6);
    }

    // ── EvaluateTopAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateTopAsync_Count3_ReturnsThreeLowestIds()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>().WithOrderBy(o => o.Id);
        var query = await ev.EvaluateTopAsync(spec, 3);
        var results = await query.ToListAsync();

        results.Should().HaveCount(3);
        results[0].Id.Should().Be(1);
        results[2].Id.Should().Be(3);
    }

    [Fact]
    public async Task EvaluateTopAsync_CountZero_ThrowsArgumentOutOfRangeException()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>().WithOrderBy(o => o.Id);

        Func<Task> act = async () => await ev.EvaluateTopAsync(spec, 0);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
