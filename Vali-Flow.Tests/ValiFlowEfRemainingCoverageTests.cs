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
/// Covers remaining gaps: EvaluateSumAsync non-decimal overloads, EvaluateMinAsync/MaxAsync non-decimal types,
/// EvaluateAllAsync, EvaluateAllFailedAsync, and AsNoTracking specification hint.
/// </summary>
public sealed class ValiFlowEfRemainingCoverageTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    /// <summary>Seeds 5 orders with Totals 100, 200, 300, 400, 500 (sum = 1500).</summary>
    private static async Task<TestDbContext> CreateSeededContextAsync()
    {
        var ctx = CreateContext();
        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "Alpha",   Total = 100m, IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 2, CustomerName = "Beta",    Total = 200m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) },
            new TestOrder { Id = 3, CustomerName = "Gamma",   Total = 300m, IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1) },
            new TestOrder { Id = 4, CustomerName = "Delta",   Total = 400m, IsShipped = false, CreatedAt = new DateTime(2024, 4, 1) },
            new TestOrder { Id = 5, CustomerName = "Epsilon", Total = 500m, IsShipped = true,  CreatedAt = new DateTime(2024, 5, 1) }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    private static BasicSpecification<TestOrder> AllOrders() => new();

    // ── Section 1: EvaluateSumAsync — int overload ────────────────────────────

    [Fact]
    public async Task EvaluateSumAsync_Int_AllOrders_ReturnsCorrectSum()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        int sum = await ev.EvaluateSumAsync(AllOrders(), o => (int)o.Total);

        sum.Should().Be(1500);
    }

    [Fact]
    public async Task EvaluateSumAsync_Int_EmptyDb_ReturnsZero()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        int sum = await ev.EvaluateSumAsync(AllOrders(), o => (int)o.Total);

        sum.Should().Be(0);
    }

    // ── Section 1: EvaluateSumAsync — long overload ───────────────────────────

    [Fact]
    public async Task EvaluateSumAsync_Long_AllOrders_ReturnsCorrectSum()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        long sum = await ev.EvaluateSumAsync(AllOrders(), o => (long)o.Total);

        sum.Should().Be(1500L);
    }

    [Fact]
    public async Task EvaluateSumAsync_Long_EmptyDb_ReturnsZero()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        long sum = await ev.EvaluateSumAsync(AllOrders(), o => (long)o.Total);

        sum.Should().Be(0L);
    }

    // ── Section 1: EvaluateSumAsync — double overload ─────────────────────────

    [Fact]
    public async Task EvaluateSumAsync_Double_AllOrders_ReturnsCorrectSum()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        double sum = await ev.EvaluateSumAsync(AllOrders(), o => (double)o.Total);

        sum.Should().BeApproximately(1500.0, 0.001);
    }

    [Fact]
    public async Task EvaluateSumAsync_Double_EmptyDb_ReturnsZero()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        double sum = await ev.EvaluateSumAsync(AllOrders(), o => (double)o.Total);

        sum.Should().BeApproximately(0.0, 0.001);
    }

    // ── Section 1: EvaluateSumAsync — float overload ──────────────────────────

    [Fact]
    public async Task EvaluateSumAsync_Float_AllOrders_ReturnsCorrectSum()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        float sum = await ev.EvaluateSumAsync(AllOrders(), o => (float)o.Total);

        sum.Should().BeApproximately(1500f, 1f);
    }

    [Fact]
    public async Task EvaluateSumAsync_Float_EmptyDb_ReturnsZero()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        float sum = await ev.EvaluateSumAsync(AllOrders(), o => (float)o.Total);

        sum.Should().BeApproximately(0f, 0.001f);
    }

    // ── Section 2: EvaluateMinAsync non-decimal types ─────────────────────────

    [Fact]
    public async Task EvaluateMinAsync_Int_AllOrders_ReturnsSmallestTotal()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        int min = await ev.EvaluateMinAsync(AllOrders(), o => (int)o.Total);

        min.Should().Be(100);
    }

    [Fact]
    public async Task EvaluateMinAsync_Int_EmptyDb_ReturnsZero()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        int min = await ev.EvaluateMinAsync(AllOrders(), o => (int)o.Total);

        min.Should().Be(0);
    }

    [Fact]
    public async Task EvaluateMinAsync_Double_AllOrders_ReturnsSmallestTotal()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        double min = await ev.EvaluateMinAsync(AllOrders(), o => (double)o.Total);

        min.Should().BeApproximately(100.0, 0.001);
    }

    [Fact]
    public async Task EvaluateMinAsync_Double_EmptyDb_ReturnsZero()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        double min = await ev.EvaluateMinAsync(AllOrders(), o => (double)o.Total);

        min.Should().BeApproximately(0.0, 0.001);
    }

    // ── Section 2: EvaluateMaxAsync non-decimal types ─────────────────────────

    [Fact]
    public async Task EvaluateMaxAsync_Int_AllOrders_ReturnsLargestTotal()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        int max = await ev.EvaluateMaxAsync(AllOrders(), o => (int)o.Total);

        max.Should().Be(500);
    }

    [Fact]
    public async Task EvaluateMaxAsync_Int_EmptyDb_ReturnsZero()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        int max = await ev.EvaluateMaxAsync(AllOrders(), o => (int)o.Total);

        max.Should().Be(0);
    }

    // ── Section 3: EvaluateAllAsync ───────────────────────────────────────────

    [Fact]
    public async Task EvaluateAllAsync_AllOrders_ReturnsAllFiveOrders()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id);

        IQueryable<TestOrder> query = await ev.EvaluateAllAsync(spec);
        List<TestOrder> result = await query.ToListAsync();

        result.Should().HaveCount(5);
        result.Select(o => o.Id).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task EvaluateAllAsync_EmptyDb_ReturnsEmptyQueryable()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id);

        IQueryable<TestOrder> query = await ev.EvaluateAllAsync(spec);
        List<TestOrder> result = await query.ToListAsync();

        result.Should().BeEmpty();
    }

    // ── Section 3: EvaluateAllFailedAsync ────────────────────────────────────

    [Fact]
    public async Task EvaluateAllFailedAsync_IsShippedFilter_ReturnsOnlyUnshippedOrders()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Filter: IsShipped == true → failed = unshipped (IsShipped == false)
        var spec = new QuerySpecification<TestOrder>(
                new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped))
            .WithOrderBy(o => o.Id);

        IQueryable<TestOrder> query = await ev.EvaluateAllFailedAsync(spec);
        List<TestOrder> result = await query.ToListAsync();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.IsShipped.Should().BeFalse());
    }

    [Fact]
    public async Task EvaluateAllFailedAsync_NoMatchForFilter_ReturnsAllOrders()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Filter: Total > 9999 → no entity passes → failed = all 5
        var spec = new QuerySpecification<TestOrder>(
                new ValiFlowQuery<TestOrder>().GreaterThan(o => o.Total, 9999m))
            .WithOrderBy(o => o.Id);

        IQueryable<TestOrder> query = await ev.EvaluateAllFailedAsync(spec);
        List<TestOrder> result = await query.ToListAsync();

        result.Should().HaveCount(5);
    }

    // ── Section 4: Specification hints — AsNoTracking ─────────────────────────

    [Fact]
    public async Task AsNoTracking_True_ModifyingFetchedEntity_DoesNotMarkContextDirty()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // AsNoTracking = true (default), so fetched entity is not tracked
        var spec = new BasicSpecification<TestOrder>()
            .WithAsNoTracking(true);

        TestOrder? order = await ev.EvaluateGetFirstAsync(spec);
        order.Should().NotBeNull();

        // Mutate the in-memory object; because it is untracked the context must not see changes
        order!.CustomerName = "MODIFIED";

        ctx.ChangeTracker.HasChanges().Should().BeFalse();
    }

    [Fact]
    public async Task AsNoTracking_False_ModifyingFetchedEntity_MarksContextDirty()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // AsNoTracking = false → entity is tracked
        var spec = new BasicSpecification<TestOrder>()
            .WithAsNoTracking(false);

        TestOrder? order = await ev.EvaluateGetFirstAsync(spec);
        order.Should().NotBeNull();

        // Mutate; the context should detect the change
        order!.CustomerName = "MODIFIED";

        ctx.ChangeTracker.HasChanges().Should().BeTrue();
    }
}
