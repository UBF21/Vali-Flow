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
/// Covers: EvaluateGroupedAsync, EvaluateSumByGroupAsync, EvaluateMinByGroupAsync,
/// EvaluateMaxByGroupAsync, EvaluateAverageByGroupAsync, EvaluateDuplicatesByGroupAsync,
/// EvaluateUniquesByGroupAsync, EvaluateTopByGroupAsync, EvaluateAggregateAsync.
/// </summary>
public sealed class ValiFlowEfGroupedTests
{
    // ── Seed ──────────────────────────────────────────────────────────────────
    // IsShipped=true:  Alice(150), Carol(80), Eve(210)  → sum=440, min=80, max=210, avg≈146.67
    // IsShipped=false: Bob(320), Dave(500)              → sum=820, min=320, max=500, avg=410
    // All totals: 150+320+80+500+210 = 1260

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

    // ── EvaluateGroupedAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateGroupedAsync_ByIsShipped_ReturnsTwoGroups()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var grouped = await ev.EvaluateGroupedAsync(spec, o => o.IsShipped);

        grouped.Should().HaveCount(2);
        grouped[true].Should().HaveCount(3);
        grouped[false].Should().HaveCount(2);
    }

    [Fact]
    public async Task EvaluateGroupedAsync_FilteredShipped_ReturnsOnlyShippedGroup()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped));

        var grouped = await ev.EvaluateGroupedAsync(spec, o => o.IsShipped);

        grouped.Should().HaveCount(1);
        grouped[true].Should().HaveCount(3);
    }

    // ── EvaluateSumByGroupAsync ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateSumByGroupAsync_Decimal_ByIsShipped_CorrectSums()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var sums = await ev.EvaluateSumByGroupAsync(spec, o => o.IsShipped, o => o.Total);

        sums[true].Should().Be(440m);   // 150+80+210
        sums[false].Should().Be(820m);  // 320+500
    }

    [Fact]
    public async Task EvaluateSumByGroupAsync_Decimal_EmptyFilter_ReturnsZeroGroups()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().GreaterThan(o => o.Total, 9999m));

        var sums = await ev.EvaluateSumByGroupAsync(spec, o => o.IsShipped, o => o.Total);

        sums.Should().BeEmpty();
    }

    // ── EvaluateMinByGroupAsync ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateMinByGroupAsync_Decimal_ByIsShipped_CorrectMins()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var mins = await ev.EvaluateMinByGroupAsync(spec, o => o.IsShipped, o => o.Total);

        mins[true].Should().Be(80m);    // Carol
        mins[false].Should().Be(320m);  // Bob
    }

    [Fact]
    public async Task EvaluateMinByGroupAsync_Decimal_FilteredShipped_OnlyShippedGroup()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped));

        var mins = await ev.EvaluateMinByGroupAsync(spec, o => o.IsShipped, o => o.Total);

        mins.Should().HaveCount(1);
        mins[true].Should().Be(80m);
    }

    // ── EvaluateMaxByGroupAsync ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateMaxByGroupAsync_Decimal_ByIsShipped_CorrectMaxes()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var maxes = await ev.EvaluateMaxByGroupAsync(spec, o => o.IsShipped, o => o.Total);

        maxes[true].Should().Be(210m);   // Eve
        maxes[false].Should().Be(500m);  // Dave
    }

    [Fact]
    public async Task EvaluateMaxByGroupAsync_Decimal_FilteredUnshipped_OnlyUnshippedGroup()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().IsFalse(o => o.IsShipped));

        var maxes = await ev.EvaluateMaxByGroupAsync(spec, o => o.IsShipped, o => o.Total);

        maxes.Should().HaveCount(1);
        maxes[false].Should().Be(500m);
    }

    // ── EvaluateAverageByGroupAsync ───────────────────────────────────────────

    [Fact]
    public async Task EvaluateAverageByGroupAsync_ByIsShipped_CorrectAverages()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var avgs = await ev.EvaluateAverageByGroupAsync(spec, o => o.IsShipped, o => o.Total);

        // true: (150+80+210)/3 = 440/3 ≈ 146.666...
        avgs[true].Should().BeApproximately(146.666m, 0.001m);
        // false: (320+500)/2 = 410
        avgs[false].Should().Be(410m);
    }

    [Fact]
    public async Task EvaluateAverageByGroupAsync_EmptyResult_ReturnsEmptyDictionary()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().GreaterThan(o => o.Total, 9999m));

        var avgs = await ev.EvaluateAverageByGroupAsync(spec, o => o.IsShipped, o => o.Total);

        avgs.Should().BeEmpty();
    }

    // ── EvaluateDuplicatesByGroupAsync ────────────────────────────────────────

    [Fact]
    public async Task EvaluateDuplicatesByGroupAsync_ByIsShipped_ReturnsBothGroups()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var dups = await ev.EvaluateDuplicatesByGroupAsync(spec, o => o.IsShipped);

        dups.Should().HaveCount(2);                  // true and false both have >1
        dups[true].Should().HaveCount(3);
        dups[false].Should().HaveCount(2);
    }

    [Fact]
    public async Task EvaluateDuplicatesByGroupAsync_ByUniqueKey_ReturnsEmpty()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        // CustomerName is unique → no group has >1 member
        var dups = await ev.EvaluateDuplicatesByGroupAsync(spec, o => o.CustomerName);

        dups.Should().BeEmpty();
    }

    // ── EvaluateUniquesByGroupAsync ───────────────────────────────────────────

    [Fact]
    public async Task EvaluateUniquesByGroupAsync_ByCustomerName_ReturnsAllFive()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        // Each CustomerName appears once → 5 unique groups
        var uniques = await ev.EvaluateUniquesByGroupAsync(spec, o => o.CustomerName);

        uniques.Should().HaveCount(5);
    }

    [Fact]
    public async Task EvaluateUniquesByGroupAsync_ByIsShipped_ReturnsEmpty()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        // IsShipped groups both have >1 member → no unique groups
        var uniques = await ev.EvaluateUniquesByGroupAsync(spec, o => o.IsShipped);

        uniques.Should().BeEmpty();
    }

    // ── EvaluateTopByGroupAsync ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateTopByGroupAsync_Top2ByIsShipped_CorrectGroupCounts()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Total)
            .WithTop(2);

        var topByGroup = await ev.EvaluateTopByGroupAsync(spec, o => o.IsShipped);

        topByGroup.Should().HaveCount(2);
        topByGroup[true].Should().HaveCount(2);   // Carol(80)+Alice(150) top 2 of 3
        topByGroup[false].Should().HaveCount(2);  // Bob(320)+Dave(500) — only 2 in group
    }

    [Fact]
    public async Task EvaluateTopByGroupAsync_Top1_ReturnsOnePerGroup()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Total)
            .WithTop(1);

        var topByGroup = await ev.EvaluateTopByGroupAsync(spec, o => o.IsShipped);

        topByGroup[true].Should().HaveCount(1);
        topByGroup[false].Should().HaveCount(1);
    }

    // ── EvaluateAggregateAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAggregateAsync_SumAggregator_ReturnsTotal()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var result = await ev.EvaluateAggregateAsync(spec, o => o.Total, (acc, x) => acc + x);

        result.Should().Be(1260m); // 150+320+80+500+210
    }

    [Fact]
    public async Task EvaluateAggregateAsync_MaxAggregator_ReturnsLargestTotal()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var result = await ev.EvaluateAggregateAsync(spec, o => o.Total, (acc, x) => acc > x ? acc : x);

        result.Should().Be(500m); // Dave
    }

    [Fact]
    public async Task EvaluateAggregateAsync_EmptyResult_ReturnsZero()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().GreaterThan(o => o.Total, 9999m));

        var result = await ev.EvaluateAggregateAsync(spec, o => o.Total, (acc, x) => acc + x);

        result.Should().Be(0m);
    }

    // ── EvaluateCountByGroupAsync (extra coverage) ────────────────────────────

    [Fact]
    public async Task EvaluateCountByGroupAsync_FilteredShipped_ReturnsOnlyShippedGroup()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped));

        var counts = await ev.EvaluateCountByGroupAsync(spec, o => o.IsShipped);

        counts.Should().HaveCount(1);
        counts[true].Should().Be(3);
    }
}
