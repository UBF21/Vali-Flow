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
/// Covers grouped async methods: EvaluateCountByGroupAsync, EvaluateSumByGroupAsync (int overload),
/// EvaluateMinByGroupAsync (int overload), EvaluateMaxByGroupAsync (int overload),
/// EvaluateAverageByGroupAsync, EvaluateTopByGroupAsync, EvaluateGroupedAsync (with filter),
/// and UpsertRangeAsync (insert, update, mixed).
/// </summary>
public sealed class ValiFlowEfGroupedAsyncTests
{
    // ── Seed ──────────────────────────────────────────────────────────────────
    // CustomerId=1: Alice(150), Carol(80), Eve(210)   → sum=440, min=80,  max=210
    // CustomerId=2: Bob(320),   Dave(500)              → sum=820, min=320, max=500
    // IsShipped=true:  Alice, Carol, Eve  (3 items)
    // IsShipped=false: Bob, Dave          (2 items)

    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<TestDbContext> CreateSeededContextAsync()
    {
        var ctx = CreateContext();
        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1), CustomerId = 1 },
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1), CustomerId = 2 },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,  IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1), CustomerId = 1 },
            new TestOrder { Id = 4, CustomerName = "Dave",  Total = 500m, IsShipped = false, CreatedAt = new DateTime(2024, 4, 1), CustomerId = 2 },
            new TestOrder { Id = 5, CustomerName = "Eve",   Total = 210m, IsShipped = true,  CreatedAt = new DateTime(2024, 5, 1), CustomerId = 1 }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ── EvaluateCountByGroupAsync ─────────────────────────────────────────────

    [Fact]
    public async Task EvaluateCountByGroupAsync_ByIsShipped_CorrectCounts()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var counts = await ev.EvaluateCountByGroupAsync(spec, o => o.IsShipped);

        counts.Should().HaveCount(2);
        counts[true].Should().Be(3);
        counts[false].Should().Be(2);
    }

    [Fact]
    public async Task EvaluateCountByGroupAsync_ByCustomerId_CorrectCounts()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var counts = await ev.EvaluateCountByGroupAsync(spec, o => o.CustomerId!.Value);

        counts.Should().HaveCount(2);
        counts[1].Should().Be(3);
        counts[2].Should().Be(2);
    }

    [Fact]
    public async Task EvaluateCountByGroupAsync_EmptyDb_ReturnsEmptyDictionary()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var counts = await ev.EvaluateCountByGroupAsync(spec, o => o.IsShipped);

        counts.Should().BeEmpty();
    }

    // ── EvaluateSumByGroupAsync (int overload) ────────────────────────────────

    [Fact]
    public async Task EvaluateSumByGroupAsync_IntSelector_ByCustomerId_CorrectSums()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var sums = await ev.EvaluateSumByGroupAsync(spec, o => o.CustomerId!.Value, o => (int)o.Total);

        sums.Should().HaveCount(2);
        sums[1].Should().Be(440);  // 150+80+210
        sums[2].Should().Be(820);  // 320+500
    }

    [Fact]
    public async Task EvaluateSumByGroupAsync_IntSelector_ByIsShipped_CorrectSums()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var sums = await ev.EvaluateSumByGroupAsync(spec, o => o.IsShipped, o => (int)o.Total);

        sums[true].Should().Be(440);
        sums[false].Should().Be(820);
    }

    // ── EvaluateMinByGroupAsync (int overload) ────────────────────────────────

    [Fact]
    public async Task EvaluateMinByGroupAsync_IntSelector_ByCustomerId_CorrectMins()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var mins = await ev.EvaluateMinByGroupAsync(spec, o => o.CustomerId!.Value, o => (int)o.Total);

        mins.Should().HaveCount(2);
        mins[1].Should().Be(80);   // Carol
        mins[2].Should().Be(320);  // Bob
    }

    [Fact]
    public async Task EvaluateMinByGroupAsync_IntSelector_FilteredShipped_OnlyShippedGroup()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped));

        var mins = await ev.EvaluateMinByGroupAsync(spec, o => o.IsShipped, o => (int)o.Total);

        mins.Should().HaveCount(1);
        mins[true].Should().Be(80);
    }

    // ── EvaluateMaxByGroupAsync (int overload) ────────────────────────────────

    [Fact]
    public async Task EvaluateMaxByGroupAsync_IntSelector_ByCustomerId_CorrectMaxes()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var maxes = await ev.EvaluateMaxByGroupAsync(spec, o => o.CustomerId!.Value, o => (int)o.Total);

        maxes.Should().HaveCount(2);
        maxes[1].Should().Be(210);  // Eve
        maxes[2].Should().Be(500);  // Dave
    }

    [Fact]
    public async Task EvaluateMaxByGroupAsync_IntSelector_FilteredUnshipped_OnlyUnshippedGroup()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().IsFalse(o => o.IsShipped));

        var maxes = await ev.EvaluateMaxByGroupAsync(spec, o => o.IsShipped, o => (int)o.Total);

        maxes.Should().HaveCount(1);
        maxes[false].Should().Be(500);
    }

    // ── EvaluateAverageByGroupAsync ───────────────────────────────────────────

    [Fact]
    public async Task EvaluateAverageByGroupAsync_ByCustomerId_CorrectAverages()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        var avgs = await ev.EvaluateAverageByGroupAsync(spec, o => o.CustomerId!.Value, o => o.Total);

        // CustomerId=1: (150+80+210)/3 ≈ 146.666...
        avgs[1].Should().BeApproximately(146.666m, 0.001m);
        // CustomerId=2: (320+500)/2 = 410
        avgs[2].Should().Be(410m);
    }

    // ── EvaluateTopByGroupAsync ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateTopByGroupAsync_Top1ByCustomerId_ReturnsOnePerGroup()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Total)
            .WithTop(1);

        var top = await ev.EvaluateTopByGroupAsync(spec, o => o.CustomerId!.Value);

        top.Should().HaveCount(2);
        top[1].Should().HaveCount(1);
        top[2].Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateTopByGroupAsync_Top2ByIsShipped_CorrectCounts()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Total, ascending: false)
            .WithTop(2);

        var top = await ev.EvaluateTopByGroupAsync(spec, o => o.IsShipped);

        top.Should().HaveCount(2);
        top[true].Should().HaveCount(2);   // top 2 of 3 shipped orders
        top[false].Should().HaveCount(2);  // only 2 unshipped orders
    }

    // ── EvaluateGroupedAsync with filter ─────────────────────────────────────

    [Fact]
    public async Task EvaluateGroupedAsync_FilteredByCustomerId1_ReturnsOnlyCustomer1Group()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(
            new ValiFlowQuery<TestOrder>().EqualTo(o => o.CustomerId!.Value, 1));

        var grouped = await ev.EvaluateGroupedAsync(spec, o => o.CustomerId!.Value);

        grouped.Should().HaveCount(1);
        grouped[1].Should().HaveCount(3);
        grouped[1].Should().OnlyContain(o => o.CustomerId == 1);
    }

    // ── UpsertRangeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpsertRangeAsync_InsertPath_NewEntitiesGetInserted()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var newOrders = new List<TestOrder>
        {
            new TestOrder { Id = 10, CustomerName = "Frank", Total = 100m, IsShipped = false, CreatedAt = DateTime.UtcNow },
            new TestOrder { Id = 11, CustomerName = "Grace", Total = 200m, IsShipped = true,  CreatedAt = DateTime.UtcNow }
        };

        await ev.UpsertRangeAsync(newOrders, o => o.Id);

        var count = await ctx.Orders.CountAsync();
        count.Should().Be(2);
        ctx.Orders.Should().Contain(o => o.CustomerName == "Frank");
        ctx.Orders.Should().Contain(o => o.CustomerName == "Grace");
    }

    [Fact]
    public async Task UpsertRangeAsync_UpdatePath_ExistingEntitiesGetUpdated()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var updatedOrders = new List<TestOrder>
        {
            new TestOrder { Id = 1, CustomerName = "Alice-Updated", Total = 999m, IsShipped = false, CreatedAt = new DateTime(2024, 1, 1), CustomerId = 1 },
            new TestOrder { Id = 2, CustomerName = "Bob-Updated",   Total = 888m, IsShipped = true,  CreatedAt = new DateTime(2024, 2, 1), CustomerId = 2 }
        };

        await ev.UpsertRangeAsync(updatedOrders, o => o.Id);

        var alice = await ctx.Orders.FirstAsync(o => o.Id == 1);
        alice.CustomerName.Should().Be("Alice-Updated");
        alice.Total.Should().Be(999m);

        var bob = await ctx.Orders.FirstAsync(o => o.Id == 2);
        bob.CustomerName.Should().Be("Bob-Updated");
        bob.Total.Should().Be(888m);
    }

    [Fact]
    public async Task UpsertRangeAsync_MixedPath_InsertsNewAndUpdatesExisting()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var mixedOrders = new List<TestOrder>
        {
            // existing order — should be updated
            new TestOrder { Id = 3, CustomerName = "Carol-Updated", Total = 999m, IsShipped = false, CreatedAt = new DateTime(2024, 3, 1), CustomerId = 1 },
            // new order — should be inserted
            new TestOrder { Id = 99, CustomerName = "NewPerson", Total = 750m, IsShipped = true, CreatedAt = DateTime.UtcNow, CustomerId = 1 }
        };

        await ev.UpsertRangeAsync(mixedOrders, o => o.Id);

        var totalCount = await ctx.Orders.CountAsync();
        totalCount.Should().Be(6); // 5 original + 1 new

        var carol = await ctx.Orders.FirstAsync(o => o.Id == 3);
        carol.CustomerName.Should().Be("Carol-Updated");

        var newPerson = await ctx.Orders.FirstAsync(o => o.Id == 99);
        newPerson.CustomerName.Should().Be("NewPerson");
    }
}
