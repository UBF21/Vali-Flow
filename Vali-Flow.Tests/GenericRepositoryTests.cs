using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Repository;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Repository;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

public sealed class GenericRepositoryTests
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
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true, CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 2, CustomerName = "Bob", Total = 320m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m, IsShipped = true, CreatedAt = new DateTime(2024, 3, 1) }
        );
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        return ctx;
    }

    private static IGenericRepository<TestOrder, int> CreateRepository(TestDbContext ctx) =>
        new GenericRepository<TestOrder, int>(new ValiFlowEvaluator<TestOrder>(ctx), o => o.Id);

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsEntity()
    {
        await using var ctx = await CreateSeededContextAsync();
        var repository = CreateRepository(ctx);

        var order = await repository.GetByIdAsync(2);

        order.Should().NotBeNull();
        order!.CustomerName.Should().Be("Bob");
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        await using var ctx = await CreateSeededContextAsync();
        var repository = CreateRepository(ctx);

        var order = await repository.GetByIdAsync(999);

        order.Should().BeNull();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_NoFilter_ReturnsAllEntities()
    {
        await using var ctx = await CreateSeededContextAsync();
        var repository = CreateRepository(ctx);

        var orders = await repository.GetAllAsync();

        orders.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_WithFilter_ReturnsOnlyMatchingEntities()
    {
        await using var ctx = await CreateSeededContextAsync();
        var repository = CreateRepository(ctx);
        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);

        var orders = await repository.GetAllAsync(filter);

        orders.Should().HaveCount(2).And.OnlyContain(o => o.IsShipped);
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPageAndMetadata()
    {
        await using var ctx = await CreateSeededContextAsync();
        var repository = CreateRepository(ctx);

        var paged = await repository.GetPagedAsync(null, o => o.Id, page: 1, pageSize: 2);

        paged.Items.Should().HaveCount(2);
        paged.TotalCount.Should().Be(3);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(2);
        paged.Items.Select(o => o.Id).Should().BeInAscendingOrder();
    }

    // ── AddAsync / UpdateAsync / DeleteAsync / SaveChangesAsync ──────────────

    [Fact]
    public async Task AddAsync_PersistsEntity()
    {
        await using var ctx = CreateContext();
        var repository = CreateRepository(ctx);
        var newOrder = new TestOrder { Id = 10, CustomerName = "Frank", Total = 99m, CreatedAt = DateTime.UtcNow };

        await repository.AddAsync(newOrder);

        (await repository.GetByIdAsync(10)).Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        await using var ctx = await CreateSeededContextAsync();
        var repository = CreateRepository(ctx);
        var order = await repository.GetByIdAsync(1);
        order!.CustomerName = "Alice Updated";

        await repository.UpdateAsync(order);

        (await repository.GetByIdAsync(1))!.CustomerName.Should().Be("Alice Updated");
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        await using var ctx = await CreateSeededContextAsync();
        var repository = CreateRepository(ctx);
        var order = await repository.GetByIdAsync(3);

        await repository.DeleteAsync(order!);

        (await repository.GetByIdAsync(3)).Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithSaveChangesFalse_DoesNotPersistUntilSaveChangesAsyncCalled()
    {
        await using var ctx = CreateContext();
        var repository = CreateRepository(ctx);
        var newOrder = new TestOrder { Id = 20, CustomerName = "Grace", Total = 10m, CreatedAt = DateTime.UtcNow };

        await repository.AddAsync(newOrder, saveChanges: false);
        ctx.ChangeTracker.Clear();
        (await repository.GetByIdAsync(20)).Should().BeNull();
    }
}
