using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

public sealed class ValiFlowEfWriteTests
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
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,  IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1) }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_NewEntity_PersistsInDatabase()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = new TestOrder { Id = 1, CustomerName = "Dave", Total = 200m, IsShipped = false, CreatedAt = DateTime.UtcNow };
        await ev.AddAsync(order);

        var saved = await ctx.Orders.FindAsync(1);
        saved.Should().NotBeNull();
        saved!.CustomerName.Should().Be("Dave");
    }

    [Fact]
    public async Task AddAsync_NullEntity_Throws()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = () => ev.AddAsync(null!);

        await act.Should().ThrowAsync<Exception>();
    }

    // ── AddRangeAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AddRangeAsync_MultipleEntities_AllPersisted()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = new[]
        {
            new TestOrder { Id = 1, CustomerName = "A", Total = 10m, IsShipped = false, CreatedAt = DateTime.UtcNow },
            new TestOrder { Id = 2, CustomerName = "B", Total = 20m, IsShipped = true,  CreatedAt = DateTime.UtcNow }
        };

        await ev.AddRangeAsync(orders);

        (await ctx.Orders.CountAsync()).Should().Be(2);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingEntity_ChangesAreSaved()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = await ctx.Orders.FindAsync(1);
        order!.Total = 999m;

        await ev.UpdateAsync(order);

        var updated = await ctx.Orders.AsNoTracking().FirstAsync(o => o.Id == 1);
        updated.Total.Should().Be(999m);
    }

    // ── UpdateRangeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRangeAsync_MultipleEntities_AllUpdated()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = await ctx.Orders.ToListAsync();
        foreach (var o in orders) o.IsShipped = true;

        await ev.UpdateRangeAsync(orders);

        var all = await ctx.Orders.AsNoTracking().ToListAsync();
        all.Should().OnlyContain(o => o.IsShipped);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingEntity_RemovedFromDatabase()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = await ctx.Orders.FindAsync(1);
        await ev.DeleteAsync(order!);

        (await ctx.Orders.FindAsync(1)).Should().BeNull();
    }

    // ── DeleteRangeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRangeAsync_MultipleEntities_AllRemoved()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var toDelete = await ctx.Orders.Where(o => o.IsShipped).ToListAsync();
        await ev.DeleteRangeAsync(toDelete);

        (await ctx.Orders.CountAsync()).Should().Be(1); // only Bob remains
    }

    // ── UpsertAsync — insert path ─────────────────────────────────────────────

    [Fact]
    public async Task UpsertAsync_EntityDoesNotExist_Inserts()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = new TestOrder { Id = 10, CustomerName = "New", Total = 50m, IsShipped = false, CreatedAt = DateTime.UtcNow };
        await ev.UpsertAsync(order, o => o.Id == 10);

        (await ctx.Orders.FindAsync(10)).Should().NotBeNull();
    }

    // ── UpsertAsync — update path ─────────────────────────────────────────────

    [Fact]
    public async Task UpsertAsync_EntityExists_UpdatesValues()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var updated = new TestOrder { Id = 1, CustomerName = "Alice-Updated", Total = 500m, IsShipped = true, CreatedAt = DateTime.UtcNow };
        await ev.UpsertAsync(updated, o => o.Id == 1);

        var result = await ctx.Orders.AsNoTracking().FirstAsync(o => o.Id == 1);
        result.CustomerName.Should().Be("Alice-Updated");
        result.Total.Should().Be(500m);
    }

    // ── SaveChangesAsync — deferred ───────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_DeferredAdd_PersistsOnExplicitSave()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = new TestOrder { Id = 1, CustomerName = "Deferred", Total = 100m, IsShipped = false, CreatedAt = DateTime.UtcNow };

        // saveChanges: false — nothing persisted yet
        await ev.AddAsync(order, saveChanges: false);
        (await ctx.Orders.AsNoTracking().CountAsync()).Should().Be(0);

        // Explicit save
        await ev.SaveChangesAsync();
        (await ctx.Orders.AsNoTracking().CountAsync()).Should().Be(1);
    }

    // ── EvaluateAggregateAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAggregateAsync_CustomSum_ReturnsCorrectResult()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Sum via custom aggregator: 150 + 320 + 80 = 550
        decimal total = await ev.EvaluateAggregateAsync(
            new BasicSpecification<TestOrder>(),
            o => o.Total,
            (acc, x) => acc + x);

        total.Should().Be(550m);
    }

    [Fact]
    public async Task EvaluateAggregateAsync_EmptySet_ReturnsZero()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        decimal total = await ev.EvaluateAggregateAsync(
            new BasicSpecification<TestOrder>(),
            o => o.Total,
            (acc, x) => acc + x);

        total.Should().Be(0m);
    }
}
