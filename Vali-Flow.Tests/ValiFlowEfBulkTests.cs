using EFCore.BulkExtensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

public sealed class ValiFlowEfBulkTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TestDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<TestDbContext> CreateSeededInMemoryContextAsync()
    {
        var ctx = CreateInMemoryContext();
        ctx.Orders.AddRange(SeedOrders());
        await ctx.SaveChangesAsync();
        return ctx;
    }

    private static IEnumerable<TestOrder> SeedOrders() =>
        Enumerable.Range(1, 5).Select(i => new TestOrder
        {
            Id = i,
            CustomerName = $"Customer{i}",
            Total = i * 100m,
            IsShipped = i % 2 == 0,
            CreatedAt = new DateTime(2024, i, 1)
        });

    // ── AddRangeAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AddRangeAsync_EmptyList_ThrowsArgumentException()
    {
        await using var ctx = await CreateSeededInMemoryContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = async () => await evaluator.AddRangeAsync(new List<TestOrder>(), saveChanges: true);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddRangeAsync_SingleEntity_PersistsToDatabase()
    {
        await using var ctx = CreateInMemoryContext();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = new TestOrder { Id = 10, CustomerName = "Single", Total = 50m, IsShipped = false, CreatedAt = DateTime.UtcNow };
        await evaluator.AddRangeAsync(new[] { order }, saveChanges: true);

        (await ctx.Orders.CountAsync()).Should().Be(1);
        (await ctx.Orders.FindAsync(10))!.CustomerName.Should().Be("Single");
    }

    [Fact]
    public async Task AddRangeAsync_HundredEntities_AllPersisted()
    {
        await using var ctx = CreateInMemoryContext();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = Enumerable.Range(1, 100).Select(i => new TestOrder
        {
            Id = i, CustomerName = $"C{i}", Total = i * 10m, IsShipped = false, CreatedAt = DateTime.UtcNow
        }).ToList();

        await evaluator.AddRangeAsync(orders, saveChanges: true);

        (await ctx.Orders.CountAsync()).Should().Be(100);
    }

    [Fact]
    public async Task AddRangeAsync_SaveChangesFalse_RequiresManualSave()
    {
        await using var ctx = CreateInMemoryContext();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = Enumerable.Range(1, 3).Select(i => new TestOrder
        {
            Id = i, CustomerName = $"C{i}", Total = i * 10m, IsShipped = false, CreatedAt = DateTime.UtcNow
        }).ToList();

        await evaluator.AddRangeAsync(orders, saveChanges: false);
        (await ctx.Orders.AsNoTracking().CountAsync()).Should().Be(0);

        await evaluator.SaveChangesAsync();
        (await ctx.Orders.CountAsync()).Should().Be(3);
    }

    // ── UpdateRangeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRangeAsync_EmptyList_ThrowsArgumentException()
    {
        await using var ctx = await CreateSeededInMemoryContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = async () => await evaluator.UpdateRangeAsync(new List<TestOrder>(), saveChanges: true);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateRangeAsync_SingleField_UpdatesPersisted()
    {
        await using var ctx = await CreateSeededInMemoryContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = await ctx.Orders.FindAsync(1);
        order!.Total = 9999m;

        await evaluator.UpdateRangeAsync(new[] { order }, saveChanges: true);

        ctx.ChangeTracker.Clear();
        (await ctx.Orders.FindAsync(1))!.Total.Should().Be(9999m);
    }

    [Fact]
    public async Task UpdateRangeAsync_MultipleFields_AllFieldsUpdated()
    {
        await using var ctx = await CreateSeededInMemoryContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = await ctx.Orders.Where(o => o.Id <= 2).ToListAsync();
        foreach (var o in orders) { o.IsShipped = true; o.Total = 1m; }

        await evaluator.UpdateRangeAsync(orders, saveChanges: true);

        ctx.ChangeTracker.Clear();
        (await ctx.Orders.Where(o => o.Id <= 2).ToListAsync())
            .Should().OnlyContain(o => o.IsShipped && o.Total == 1m);
    }

    [Fact]
    public async Task UpdateRangeAsync_SaveChangesFalse_PendingUntilManualSave()
    {
        await using var ctx = await CreateSeededInMemoryContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = await ctx.Orders.FindAsync(3);
        order!.CustomerName = "Modified";

        await evaluator.UpdateRangeAsync(new[] { order }, saveChanges: false);
        ctx.ChangeTracker.HasChanges().Should().BeTrue();

        await evaluator.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        (await ctx.Orders.FindAsync(3))!.CustomerName.Should().Be("Modified");
    }

    // ── DeleteRangeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRangeAsync_EmptyList_ThrowsArgumentException()
    {
        await using var ctx = await CreateSeededInMemoryContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = async () => await evaluator.DeleteRangeAsync(new List<TestOrder>(), saveChanges: true);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteRangeAsync_SingleEntity_RemovesFromDatabase()
    {
        await using var ctx = await CreateSeededInMemoryContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = await ctx.Orders.FindAsync(2);
        await evaluator.DeleteRangeAsync(new[] { order! }, saveChanges: true);

        (await ctx.Orders.FindAsync(2)).Should().BeNull();
        (await ctx.Orders.CountAsync()).Should().Be(4);
    }

    [Fact]
    public async Task DeleteRangeAsync_ByCondition_RemovesMatching()
    {
        await using var ctx = await CreateSeededInMemoryContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var shipped = await ctx.Orders.Where(o => o.IsShipped).ToListAsync();
        await evaluator.DeleteRangeAsync(shipped, saveChanges: true);

        (await ctx.Orders.AnyAsync(o => o.IsShipped)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRangeAsync_SaveChangesFalse_PendingUntilManualSave()
    {
        await using var ctx = await CreateSeededInMemoryContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = await ctx.Orders.FindAsync(5);
        await evaluator.DeleteRangeAsync(new[] { order! }, saveChanges: false);

        ctx.ChangeTracker.HasChanges().Should().BeTrue();
        await evaluator.SaveChangesAsync();
        (await ctx.Orders.FindAsync(5)).Should().BeNull();
    }

    // ── BulkInsertAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task BulkInsertAsync_EmptyList_ThrowsArgumentException()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var evaluator = new ValiFlowEvaluator<TestOrder>(sqlCtx.Context);

        Func<Task> act = async () => await evaluator.BulkInsertAsync(new List<TestOrder>());

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task BulkInsertAsync_BasicInsert_AllEntitiesPersisted()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = Enumerable.Range(1, 10).Select(i => new TestOrder
        {
            Id = i, CustomerName = $"C{i}", Total = i * 50m, IsShipped = false, CreatedAt = DateTime.UtcNow
        }).ToList();

        await evaluator.BulkInsertAsync(orders);

        (await ctx.Orders.CountAsync()).Should().Be(10);
    }

    [Fact]
    public async Task BulkInsertAsync_WithBulkConfig_RespectsConfig()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = Enumerable.Range(1, 5).Select(i => new TestOrder
        {
            Id = i, CustomerName = $"Bulk{i}", Total = i * 10m, IsShipped = true, CreatedAt = DateTime.UtcNow
        }).ToList();

        var config = new BulkConfig { BatchSize = 2 };
        await evaluator.BulkInsertAsync(orders, config);

        (await ctx.Orders.CountAsync()).Should().Be(5);
        (await ctx.Orders.FirstAsync(o => o.Id == 3)).CustomerName.Should().Be("Bulk3");
    }

    [Fact]
    public async Task BulkInsertAsync_FiveHundredEntities_AllPersisted()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = Enumerable.Range(1, 500).Select(i => new TestOrder
        {
            Id = i, CustomerName = $"C{i}", Total = i * 1m, IsShipped = i % 2 == 0, CreatedAt = DateTime.UtcNow
        }).ToList();

        await evaluator.BulkInsertAsync(orders);

        (await ctx.Orders.CountAsync()).Should().Be(500);
    }

    // ── BulkUpdateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task BulkUpdateAsync_EmptyList_ThrowsArgumentException()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        ctx_seed(sqlCtx.Context);
        await sqlCtx.Context.SaveChangesAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(sqlCtx.Context);

        Func<Task> act = async () => await evaluator.BulkUpdateAsync(new List<TestOrder>());

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task BulkUpdateAsync_SingleField_UpdatesPersisted()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        ctx.Orders.AddRange(SeedOrders());
        await ctx.SaveChangesAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = await ctx.Orders.ToListAsync();
        foreach (var o in orders) o.IsShipped = true;

        await evaluator.BulkUpdateAsync(orders);

        ctx.ChangeTracker.Clear();
        (await ctx.Orders.AllAsync(o => o.IsShipped)).Should().BeTrue();
    }

    [Fact]
    public async Task BulkUpdateAsync_MultipleFields_AllFieldsUpdated()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        ctx.Orders.AddRange(SeedOrders());
        await ctx.SaveChangesAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = await ctx.Orders.Where(o => o.Id <= 3).ToListAsync();
        foreach (var o in orders) { o.Total = 9999m; o.CustomerName = "Bulk"; }

        await evaluator.BulkUpdateAsync(orders);

        ctx.ChangeTracker.Clear();
        (await ctx.Orders.Where(o => o.Id <= 3).ToListAsync())
            .Should().OnlyContain(o => o.Total == 9999m && o.CustomerName == "Bulk");
    }

    [Fact]
    public async Task BulkUpdateAsync_WithBulkConfig_RespectsConfig()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        ctx.Orders.AddRange(SeedOrders());
        await ctx.SaveChangesAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = await ctx.Orders.ToListAsync();
        foreach (var o in orders) o.Total = 1m;

        var config = new BulkConfig { BatchSize = 2 };
        await evaluator.BulkUpdateAsync(orders, config);

        ctx.ChangeTracker.Clear();
        (await ctx.Orders.AllAsync(o => o.Total == 1m)).Should().BeTrue();
    }

    // ── BulkDeleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task BulkDeleteAsync_EmptyList_ThrowsArgumentException()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        ctx_seed(sqlCtx.Context);
        await sqlCtx.Context.SaveChangesAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(sqlCtx.Context);

        Func<Task> act = async () => await evaluator.BulkDeleteAsync(new List<TestOrder>());

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task BulkDeleteAsync_AllEntities_DatabaseIsEmpty()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        ctx.Orders.AddRange(SeedOrders());
        await ctx.SaveChangesAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = await ctx.Orders.ToListAsync();
        await evaluator.BulkDeleteAsync(orders);

        (await ctx.Orders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BulkDeleteAsync_ByCondition_RemovesMatching()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        ctx.Orders.AddRange(SeedOrders());
        await ctx.SaveChangesAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var shipped = await ctx.Orders.Where(o => o.IsShipped).ToListAsync();
        await evaluator.BulkDeleteAsync(shipped);

        (await ctx.Orders.AnyAsync(o => o.IsShipped)).Should().BeFalse();
    }

    [Fact]
    public async Task BulkDeleteAsync_WithBulkConfig_DeletesCorrectly()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        ctx.Orders.AddRange(SeedOrders());
        await ctx.SaveChangesAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var initialCount = await ctx.Orders.CountAsync();
        var orders = await ctx.Orders.AsNoTracking().Where(o => o.IsShipped).ToListAsync();
        var config = new BulkConfig { BatchSize = 10 };
        await evaluator.BulkDeleteAsync(orders, config);

        ctx.ChangeTracker.Clear();
        var finalCount = await ctx.Orders.CountAsync();
        finalCount.Should().Be(initialCount - orders.Count);
    }

    // ── BulkInsertOrUpdateAsync ───────────────────────────────────────────────

    [Fact]
    public async Task BulkInsertOrUpdateAsync_NewEntities_AllInserted()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = Enumerable.Range(1, 5).Select(i => new TestOrder
        {
            Id = i, CustomerName = $"New{i}", Total = i * 10m, IsShipped = false, CreatedAt = DateTime.UtcNow
        }).ToList();

        await evaluator.BulkInsertOrUpdateAsync(orders);

        (await ctx.Orders.CountAsync()).Should().Be(5);
    }

    [Fact]
    public async Task BulkInsertOrUpdateAsync_ExistingEntities_DoesNotThrow()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        ctx.Orders.AddRange(SeedOrders());
        await ctx.SaveChangesAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = await ctx.Orders.AsNoTracking().ToListAsync();
        foreach (var o in orders) o.CustomerName = "Updated";

        // BulkInsertOrUpdate should complete without exceptions
        Func<Task> act = async () => await evaluator.BulkInsertOrUpdateAsync(orders);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BulkInsertOrUpdateAsync_NewEntitiesInMix_NewOnesInserted()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        // Insert only new entities (no conflict with existing PKs)
        var newOrders = Enumerable.Range(100, 5).Select(i => new TestOrder
        {
            Id = i, CustomerName = $"New{i}", Total = i * 10m, IsShipped = false, CreatedAt = DateTime.UtcNow
        }).ToList();

        await evaluator.BulkInsertOrUpdateAsync(newOrders);

        ctx.ChangeTracker.Clear();
        (await ctx.Orders.CountAsync()).Should().Be(5);
        (await ctx.Orders.FirstOrDefaultAsync(o => o.CustomerName == "New100")).Should().NotBeNull();
    }

    [Fact]
    public async Task BulkInsertOrUpdateAsync_WithBulkConfig_RespectsConfig()
    {
        await using var sqlCtx = SqliteTestContext.Create();
        var ctx = sqlCtx.Context;
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var orders = Enumerable.Range(1, 10).Select(i => new TestOrder
        {
            Id = i, CustomerName = $"C{i}", Total = i * 5m, IsShipped = false, CreatedAt = DateTime.UtcNow
        }).ToList();

        var config = new BulkConfig { BatchSize = 3 };
        await evaluator.BulkInsertOrUpdateAsync(orders, config);

        (await ctx.Orders.CountAsync()).Should().Be(10);
    }

    private static void ctx_seed(TestDbContext ctx) => ctx.Orders.AddRange(SeedOrders());
}
